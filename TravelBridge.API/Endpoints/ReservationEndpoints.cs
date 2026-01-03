using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.Contracts.Contracts.Responses;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Payments.Viva.Models.ExternalModels;
using TravelBridge.Payments.Viva.Services.Viva;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Endpoints
{
    public class ReservationEndpoints
    {
        private readonly WebHotelierPropertiesService webHotelierPropertiesService;
        private readonly ILogger<ReservationEndpoints> _logger;

        public ReservationEndpoints(WebHotelierPropertiesService webHotelierPropertiesService, ILogger<ReservationEndpoints> logger)
        {
            this.webHotelierPropertiesService = webHotelierPropertiesService;
            _logger = logger;
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
            _logger.LogInformation("CancelBooking started for OrderCode: {OrderCode}", OrderCode);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var reservation = await repo.GetReservationBasicDataByPaymentCode(OrderCode);
                if (reservation == null)
                {
                    _logger.LogWarning("CancelBooking failed: Reservation not found for OrderCode: {OrderCode}", OrderCode);
                    throw new InvalidOperationException("Reservation not found");
                }

                _logger.LogInformation("CancelBooking: Found reservation {ReservationId} for OrderCode: {OrderCode}", reservation.Id, OrderCode);
                await webHotelierPropertiesService.CancelBooking(reservation, repo);

                stopwatch.Stop();
                _logger.LogInformation("CancelBooking completed for OrderCode: {OrderCode}, ReservationId: {ReservationId} in {ElapsedMs}ms", 
                    OrderCode, reservation.Id, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "CancelBooking failed for OrderCode: {OrderCode} after {ElapsedMs}ms", 
                    OrderCode, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<CheckoutResponse> GetOrderInfo(PaymentInfo pay, ReservationsRepository repo)
        {
            _logger.LogInformation("GetOrderInfo started for OrderCode: {OrderCode}", pay.OrderCode);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(pay.OrderCode))
                {
                    _logger.LogWarning("GetOrderInfo failed: Invalid order code");
                    throw new ArgumentException("Invalid order code");
                }

                var reservation = await repo.GetFullReservationFromPaymentCode(pay.OrderCode)
                    ?? throw new InvalidOperationException("Reservation not found");

                _logger.LogInformation("GetOrderInfo: Found reservation {ReservationId} for OrderCode: {OrderCode}", reservation.Id, pay.OrderCode);

                var payment = reservation.Payments.FirstOrDefault(p => p.OrderCode == pay.OrderCode)
                    ?? throw new InvalidOperationException("Payment not found");

                _logger.LogInformation("GetOrderInfo: Updating payment status to failed for PaymentId: {PaymentId}", payment.Id);
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

                stopwatch.Stop();
                _logger.LogInformation("GetOrderInfo completed for OrderCode: {OrderCode}, ReservationId: {ReservationId} in {ElapsedMs}ms", 
                    pay.OrderCode, reservation.Id, stopwatch.ElapsedMilliseconds);

                return res;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetOrderInfo failed for OrderCode: {OrderCode} after {ElapsedMs}ms", 
                    pay.OrderCode, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<SuccessfulPaymentResponse> ConfirmPayment(PaymentInfo pay, ReservationsRepository repo, VivaService viva)
        {
            _logger.LogInformation("ConfirmPayment started for OrderCode: {OrderCode}, Tid: {Tid}", pay.OrderCode, pay.Tid);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(pay.OrderCode) || string.IsNullOrWhiteSpace(pay.Tid))
                {
                    _logger.LogWarning("ConfirmPayment failed: Invalid payment info - OrderCode: {OrderCode}, Tid: {Tid}", pay.OrderCode, pay.Tid);
                    throw new ArgumentException("Invalid payment info");
                }

                var reservation = await repo.GetReservationBasicDataByPaymentCode(pay.OrderCode);
                if (reservation == null)
                {
                    _logger.LogWarning("ConfirmPayment failed: Reservation not found for OrderCode: {OrderCode}", pay.OrderCode);
                    return new SuccessfulPaymentResponse(error: "Reservation not found", "NO_RES");
                }

                _logger.LogInformation("ConfirmPayment: Found reservation {ReservationId}, TotalAmount: {TotalAmount}, PrepayAmount: {PrepayAmount}", 
                    reservation.Id, reservation.TotalAmount, reservation.PartialPayment?.prepayAmount);

                try
                {
                    _logger.LogDebug("ConfirmPayment: Validating payment with Viva for OrderCode: {OrderCode}", pay.OrderCode);
                    if (await viva.ValidatePayment(pay.OrderCode, pay.Tid, reservation.TotalAmount, reservation.PartialPayment?.prepayAmount) && await repo.UpdatePaymentSucceed(pay.OrderCode, pay.Tid))
                    {
                        _logger.LogInformation("ConfirmPayment: Payment validated successfully for OrderCode: {OrderCode}, creating booking", pay.OrderCode);
                        await webHotelierPropertiesService.CreateBooking(reservation, repo);

                        stopwatch.Stop();
                        _logger.LogInformation("ConfirmPayment completed successfully for OrderCode: {OrderCode}, ReservationId: {ReservationId} in {ElapsedMs}ms", 
                            pay.OrderCode, reservation.Id, stopwatch.ElapsedMilliseconds);

                        return new SuccessfulPaymentResponse
                        {
                            SuccessfulPayment = true,
                            Data = new DataSuccess
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
                        stopwatch.Stop();
                        _logger.LogWarning("ConfirmPayment: Payment validation failed for OrderCode: {OrderCode}, ReservationId: {ReservationId} in {ElapsedMs}ms", 
                            pay.OrderCode, reservation.Id, stopwatch.ElapsedMilliseconds);
                        return new SuccessfulPaymentResponse(error: $"Υπήρξε πρόβλημα με την πληρωμή της κράτησής σας με αριθμό {reservation.Id}, παρακαλώ επικοινωνήστε μαζί μας.", "RES_ERROR");
                    }
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "ConfirmPayment: Exception during payment validation/booking for OrderCode: {OrderCode}, ReservationId: {ReservationId} in {ElapsedMs}ms", 
                        pay.OrderCode, reservation.Id, stopwatch.ElapsedMilliseconds);
                    return new SuccessfulPaymentResponse(error: $"Υπήρξε πρόβλημα με την πληρωμή της κράτησής σας με αριθμό {reservation.Id}, παρακαλώ επικοινωνήστε μαζί μας.", "RES_ERROR");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "ConfirmPayment failed for OrderCode: {OrderCode} after {ElapsedMs}ms", 
                    pay.OrderCode, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<PreparePaymentResponse> PreparePayment(BookingRequest pars, ReservationsRepository repo, VivaService viva)
        {
            _logger.LogInformation("PreparePayment started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, TotalPrice: {TotalPrice}, PrepayAmount: {PrepayAmount}", 
                pars.HotelId, pars.CheckIn, pars.CheckOut, pars.TotalPrice, pars.PrepayAmount);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Param Validation

                if (!DateTime.TryParseExact(pars.CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("PreparePayment failed: Invalid checkin date format {CheckIn}", pars.CheckIn);
                    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
                }

                if (!DateTime.TryParseExact(pars.CheckOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("PreparePayment failed: Invalid checkout date format {CheckOut}", pars.CheckOut);
                    throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
                }

                List<SelectedRate>? SelectedRates;
                if (string.IsNullOrWhiteSpace(pars.SelectedRates))
                {
                    _logger.LogWarning("PreparePayment failed: Invalid selected rates");
                    throw new InvalidCastException("Invalid selected rates");
                }
                else
                {
                    SelectedRates = RatesToList(pars.SelectedRates);
                    if (SelectedRates == null)
                    {
                        _logger.LogWarning("PreparePayment failed: Could not parse selected rates");
                        throw new InvalidCastException("Invalid selected rates");
                    }
                }

                _logger.LogDebug("PreparePayment: Parsed {RateCount} selected rates", SelectedRates.Count);

                string party;
                if (string.IsNullOrWhiteSpace(pars.Party))
                {
                    if (pars.Adults == null || pars.Adults < 1)
                    {
                        _logger.LogWarning("PreparePayment failed: At least one adult required, Adults: {Adults}", pars.Adults);
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
                    _logger.LogWarning("PreparePayment failed: Invalid hotelId format {HotelId}", pars.HotelId);
                    throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
                }

                #endregion Param Validation

                _logger.LogDebug("PreparePayment: Fetching hotel info and availability for HotelId: {HotelId}", pars.HotelId);

                WHSingleAvailabilityRequest whReq = new()
                {
                    PropertyId = hotelInfo[1],
                    CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                    CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                    Party = party
                };

                var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
                var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(whReq, parsedCheckin, repo, SelectedRates, pars.couponCode);
                await Task.WhenAll(availTask, hotelTask);

                SingleAvailabilityResponse? availRes = await availTask;
                WHHotelInfoResponse? hotelRes = await hotelTask;

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

                if (!AvailabilityProcessor.HasSufficientAvailability(availRes, SelectedRates))
                {
                    _logger.LogWarning("PreparePayment failed: Not enough rooms available for HotelId: {HotelId}", pars.HotelId);
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
                                {
                                    _logger.LogWarning("PreparePayment failed: Rates don't exist anymore for RateId: {RateId}", selectedRate.rateId);
                                    throw new InvalidOperationException("Rates don't exist any more");
                                }

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
                        _logger.LogWarning("PreparePayment failed: Rate not found - RateId: {RateId}", selectedRate.rateId);
                        throw new InvalidOperationException("Rates don't exist any more");
                    }
                }

                CheckoutProcessor.CalculatePayments(res);

                res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

                if (res.TotalPrice != pars.TotalPrice || (res.PartialPayment != null && (res.PartialPayment.prepayAmount != pars.PrepayAmount && res.TotalPrice != pars.PrepayAmount)))
                {
                    _logger.LogWarning("PreparePayment failed: Price has changed. Expected: {ExpectedTotal}/{ExpectedPrepay}, Got: {ActualTotal}/{ActualPrepay}", 
                        pars.TotalPrice, pars.PrepayAmount, res.TotalPrice, res.PartialPayment?.prepayAmount);
                    throw new InvalidOperationException("Price has changed");
                }

                _logger.LogInformation("PreparePayment: Creating Viva payment for Amount: {Amount}", pars.PrepayAmount);

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
                _logger.LogInformation("PreparePayment: Received OrderCode: {OrderCode} from Viva", orderCode);

                await repo.CreateTemporaryExternalReservation(res, pars, parsedCheckin, parsedCheckOut, payment, orderCode, party);

                stopwatch.Stop();
                _logger.LogInformation("PreparePayment completed for HotelId: {HotelId}, OrderCode: {OrderCode} in {ElapsedMs}ms", 
                    pars.HotelId, orderCode, stopwatch.ElapsedMilliseconds);

                PreparePaymentResponse response = new()
                {
                    OrderCode = orderCode
                };
                return response;
            }
            catch (Exception ex) when (ex is not InvalidCastException && ex is not ArgumentException && ex is not InvalidOperationException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "PreparePayment failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    pars.HotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<CheckoutResponse> ApplyCoupon(ReservationRequest reservationRequest, ReservationsRepository repo)
        {
            _logger.LogInformation("ApplyCoupon started for HotelId: {HotelId}, CouponCode: {CouponCode}", 
                reservationRequest.reservationDetails.hotelId, reservationRequest.couponCode);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Param Validation

                if (!DateTime.TryParseExact(reservationRequest.reservationDetails.checkIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("ApplyCoupon failed: Invalid checkin date format");
                    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
                }

                if (!DateTime.TryParseExact(reservationRequest.reservationDetails.checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("ApplyCoupon failed: Invalid checkout date format");
                    throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
                }

                List<SelectedRate>? Selectedrates;
                if (string.IsNullOrWhiteSpace(reservationRequest.reservationDetails.selectedRates))
                {
                    _logger.LogWarning("ApplyCoupon failed: Invalid selected rates");
                    throw new InvalidCastException("Invalid selected rates");
                }
                else
                {
                    Selectedrates = RatesToList(reservationRequest.reservationDetails.selectedRates);
                    if (Selectedrates == null)
                    {
                        _logger.LogWarning("ApplyCoupon failed: Could not parse selected rates");
                        throw new InvalidCastException("Invalid selected rates");
                    }
                }

                var hotelInfo = reservationRequest.reservationDetails.hotelId?.Split('-');
                if (hotelInfo?.Length != 2)
                {
                    _logger.LogWarning("ApplyCoupon failed: Invalid hotelId format");
                    throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
                }

                #endregion Param Validation

                _logger.LogDebug("ApplyCoupon: Fetching hotel info and availability");

                WHSingleAvailabilityRequest whReq = new()
                {
                    PropertyId = hotelInfo[1],
                    CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                    CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                    Party = null
                };

                var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
                var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(whReq, parsedCheckin, repo, Selectedrates,reservationRequest.couponCode);
                await Task.WhenAll(availTask, hotelTask);

                SingleAvailabilityResponse? availRes = await availTask;
                WHHotelInfoResponse? hotelRes = await hotelTask;
                var hotelData = hotelRes.Data!.ToContracts();

                if (availRes.Data != null)
                {
                    availRes.Data.Provider = Provider.WebHotelier;
                }

                hotelData.Provider = Provider.WebHotelier;

                int nights = (parsedCheckOut - parsedCheckin).Days;

                var res = new CheckoutResponse
                {
                    ErrorCode = hotelRes.ErrorCode,
                    CouponUsed = reservationRequest.couponCode,
                    CouponDiscount = availRes.CouponDiscount,
                    CouponValid = availRes.CouponValid,
                    LabelErrorMessage = hotelRes.ErrorMessage,
                    HotelData = new CheckoutHotelInfo
                    {
                        Id = hotelData.Id,
                        Name = hotelData.Name,
                        Image = hotelData.LargePhotos.FirstOrDefault() ?? "",
                        Operation = hotelData.Operation,
                        Rating = hotelData.Rating
                    },
                    CheckIn = reservationRequest.reservationDetails.checkIn,
                    CheckOut = reservationRequest.reservationDetails.checkOut,
                    Nights = nights,
                    Rooms = GetDistinctRoomsPerRate(availRes.Data?.Rooms),
                    SelectedPeople = GetPartyInfo(Selectedrates)
                };

                _logger.LogInformation("ApplyCoupon: CouponValid: {CouponValid}, CouponDiscount: {CouponDiscount}", 
                    availRes.CouponValid, availRes.CouponDiscount);

                if (!AvailabilityProcessor.HasSufficientAvailability(availRes, Selectedrates))
                {
                    _logger.LogWarning("ApplyCoupon: Not enough rooms available");
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
                CheckoutProcessor.CalculatePayments(res);
                res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

                stopwatch.Stop();
                _logger.LogInformation("ApplyCoupon completed for HotelId: {HotelId}, TotalPrice: {TotalPrice} in {ElapsedMs}ms", 
                    reservationRequest.reservationDetails.hotelId, res.TotalPrice, stopwatch.ElapsedMilliseconds);

                return res;
            }
            catch (Exception ex) when (ex is not InvalidCastException && ex is not ArgumentException && ex is not InvalidOperationException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "ApplyCoupon failed after {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<CheckoutResponse> GetCheckoutInfo(SubmitSearchParameters pars)
        {
            _logger.LogInformation("GetCheckoutInfo started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}", 
                pars.hotelId, pars.checkin, pars.checkOut);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Param Validation

                if (!DateTime.TryParseExact(pars.checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("GetCheckoutInfo failed: Invalid checkin date format");
                    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
                }

                if (!DateTime.TryParseExact(pars.checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("GetCheckoutInfo failed: Invalid checkout date format");
                    throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
                }

                List<SelectedRate>? Selectedrates;
                if (string.IsNullOrWhiteSpace(pars.selectedRates))
                {
                    _logger.LogWarning("GetCheckoutInfo failed: Invalid selected rates");
                    throw new InvalidCastException("Invalid selected rates");
                }
                else
                {
                    Selectedrates = RatesToList(pars.selectedRates);
                    if (Selectedrates == null)
                    {
                        _logger.LogWarning("GetCheckoutInfo failed: Could not parse selected rates");
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
                    _logger.LogWarning("GetCheckoutInfo failed: Invalid hotelId format");
                    throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
                }

                #endregion Param Validation

                _logger.LogDebug("GetCheckoutInfo: Fetching hotel info and availability");

                WHSingleAvailabilityRequest whReq = new()
                {
                    PropertyId = hotelInfo[1],
                    CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                    CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                    Party = null
                };

                var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
                var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(whReq, parsedCheckin, null, Selectedrates);
                await Task.WhenAll(availTask, hotelTask);

                SingleAvailabilityResponse? availRes = await availTask;
                WHHotelInfoResponse? hotelRes = await hotelTask;
                var hotelData = hotelRes.Data!.ToContracts();

                if (availRes.Data != null)
                {
                    availRes.Data.Provider = Provider.WebHotelier;
                }

                hotelData.Provider = Provider.WebHotelier;

                int nights = (parsedCheckOut - parsedCheckin).Days;

                var res = new CheckoutResponse
                {
                    ErrorCode = hotelRes.ErrorCode,
                    LabelErrorMessage = hotelRes.ErrorMessage,
                    CouponUsed = pars.couponCode,
                    CouponValid = availRes.CouponValid,
                    CouponDiscount = availRes.CouponDiscount,
                    HotelData = new CheckoutHotelInfo
                    {
                        Id = hotelData.Id,
                        Name = hotelData.Name,
                        Image = hotelData.LargePhotos.FirstOrDefault() ?? "",
                        Operation = hotelData.Operation,
                        Rating = hotelData.Rating
                    },
                    CheckIn = pars.checkin,
                    CheckOut = pars.checkOut,
                    Nights = nights,
                    Rooms = GetDistinctRoomsPerRate(availRes.Data?.Rooms),//TODO: ti kanei afto?
                    SelectedPeople = GetPartyInfo(Selectedrates)
                };

                if (!AvailabilityProcessor.HasSufficientAvailability(availRes, Selectedrates))
                {
                    _logger.LogWarning("GetCheckoutInfo: Not enough rooms available for HotelId: {HotelId}", pars.hotelId);
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
                                {
                                    _logger.LogWarning("GetCheckoutInfo: Rates don't exist anymore for RateId: {RateId}", selectedRate.rateId);
                                    throw new InvalidOperationException("Rates don't exist any more");
                                }

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
                        _logger.LogWarning("GetCheckoutInfo: Rate not found - RateId: {RateId}", selectedRate.rateId);
                        throw new InvalidOperationException("Rates don't exist any more");
                    }
                }
            
                CheckoutProcessor.CalculatePayments(res);
                res.TotalPrice = res.Rooms.Sum(r => (r.TotalPrice * r.SelectedQuantity));

                stopwatch.Stop();
                _logger.LogInformation("GetCheckoutInfo completed for HotelId: {HotelId}, TotalPrice: {TotalPrice}, RoomsCount: {RoomsCount} in {ElapsedMs}ms", 
                    pars.hotelId, res.TotalPrice, res.Rooms.Count, stopwatch.ElapsedMilliseconds);

                return res;
            }
            catch (Exception ex) when (ex is not InvalidCastException && ex is not ArgumentException && ex is not InvalidOperationException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetCheckoutInfo failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    pars.hotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
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
                { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "17/03/2026", true) },
                { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "20/03/2026", true) },
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
                    "checkIn": "17/03/2026",
                    "checkOut": "20/03/2026",
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