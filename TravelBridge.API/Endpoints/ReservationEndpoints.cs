using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.API.Models;
using TravelBridge.API.Models.ExternalModels;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services.Viva;
using TravelBridge.API.Services.WebHotelier;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Endpoints
{
    public class ReservationEndpoints
    {
        private readonly WebHotelierPropertiesService webHotelierPropertiesService;

        public ReservationEndpoints(WebHotelierPropertiesService webHotelierPropertiesService)
        {
            this.webHotelierPropertiesService = webHotelierPropertiesService;
        }

        public record SubmitSearchParameters
        (
            [FromQuery] string checkin,
            [FromQuery] string checkOut,
            [FromQuery] string? couponCode,
            //[FromQuery] int? adults,
            //[FromQuery] string? children,
            //[FromQuery] string? party,
            [FromQuery] string? hotelId,
            [FromQuery] string? selectedRates
        );

        public record BookingRequest(
               string HotelId,
               string CheckIn,
               string CheckOut,
               int? Rooms,
               string? Children,
               string? couponCode,
               int? Adults,
               string? Party,
               string? SelectedRates,
               decimal? TotalPrice,
               decimal? PrepayAmount,
               CustomerInfo? CustomerInfo
           )
        {
            public decimal? PrepayAmount { get; init; } = (PrepayAmount == null || PrepayAmount == 0) ? TotalPrice : PrepayAmount;
        };

        public record CustomerInfo(
            string? FirstName,
            string? LastName,
            string? Email,
            string? Phone,
            string? Requests
        );

        public record PaymentInfo
        (
            string? Tid,
            string OrderCode
        );


        public record ReservationDetails(
            string hotelId,
            string checkIn,
            string checkOut,
            string children,
            string adults,
            string party,
            string selectedRates,
            decimal totalPrice
        );

        public record FormData(
            string firstName,
            string lastName,
            string email,
            string phone,
            string requests
        );

        public record ReservationRequest(
            string? couponCode,
            ReservationDetails reservationDetails,
            FormData formData
        );

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/reservation");

            apiGroup.MapGet("/checkout",
            [EndpointSummary("Returns best matching locations that contain the provided search term")]
            async ([AsParameters] SubmitSearchParameters pars) =>
            await GetCheckoutInfo(pars))
                .WithName("Checkout")
                .WithOpenApi(CustomizeCheckoutOperation);

            apiGroup.MapPost("/preparePayment",
            [EndpointSummary("Creates Reservation On DB and prepares payment")]
            async (BookingRequest pars, ReservationsRepository repo, VivaService viva) =>
            await PreparePayment(pars, repo, viva))
                .WithName("PreparePayment")
                .WithOpenApi(CustomizePreparePaymentOperation);

            apiGroup.MapPost("/paymentFailed",
            [EndpointSummary("returns full reservation data")]
            async (PaymentInfo pay, ReservationsRepository repo) =>
            await GetOrderInfo(pay, repo))
                .WithName("PaymentFailed")
                .WithOpenApi(CustomizePaymentFailedOperation);

            apiGroup.MapPost("/paymentSucceed",
           [EndpointSummary("Confirms Payment and returns reservation basic data")]
            async (PaymentInfo pay, ReservationsRepository repo, VivaService viva) =>
           await ConfirmPayment(pay, repo, viva))
               .WithName("PaymentSucceed")
               .WithOpenApi(CustomizePaymentSucceedOperation);

            apiGroup.MapPost("/cancelBooking",
          [EndpointSummary("cancels booking")]
            async (string bookingNumber, ReservationsRepository repo) =>
          await CancelBooking(bookingNumber, repo))
              .WithName("CancelBooking");

            apiGroup.MapPost("/applyCoupon",
          [EndpointSummary("Applies Coupon")]
            async (ReservationRequest reservationRequest, ReservationsRepository repo) =>
          await ApplyCoupon(reservationRequest, repo))
              .WithName("ApplyCoupon");
        }

        private async Task CancelBooking(string OrderCode, ReservationsRepository repo)
        {
            var reservation = await repo.GetReservationBasicDataByPaymentCode(OrderCode);

            await webHotelierPropertiesService.CancelBooking(reservation, repo);
        }

        private async Task<CheckoutResponse> GetOrderInfo(PaymentInfo pay, ReservationsRepository repo)
        {
            if (string.IsNullOrWhiteSpace(pay.OrderCode))
            {
                throw new ArgumentException("Invalid order code");
            }

            var reservation = await repo.GetFullReservationFromPaymentCode(pay.OrderCode)
                ?? throw new InvalidOperationException("Reservation not found");

            var payment = reservation.Payments.FirstOrDefault(p => p.OrderCode == pay.OrderCode)
                ?? throw new InvalidOperationException("Payment not found");

            await repo.UpdatePaymentFailed(payment);

            var res = await GetCheckoutInfo(new SubmitSearchParameters(
                reservation.CheckIn.ToString("dd/MM/yyyy"),
                reservation.CheckOut.ToString("dd/MM/yyyy"),
                "",
                reservation.HotelCode,
                JsonSerializer.Serialize(reservation.Rates.Select(r => new SelectedRate { rateId = r.RateId, count = r.Quantity, searchParty = r.SearchParty?.Party ?? "" }))
            ));

            res.LabelErrorMessage = "Η πληρωμή απέτυχε. Παρακαλώ δοκιμάστε ξανά.";
            res.ErrorCode = "PAY_FAILED";

            return res;
        }

        private async Task<SuccessfullPaymentResponse> ConfirmPayment(PaymentInfo pay, ReservationsRepository repo, VivaService viva)
        {
            if (string.IsNullOrWhiteSpace(pay.OrderCode) || string.IsNullOrWhiteSpace(pay.Tid))
            {
                throw new ArgumentException("Invalid payment info");
            }

            var reservation = await repo.GetReservationBasicDataByPaymentCode(pay.OrderCode);
            if (reservation == null)
            {
                return new SuccessfullPaymentResponse(error: "Reservation not found", "NO_RES");
            }
            //await webHotelierPropertiesService.SendConfirmationEmail();

            try
            {
                if (await viva.ValidatePayment(pay.OrderCode, pay.Tid, reservation) && await repo.UpdatePaymentSucceed(pay.OrderCode, pay.Tid))
                {
                    await webHotelierPropertiesService.CreateBooking(reservation, repo);

                    return new SuccessfullPaymentResponse
                    {
                        SuccessfullPayment = true,
                        Data = new DataSucess
                        {
                            CheckIn = reservation.CheckIn.ToString("dd/MM/yyyy"),
                            CheckOut = reservation.CheckOut.ToString("dd/MM/yyyy"),
                            HotelName = reservation.HotelName ?? "",
                            ReservationId = reservation.Id
                        }
                    };
                }
                else
                {
                    return new SuccessfullPaymentResponse(error: $"Υπήρξε πρόβλημα με την πληρωμή της κράτησής σας με αριθμό {reservation.Id}, παρακαλώ επικοινωνήστε μαζί μας.", "RES_ERROR");
                }
            }
            catch (Exception ex)
            {
                return new SuccessfullPaymentResponse(error: $"Υπήρξε πρόβλημα με την πληρωμή της κράτησής σας με αριθμό {reservation.Id}, παρακαλώ επικοινωνήστε μαζί μας.", "RES_ERROR");
            }
        }

        private async Task<PreparePaymentResponse> PreparePayment(BookingRequest pars, ReservationsRepository repo, VivaService viva)
        {
            #region Param Validation

            if (!DateTime.TryParseExact(pars.CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(pars.CheckOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            List<SelectedRate>? SelectedRates;
            if (string.IsNullOrWhiteSpace(pars.SelectedRates))
            {
                throw new InvalidCastException("Invalid selected rates");
            }
            else
            {
                SelectedRates = RatesToList(pars.SelectedRates);
                if (SelectedRates == null)
                {
                    throw new InvalidCastException("Invalid selected rates");
                }
            }

            string party;
            if (string.IsNullOrWhiteSpace(pars.Party))
            {
                if (pars.Adults == null || pars.Adults < 1)
                {
                    throw new ArgumentException("There must be at least one adult in the room.");
                }

                party = CreateParty(pars.Adults.Value, pars.Children);
            }
            else
            {
                party = BuildMultiRoomJson(pars.Party);
            }

            var hotelInfo = pars.HotelId?.Split('-');
            if (hotelInfo?.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            #endregion Param Validation

            SingleAvailabilityRequest availReq = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                Party = party,
                PropertyId = hotelInfo[1]
            };

            var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
            var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(availReq, parsedCheckin, repo, SelectedRates, pars.couponCode);
            Task.WaitAll(availTask, hotelTask);

            SingleAvailabilityResponse? availRes = await availTask;
            HotelInfoResponse? hotelRes = await hotelTask;

            if (availRes.Data != null)
            {
                availRes.Data.Provider = Provider.WebHotelier;
            }

            int nights = (parsedCheckOut - parsedCheckin).Days;

            var res = new CheckoutResponse
            {
                HotelData = new CheckoutHotelInfo
                {
                    Id = hotelRes.Data.Id,
                    Name = hotelRes.Data.Name
                },
                CheckIn = pars.CheckIn,
                CouponUsed = pars.couponCode,
                CouponValid = availRes.CouponValid,
                CouponDiscount = availRes.CouponDiscount,
                CheckOut = pars.CheckOut,
                CheckInTime = hotelRes.Data.Operation.CheckinTime,
                CheckOutTime = hotelRes.Data.Operation.CheckoutTime,
                Rooms = GetDistinctRoomsPerRate(availRes.Data?.Rooms)
            };

            if (!availRes.CoversRequest(SelectedRates))
            {
                return new PreparePaymentResponse
                {
                    ErrorCode = "Error",
                    ErrorMessage = "Not enough rooms"
                };
            }

            foreach (var selectedRate in SelectedRates)
            {
                bool found = false;
                foreach (var room in availRes.Data?.Rooms ?? [])
                {
                    foreach (var rate in room.Rates)
                    {
                        if (selectedRate.roomId != null && selectedRate.rateId == null)
                        {
                            selectedRate.rateId = selectedRate.roomId;
                        }
                        if (rate.Id.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true && rate.RemainingRooms >= selectedRate.count)
                        {
                            if (res.Rooms.FirstOrDefault(r => r.RateId.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true) is CheckoutRoomInfo cri)
                            {
                                cri.SelectedQuantity = selectedRate.count;
                            }
                            else
                                throw new InvalidOperationException("Rates don't exist any more");

                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException("Rates don't exist any more");
                }
            }

            res.MergePayments(SelectedRates);

            res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

            if (res.TotalPrice != pars.TotalPrice || (res.PartialPayment != null && (res.PartialPayment.prepayAmount != pars.PrepayAmount && res.TotalPrice != pars.PrepayAmount)))
            {
                throw new InvalidOperationException("Price has changed");
            }

            var payment = new VivaPaymentRequest
            {
                Amount = (int)(pars.PrepayAmount * 100),
                CustomerTrns = $"reservation for {parsedCheckin:dd_MM_yy}-{parsedCheckOut:dd_MM_yy} in {res.HotelData.Name}",
                Customer = new VivaCustomer
                {
                    CountryCode = "GR",
                    Email = pars.CustomerInfo?.Email ?? string.Empty,
                    FullName = $"{pars.CustomerInfo?.FirstName} {pars.CustomerInfo?.LastName}",
                    Phone = pars.CustomerInfo?.Phone ?? string.Empty
                },
                MerchantTrns = $"reservation for {parsedCheckin:dd_MM_yy}-{parsedCheckOut:dd_MM_yy} in {res.HotelData.Name}"
            };

            var orderCode = await viva.GetPaymentCode(payment);

            await repo.CreateTemporaryExternalReservation(res, pars, parsedCheckin, parsedCheckOut, payment, orderCode, party);

            PreparePaymentResponse response = new()
            {
                OrderCode = orderCode
            };
            return response;
        }

        private async Task<CheckoutResponse> ApplyCoupon(ReservationRequest reservationRequest, ReservationsRepository repo)
        {
            #region Param Validation

            if (!DateTime.TryParseExact(reservationRequest.reservationDetails.checkIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(reservationRequest.reservationDetails.checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            List<SelectedRate>? Selectedrates;
            if (string.IsNullOrWhiteSpace(reservationRequest.reservationDetails.selectedRates))
            {
                throw new InvalidCastException("Invalid selected rates");
            }
            else
            {
                Selectedrates = RatesToList(reservationRequest.reservationDetails.selectedRates);
                if (Selectedrates == null)
                {
                    throw new InvalidCastException("Invalid selected rates");
                }
            }

            var hotelInfo = reservationRequest.reservationDetails.hotelId?.Split('-');
            if (hotelInfo?.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            #endregion Param Validation

            SingleAvailabilityRequest availReq = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                PropertyId = hotelInfo[1]
            };

            var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
            var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(availReq, parsedCheckin, repo, Selectedrates, reservationRequest.couponCode);
            Task.WaitAll(availTask, hotelTask);

            SingleAvailabilityResponse? availRes = await availTask;
            HotelInfoResponse? hotelRes = await hotelTask;

            if (availRes.Data != null)
            {
                availRes.Data.Provider = Provider.WebHotelier;
            }

            hotelRes.Data.Provider = Provider.WebHotelier;

            int nights = (parsedCheckOut - parsedCheckin).Days;

            var res = new CheckoutResponse
            {
                ErrorCode = hotelRes.ErrorCode,
                CouponUsed = reservationRequest.couponCode,
                CouponDiscount = availRes.CouponDiscount,
                CouponValid = availRes.CouponValid,
                LabelErrorMessage = hotelRes.ErrorMsg,
                HotelData = new CheckoutHotelInfo
                {
                    Id = hotelRes.Data.Id,
                    Name = hotelRes.Data.Name,
                    Image = hotelRes.Data.LargePhotos.FirstOrDefault() ?? "",
                    Operation = hotelRes.Data.Operation,
                    Rating = hotelRes.Data.Rating
                },
                CheckIn = reservationRequest.reservationDetails.checkIn,
                CheckOut = reservationRequest.reservationDetails.checkOut,
                Nights = nights,
                Rooms = GetDistinctRoomsPerRate(availRes.Data?.Rooms),//TODO: ti kanei afto?
                SelectedPeople = GetPartyInfo(Selectedrates)
            };

            if (!availRes.CoversRequest(Selectedrates))
            {
                res.ErrorCode = "Error";
                res.ErrorMessage = "Not enough rooms";
                res.Rooms = new List<CheckoutRoomInfo>();
                return res;
            }

            //TODO: recheck this validation
            //i might need to add something with sums. cause when i have the same room for dif party and remaing is 1 i need error
            foreach (var selectedRate in Selectedrates)
            {
                bool found = false;
                foreach (var room in availRes.Data?.Rooms ?? [])
                {
                    foreach (var rate in room.Rates)
                    {
                        if (selectedRate.roomId != null && selectedRate.rateId == null)
                        {
                            selectedRate.rateId = selectedRate.roomId;
                        }

                        if (rate.Id.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true && rate.RemainingRooms >= selectedRate.count)
                        {
                            if (res.Rooms.FirstOrDefault(r => r.RateId.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true) is CheckoutRoomInfo cri)
                            {
                                cri.SelectedQuantity = selectedRate.count;
                            }
                            else
                                throw new InvalidOperationException("Rates don't exist any more");

                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException("Rates don't exist any more");
                }
            }
            res.MergePayments(Selectedrates);
            res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

            return res;
        }

        private async Task<CheckoutResponse> GetCheckoutInfo(SubmitSearchParameters pars)
        {
            #region Param Validation

            if (!DateTime.TryParseExact(pars.checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(pars.checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            List<SelectedRate>? Selectedrates;
            if (string.IsNullOrWhiteSpace(pars.selectedRates))
            {
                throw new InvalidCastException("Invalid selected rates");
            }
            else
            {
                Selectedrates = RatesToList(pars.selectedRates);
                if (Selectedrates == null)
                {
                    throw new InvalidCastException("Invalid selected rates");
                }
            }

            //string party;
            //if (string.IsNullOrWhiteSpace(pars.party))
            //{
            //    if (pars.adults == null || pars.adults < 1)
            //    {
            //        throw new ArgumentException("There must be at least one adult in the room.");
            //    }

            //    party = CreateParty(pars.adults.Value, pars.children);
            //}
            //else
            //{
            //    party = BuildMultiRoomJson(pars.party);
            //}

            var hotelInfo = pars.hotelId?.Split('-');
            if (hotelInfo?.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            #endregion Param Validation

            SingleAvailabilityRequest availReq = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                //Party = party,
                PropertyId = hotelInfo[1]
            };

            var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
            var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(availReq, parsedCheckin, null, Selectedrates);
            Task.WaitAll(availTask, hotelTask);

            SingleAvailabilityResponse? availRes = await availTask;
            HotelInfoResponse? hotelRes = await hotelTask;

            if (availRes.Data != null)
            {
                availRes.Data.Provider = Provider.WebHotelier;
            }

            hotelRes.Data.Provider = Provider.WebHotelier;

            int nights = (parsedCheckOut - parsedCheckin).Days;

            var res = new CheckoutResponse
            {
                ErrorCode = hotelRes.ErrorCode,
                LabelErrorMessage = hotelRes.ErrorMsg,
                CouponUsed = pars.couponCode,
                CouponValid = availRes.CouponValid,
                CouponDiscount = availRes.CouponDiscount,
                HotelData = new CheckoutHotelInfo
                {
                    Id = hotelRes.Data.Id,
                    Name = hotelRes.Data.Name,
                    Image = hotelRes.Data.LargePhotos.FirstOrDefault() ?? "",
                    Operation = hotelRes.Data.Operation,
                    Rating = hotelRes.Data.Rating
                },
                CheckIn = pars.checkin,
                CheckOut = pars.checkOut,
                Nights = nights,
                Rooms = GetDistinctRoomsPerRate(availRes.Data?.Rooms),//TODO: ti kanei afto?
                SelectedPeople = GetPartyInfo(Selectedrates)
            };

            if (!availRes.CoversRequest(Selectedrates))
            {
                res.ErrorCode = "Error";
                res.ErrorMessage = "Not enough rooms";
                res.Rooms = new List<CheckoutRoomInfo>();
                return res;
            }

            //TODO: recheck this validation
            //i might need to add something with sums. cause when i have the same room for dif party and remaing is 1 i need error
            foreach (var selectedRate in Selectedrates)
            {
                bool found = false;
                foreach (var room in availRes.Data?.Rooms ?? [])
                {
                    foreach (var rate in room.Rates)
                    {
                        if (selectedRate.roomId != null && selectedRate.rateId == null)
                        {
                            selectedRate.rateId = selectedRate.roomId;
                        }

                        if (rate.Id.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true && rate.RemainingRooms >= selectedRate.count)
                        {
                            if (res.Rooms.FirstOrDefault(r => r.RateId.Equals(selectedRate.rateId) && rate.SearchParty.party?.Equals(selectedRate.searchParty) == true) is CheckoutRoomInfo cri)
                            {
                                cri.SelectedQuantity = selectedRate.count;
                            }
                            else
                                throw new InvalidOperationException("Rates don't exist any more");

                            found = true;
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
                if (!found)
                {
                    throw new InvalidOperationException("Rates don't exist any more");
                }
            }
            res.MergePayments(Selectedrates);
            res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

            return res;
        }


        private static string GetPartyInfo(List<SelectedRate> selectedRates)
        {
            try
            {
                List<string> response = [];
                if (!selectedRates.IsNullOrEmpty())
                {
                    var adults = selectedRates!.Sum(p => p.adults * p.count);
                    var childs = selectedRates!.Sum(p => p.children * p.count);
                    if (adults > 0)
                        response.Add($"{adults} ενήλικες");
                    if (childs > 0)
                        response.Add($"{childs} παιδιά");
                    if (selectedRates!.Sum(r => r.count) == 1)
                        response.Add($"1 δωμάτιο");
                    else
                        response.Add($"{selectedRates!.Sum(r => r.count)} δωμάτια");
                }
                return string.Join(", ", response);
            }
            catch (Exception)
            {
                return "Σφάλμα";
            }
        }

        private static List<CheckoutRoomInfo> GetDistinctRoomsPerRate(IEnumerable<SingleHotelRoom>? rooms)
        {
            if (rooms.IsNullOrEmpty())
            {
                return [];
            }
            List<CheckoutRoomInfo> results = new();
            foreach (var room in rooms)
            {
                foreach (var rate in room.Rates)
                {
                    results.Add(new CheckoutRoomInfo
                    {
                        Type = room.Type,
                        RoomName = room.RoomName,
                        RateId = rate.Id,
                        SelectedQuantity = 0,
                        TotalPrice = rate.TotalPrice,
                        NetPrice = rate.NetPrice,
                        RateProperties = new CheckoutRateProperties
                        {
                            Board = rate.RateProperties.Board,
                            BoardId = rate.BoardType,
                            HasBoard = !NoboardIds.Contains(rate.BoardType ?? 0),
                            CancellationExpiry = rate.RateProperties.CancellationExpiry,
                            CancellationName = rate.RateProperties.CancellationName,
                            HasCancellation = rate.RateProperties.HasCancellation,
                            CancellationFees = rate.RateProperties.CancellationFees,
                            Payments = rate.RateProperties.Payments,
                            SearchParty = rate.SearchParty
                        }
                    });
                }
            }

            return results;
        }

        private static (string Description, object Example, bool Required) GetParameterDetails(string paramName)
        {
            var details = new Dictionary<string, (string Description, object Example, bool Required)>
            {
                { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "17/06/2025", true) },
                { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "20/06/2025", true) },
                //{ "adults", ("The number of adults for the search. (only if 1 room)", 2, false) },
                //{ "children", ("The ages of children, comma-separated (e.g., '5,10'). (only if 1 room)", "5,10", false) },
                //{ "party", ("Additional information about the party (required if more than 1 room. always wins).", "[{\"adults\":2,\"children\":[2,6]},{\"adults\":3}]", false) },
                { "hotelId", ("The id of the hotel", "1-VAROSVILL", true) },
                { "selectedRates", ("The selected rates", "[{\"rateId\":\"328000-226\",\"count\":1,\"roomType\":\"SUPFAM\"},{\"rateId\":\"273063-3\",\"count\":1,\"roomType\":\"EXEDBL\"}]", true) },
            };

            return details.TryGetValue(paramName, out (string Description, object Example, bool Required) value) ? value : ("No description available.", "N/A", false);
        }

        private static OpenApiOperation CustomizePreparePaymentOperation(OpenApiOperation operation)
        {
            if (operation.RequestBody?.Content.ContainsKey("application/json") == true)
            {
                operation.RequestBody.Content["application/json"].Example = new Microsoft.OpenApi.Any.OpenApiString(
                """
                {
                    "hotelId": "1-VAROSVILL",
                    "checkIn": "17/06/2025",
                    "checkOut": "20/06/2025",
                    "rooms": 1,
                    "children": "0",
                    "adults": 2,
                    "TotalPrice": 930,
                    "prepayAmount": 255.75,
                    "party": "[{\"adults\":2,\"children\":[2,6]},{\"adults\":3}]",
                    "selectedRates":"[{\"rateId\":\"328000-226\",\"count\":1,\"roomType\":\"SUPFAM\"},{\"rateId\":\"273063-3\",\"count\":1,\"roomType\":\"EXEDBL\"}]",
                    "customerInfo": {
                        "firstName": "akis",
                        "lastName": "pakis",
                        "email": "aa@aa.com",
                        "phone": "6977771645",
                        "requests": "dadadadadad"
                    }
                }
                """
                );
            }

            return operation;
        }

        private static OpenApiOperation CustomizePaymentFailedOperation(OpenApiOperation operation)
        {
            if (operation.RequestBody?.Content.ContainsKey("application/json") == true)
            {
                operation.RequestBody.Content["application/json"].Example = new Microsoft.OpenApi.Any.OpenApiString(
                """
                    {
                      "orderCode":"4918784106772600"
                    }
                    """
                );
            }

            return operation;
        }

        private static OpenApiOperation CustomizePaymentSucceedOperation(OpenApiOperation operation)
        {
            if (operation.RequestBody?.Content.ContainsKey("application/json") == true)
            {
                operation.RequestBody.Content["application/json"].Example = new Microsoft.OpenApi.Any.OpenApiString(
                """
                    {
                        "tid":"dc90abcc-0350-4383-a624-5821811aedb9",
                        "orderCode":"7224745916872609"
                    }
                    """
                );
            }

            return operation;
        }

        private static OpenApiOperation CustomizeCheckoutOperation(OpenApiOperation operation)
        {
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                // Dynamically extract properties from the record
                var recordProperties = typeof(SubmitSearchParameters).GetProperties();

                foreach (var property in recordProperties)
                {
                    var param = operation.Parameters.FirstOrDefault(p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                    if (param != null)
                    {
                        var queryAttr = property.GetCustomAttribute<FromQueryAttribute>();
                        var paramName = queryAttr?.Name ?? property.Name; // Use query parameter name if provided

                        // Provide descriptions and examples dynamically
                        var details = GetParameterDetails(paramName);

                        param.Description = details.Description;
                        param.Example = new Microsoft.OpenApi.Any.OpenApiString(details.Example?.ToString() ?? "");
                        param.Required = details.Required;
                    }
                }
            }

            // Add response descriptions
            operation.Responses.TryAdd("400", new OpenApiResponse
            {
                Description = "Bad request. One or more parameters are invalid or missing."
            });

            operation.Responses.TryAdd("500", new OpenApiResponse
            {
                Description = "Internal server error. Something went wrong on the server."
            });

            return operation;
        }

        // Helper function for parameter details
    }
}