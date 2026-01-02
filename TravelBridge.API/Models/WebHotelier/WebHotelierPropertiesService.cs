using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection;
using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Models.DB;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.Providers.WebHotelier.Models.Common;
using TravelBridge.Providers.WebHotelier.Models.Hotel;
using TravelBridge.Providers.WebHotelier.Models.Rate;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Models.WebHotelier
{
    public class WebHotelierPropertiesService
    {
        private readonly WebHotelierClient _whClient;
        private readonly SmtpEmailSender _mailSender;

        public WebHotelierPropertiesService(WebHotelierClient whClient, SmtpEmailSender mailSender)
        {
            _whClient = whClient;
            _mailSender = mailSender;
        }

        /// <summary>
        /// Search for properties by name in WebHotelier
        /// </summary>
        /// <returns>Array of WHHotel objects from WebHotelier API</returns>
        public async Task<WHHotel[]> SearchPropertyFromWebHotelierAsync(string propertyName)
        {
            try
            {
                return await _whClient.SearchPropertiesAsync(propertyName);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(ex.ToString());
            }
        }

        /// <summary>
        /// Get all properties from WebHotelier
        /// </summary>
        /// <returns>Array of WHHotel objects from WebHotelier API</returns>
        public async Task<WHHotel[]> GetAllPropertiesFromWebHotelierAsync()
        {
            try
            {
                return await _whClient.GetAllPropertiesAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(ex.ToString());
            }
        }

        public async Task<PluginSearchResponse> GetAvailabilityAsync(WHAvailabilityRequest request)
        {
            try
            {
                List<WHPartyItem> partyList = GetPartyList(request.Party);

                if (partyList.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API. Invalid parties");
                }

                Dictionary<WHPartyItem, Task<WHMultiAvailabilityResponse?>> tasks = new();
                foreach (var partyItem in partyList ?? new())
                {
                    tasks.Add(partyItem, _whClient.GetAvailabilityAsync(request, partyItem.party!));
                }

                await Task.WhenAll(tasks.Values);

                Dictionary<WHPartyItem, WHMultiAvailabilityResponse> respones = new();

                foreach (var task in tasks)
                {
                    var res = await task.Value;
                    if (res == null)
                    {
                        return new PluginSearchResponse();
                    }
                    WHProvider Provider = WHProvider.WebHotelier;
                    // Deserialize the response JSON
                    foreach (WHWebHotel hotel in res.Data?.Hotels ?? [])
                    {
                        hotel.Id = $"{(int)Provider}-{hotel.Code}";
                        hotel.SearchParty = task.Key;
                        var contractsHotel = hotel.ToContracts();
                        hotel.MinPrice = Math.Floor(contractsHotel.GetMinPrice(out decimal salePrice));
                        hotel.MinPricePerDay = Math.Floor((hotel.MinPrice ?? 0) / (int)(DateTime.Parse(request.CheckOut) - DateTime.Parse(request.CheckIn)).TotalDays);
                        if (salePrice >= hotel.MinPrice + 5)
                            hotel.SalePrice = salePrice;
                        else
                            hotel.SalePrice = hotel.MinPrice;

                        foreach (var rate in hotel.Rates)
                        {
                            rate.SearchParty = task.Key;
                        }
                    }

                    respones.Add(task.Key, res);
                }

                var MergedRooms = MergeResponses(respones);
                if (MergedRooms == null || MergedRooms.Results.IsNullOrEmpty())
                {
                    return new PluginSearchResponse();
                }
                MergedRooms.Results = MergedRooms.CoverRequest(partyList.ToContracts());
                return MergedRooms;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(
            WHSingleAvailabilityRequest request,
            DateTime checkinDate,
            ReservationsRepository? reservationsRepository,
            List<SelectedRate>? rates = null,
            string? couponCode = null)
        {
            try
            {
                rates?.ForEach(r => r.FillPartyFromId());

                List<WHPartyItem> partyList = rates == null ? GetPartyList(request.Party) : GetPartyList(rates);

                if (partyList.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API. Invalid parties");
                }

                Dictionary<WHPartyItem, Task<WHSingleAvailabilityData?>> tasks = new();
                foreach (var partyItem in partyList ?? new())
                {
                    tasks.Add(partyItem, _whClient.GetSingleAvailabilityAsync(request.PropertyId, request.CheckIn, request.CheckOut, partyItem.party!));
                }

                await Task.WhenAll(tasks.Values);

                WHSingleAvailabilityData? finalRes = null;
                foreach (var task in tasks.OrderBy(p => p.Key.adults))
                {
                    var res = await task.Value ?? throw new InvalidOperationException("No Results");
                    if (res == null)
                    {
                        return new SingleAvailabilityResponse { ErrorMessage = "No response" };
                    }
                    finalRes?.Data?.Rates.AddRange(res.Data?.Rates ?? new List<WHHotelRate>());
                    finalRes ??= res;

                    WHProvider Provider = WHProvider.WebHotelier;
                    foreach (var rate in res.Data?.Rates ?? [])
                    {
                        rate.SearchParty = task.Key;
                        rate.Id += $"-{task.Key.adults}{(task.Key.children?.Length > 0 ? ("_" + string.Join("_", task.Key.children)) : "")}";
                    }
                }

                if (rates != null)
                {
                    rates.ForEach(selectedRate => selectedRate.rateId = (selectedRate.roomId != null && selectedRate.rateId == null) ? selectedRate.roomId : selectedRate.rateId);
                    finalRes!.Data!.Rates = finalRes.Data.Rates
                        .Where(r => rates.Any(sr => sr.rateId.Equals(r.Id.ToString()) && sr.count > 0)).ToList();

                    if (!finalRes.CoversRequest(partyList))
                    {
                        return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Not enough rooms", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
                    }
                }
                else if (finalRes?.Data?.Rates.IsNullOrEmpty() != false)
                {
                    finalRes!.Alternatives = await GetAlternatives(partyList, request.PropertyId, request.CheckIn, request.CheckOut);
                }

                decimal disc = 0;
                CouponType couponType = CouponType.none;
                if (reservationsRepository != null && !string.IsNullOrWhiteSpace(couponCode))
                {
                    var coupon = await reservationsRepository.RetrieveCoupon(couponCode.ToUpper());
                    if (coupon != null && coupon.CouponType == CouponType.percentage)
                    {
                        disc = coupon.Percentage / 100m;
                        couponType = CouponType.percentage;
                    }
                    else if (coupon != null && coupon.CouponType == CouponType.flat)
                    {
                        disc = coupon.Amount;
                        couponType = CouponType.flat;
                    }
                }

                if (finalRes != null && finalRes.Data == null)
                {
                    finalRes.Data = new WHHotelInfo
                    {
                        Code = request.PropertyId,
                        Provider = WHProvider.WebHotelier,
                        Rates = []
                    };
                }

                return finalRes?.MapToResponse(checkinDate, disc, couponType)
                    ?? new SingleAvailabilityResponse { ErrorCode = "Empty", ErrorMessage = "No records found", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
            catch (HttpRequestException ex)
            {
                return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Internal Error", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
        }

        private async Task<List<WHAlternative>> GetAlternatives(List<WHPartyItem>? partyList, string propertyId, string checkIn, string checkOut)
        {
            var from = DateTime.Parse(checkIn).AddDays(-14);
            var to = DateTime.Parse(checkOut).AddDays(14);

            if (from < DateTime.Today.AddDays(1))
            {
                from = DateTime.Today.AddDays(1);
            }

            Dictionary<WHPartyItem, Task<WHAlternativeDaysData?>> tasks = new();
            foreach (var partyItem in partyList ?? new())
            {
                tasks.Add(partyItem, _whClient.GetFlexibleCalendarAsync(propertyId, partyItem.party!, from, to));
            }

            await Task.WhenAll(tasks.Values);
            
            Dictionary<WHPartyItem, List<WHAlternative>> alterDatesDict = new();
            foreach (var task in tasks.OrderBy(p => p.Key.adults))
            {
                var res = await task.Value ?? throw new InvalidOperationException("No Results");
                if (res == null)
                {
                    return [];
                }
                var alterDates = res.Data?.days?.Where(d => d.status == "AVL" || d.status == "MIN") ?? [];
                alterDatesDict.Add(task.Key, GetAlterDates(alterDates, checkIn, checkOut));
            }

            return KeepCommon(alterDatesDict);
        }

        private static List<WHAlternative> KeepCommon(Dictionary<WHPartyItem, List<WHAlternative>> alterDatesDict)
        {
            var allCheckInOutPairs = alterDatesDict.Values
             .Select(list => list
                 .Select(a => (a.CheckIn, a.Checkout))
                 .Distinct()
                 .ToHashSet())
             .ToList();

            // Find common (CheckIn, CheckOut) pairs in all lists
            var commonPairs = allCheckInOutPairs
                .Skip(1)
                .Aggregate(new HashSet<(DateTime, DateTime)>(allCheckInOutPairs.First()),
                           (h, s) => { h.IntersectWith(s); return h; });

            // Filter and flatten all matching alternatives
            var matchingAlternatives = alterDatesDict.Values
                .SelectMany(list => list)
                .Where(a => commonPairs.Contains((a.CheckIn, a.Checkout)));

            // Group by date and sum
            return matchingAlternatives
                .GroupBy(a => new { a.CheckIn, a.Checkout })
                .Select(g => new WHAlternative
                {
                    CheckIn = g.Key.CheckIn,
                    Checkout = g.Key.Checkout,
                    MinPrice = g.Sum(x => x.MinPrice),
                    NetPrice = g.Sum(x => x.NetPrice),
                    Nights = (g.Key.Checkout - g.Key.CheckIn).Days
                })
                .OrderBy(a => a.CheckIn)
                .ToList();
        }

        private static List<WHAlternative> GetAlterDates(IEnumerable<WHAlternativeDayInfo> alterDates, string checkIn, string checkOut)
        {
            try
            {
                if (!alterDates.Any())
                {
                    return [];
                }
                List<WHAlternative> results = new List<WHAlternative>();
                foreach (var dat in alterDates)
                {
                    dat.dateOnly = DateTime.ParseExact(dat.date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                int requestedNights = (DateTime.Parse(checkOut) - DateTime.Parse(checkIn)).Days;
                var maxDay = alterDates.Max(a => a.dateOnly).AddDays(1);
                for (int i = 0; i < alterDates.Count(); i++)
                {
                    var curDate = alterDates.ElementAt(i);
                    var duration = requestedNights;
                    if (curDate.min_stay > requestedNights)
                    {
                        duration = curDate.min_stay;
                    }
                    WHAlternative alt = new WHAlternative()
                    {
                        CheckIn = curDate.dateOnly,
                        Checkout = curDate.dateOnly.AddDays(duration),
                        Nights = duration
                    };
                    if (curDate.dateOnly.AddDays(duration) > maxDay)
                    {
                        continue; // skip if the duration exceeds the max day available
                    }

                    var tempDate = curDate.dateOnly;
                    var tempItem = curDate;
                    bool isOk = true;
                    for (int j = 1; j <= duration; j++)
                    {
                        if (tempItem.min_stay > duration || tempItem.dateOnly != tempDate)
                        {
                            isOk = false;
                            break;
                        }
                        alt.MinPrice += tempItem.retail;
                        alt.NetPrice += tempItem.price;
                        tempItem = (i + j) >= alterDates.Count() ? null : alterDates.ElementAt(i + j);
                        tempDate = tempDate.AddDays(1);
                    }
                    if (isOk)
                    {
                        results.Add(alt);
                    }

                }
                return results;
            }
            catch (Exception ex)
            {
                return [];
            }
        }

        private static List<WHPartyItem> GetPartyList(string party)
        {
            return JsonSerializer.Deserialize<List<WHPartyItem>>(party)!.GroupBy(g => new { g.adults, Children = g.children != null ? string.Join(",", g.children) : "" })
                    .Select(g => new WHPartyItem
                    {
                        adults = g.Key.adults,
                        children = g.First().children,
                        RoomsCount = g.Count(),
                        party = JsonSerializer.Serialize(new List<WHPartyItem> { new WHPartyItem { adults = g.Key.adults, children = g.First().children } })
                    }).ToList();
        }

        private static List<WHPartyItem> GetPartyList(List<SelectedRate> rates)
        {
            var partyList = new List<WHPartyItem>();

            foreach (var selectedRate in rates.GroupBy(g => g.searchParty))
            {
                var item = JsonSerializer.Deserialize<WHPartyItem>(selectedRate.Key.TrimStart('[').TrimEnd(']')) ?? throw new InvalidDataException("Invalid Party");
                item.RoomsCount = selectedRate.Count();
                item.party = selectedRate.Key;
                partyList.Add(item);
            }
            return partyList;
        }

        private static PluginSearchResponse MergeResponses(Dictionary<WHPartyItem, WHMultiAvailabilityResponse> respones)
        {
            try
            {
                if (respones.Count == 0)
                {
                    throw new InvalidOperationException($"No results");
                }

                if (respones.Count == 1)
                {
                    return new PluginSearchResponse
                    {
                        Results = respones.First().Value?.Data?.Hotels?.ToContracts()
                        .Where(h => !string.IsNullOrWhiteSpace(h.PhotoL) && h.MinPrice > 0)
                        ?? []
                    };
                }

                if (respones.Any(r => r.Value?.Data?.Hotels?.Any() != true))
                {
                    throw new InvalidOperationException($"No results");
                }

                //keep only hotels that exist in all results
                var GroupedHotels = respones.SelectMany(h => h.Value.Data!.Hotels)
                     .GroupBy(h => h.Id).Where(h => h.Count() == respones.Count).ToList()
                     ?? throw new InvalidOperationException($"No results");

                List<WebHotel> FinalObjects = new();
                //step 3: merge hotels
                foreach (var hotels in GroupedHotels)
                {
                    var temp = hotels.OrderBy(h => h.SearchParty?.adults ?? 0).FirstOrDefault();
                    if (temp == null)
                    {
                        continue;
                    }
                    var contractsHotel = temp.ToContracts();
                    contractsHotel.Rates = hotels.SelectMany(h => h.Rates.Select(r => r.ToContracts())).ToList();
                    contractsHotel.MinPrice = hotels.Sum(h => h.MinPrice * h.SearchParty?.RoomsCount ?? 1);
                    contractsHotel.MinPricePerDay = hotels.Sum(h => h.MinPricePerDay * h.SearchParty?.RoomsCount ?? 1);
                    contractsHotel.SalePrice = hotels.Sum(h => h.SalePrice * h.SearchParty?.RoomsCount ?? 1);

                    FinalObjects.Add(contractsHotel);
                }

                return new PluginSearchResponse
                {
                    Results = FinalObjects?
                       .Where(h => !string.IsNullOrWhiteSpace(h.PhotoL) && h.MinPrice > 0)
                       ?? []
                };
            }
            catch (Exception ex)
            {
                //TODO: log error
                return new PluginSearchResponse();
            }
        }

        internal async Task<WHHotelInfoResponse> GetHotelInfo(string hotelId)
        {
            try
            {
                var res = await _whClient.GetHotelInfoAsync(hotelId) ?? throw new InvalidOperationException("Hotel not Found");
                res.Data!.LargePhotos = res.Data.PhotosItems.Select(p => p.Large);
                res.Data.PhotosItems = [];
                return res;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task<WHRoomInfoResponse> GetRoomInfo(string hotelId, string roomcode)
        {
            try
            {
                var res = await _whClient.GetRoomInfoAsync(hotelId, roomcode) ?? throw new InvalidOperationException("Room not Found");
                res.Data!.LargePhotos = res.Data.PhotosItems.Select(p => p.Large);
                res.Data.MediumPhotos = res.Data.PhotosItems.Select(p => p.Medium);
                return res;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task CreateBooking(Reservation reservation, Repositories.ReservationsRepository repo)
        {
            try
            {
                if (reservation.Customer == null)
                {
                    throw new InvalidDataException($"Customer info is required");
                }

                if (reservation.HotelCode == null)
                {
                    throw new InvalidDataException($"Hotel code is required");
                }

                foreach (var rate in reservation.Rates)
                {
                    var parameters = new Dictionary<string, string>
                    {
                        { "checkin", reservation.CheckIn.ToString("yyyy-MM-dd")},
                        { "checkout", reservation.CheckOut.ToString("yyyy-MM-dd") },
                        { "rate", rate.RateId.Split('-')[0].ToString() },
                        { "price", (rate.NetPrice).ToString(CultureInfo.InvariantCulture) },
                        { "rooms", rate.Quantity.ToString() },
                        { "adults", rate.SearchParty?.Adults.ToString()??throw new InvalidDataException($"adults are required in party. party:{rate.SearchParty?.ToString()??"empty party"}") },
                        { "party", string.Join(",", Enumerable.Repeat(rate.SearchParty?.Party??"", rate.Quantity)).Replace("],[",",") },
                        { "firstName", reservation.Customer!.FirstName },
                        { "lastName", reservation.Customer!.LastName },
                        { "email", reservation.Customer!.Email },
                        { "remarks", reservation.Customer!.Notes },
                        { "payment_method", "CC" },
                        { "cardNumber", "5375346200033267" },
                        { "cardType", "MC" },
                        { "cardName", "Vasileios Kioroglou" },
                        { "cardMonth", "05" },
                        { "cardYear", "2026" },
                        { "cardCVV", "590" }
                    };

                    if (!await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Running, BookingStatus.Pending))
                    {
                        throw new InvalidOperationException($"Error updating reservation rate status");
                    }

                    var hotelCode = reservation.HotelCode!.Split('-')[1];
                    var res = await _whClient.CreateBookingAsync(hotelCode, parameters);
                    
                    if (res == null)
                    {
                        throw new InvalidOperationException("Invalid Booking Response");
                    }
                    
                    if (!await repo.UpdateReservationRateStatusConfirmed(rate.Id, BookingStatus.Confirmed, res.data!.res_id))
                    {
                        throw new InvalidOperationException($"Error updating reservation rate status to Confirmed. {rate.Id}");
                    }
                }

                if (reservation.Rates.All(r => r.BookingStatus == BookingStatus.Confirmed))
                {
                    if (await repo.UpdateReservationStatus(reservation.Id, BookingStatus.Confirmed, BookingStatus.Pending))
                    {
                        await SendConfirmationEmail(reservation);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                if (await CancelBooking(reservation, repo))
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message} - Error cancelling booking. ", ex);
                }
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                if (await CancelBooking(reservation, repo))
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message} - Error cancelling booking. ", ex);
                }
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        public async Task<bool> CancelBooking(Reservation reservation, Repositories.ReservationsRepository repo)
        {
            bool allOk = true;
            foreach (var rate in reservation.Rates)
            {
                if (rate.ProviderResId > 0 == true)
                {
                    var success = await _whClient.CancelBookingAsync(rate.ProviderResId);

                    if (success)
                    {
                        if (!await repo.UpdateReservationStatus(reservation.Id, BookingStatus.Cancelled))
                        {
                            throw new InvalidOperationException($"Error updating reservation status to canceled");
                        }
                    }
                    else
                    {
                        allOk = false;
                        if (!await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Running, BookingStatus.Error))
                        {
                            throw new InvalidOperationException($"Error updating reservation rate status");
                        }
                        throw new InvalidOperationException($"Error calling WebHotelier API for cancellation");
                    }
                }
            }

            return allOk;
        }

        public async Task SendConfirmationEmail(Reservation reservation)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "TravelBridge.API.Resources.BookingConfirmationEmailTemplate.html";
            string htmlContent;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName)!)
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource {resourceName} not found.");

                using StreamReader reader = new(stream);
                htmlContent = reader.ReadToEnd();
            }
            var paid = reservation.Payments.Where(p => p.PaymentStatus == PaymentStatus.Success).Sum(a => a.Amount);
            htmlContent = htmlContent
                .Replace("[Client Full Name]", $"{reservation.Customer!.LastName} {reservation.Customer.FirstName}")
                .Replace("[Hotel Name]", reservation.HotelName)
                .Replace("[ReservationCode]", string.Join(", ", reservation.Rates.Select(r => r.ProviderResId)))
                .Replace("[CheckInString]", reservation.CheckIn.ToString("dd/MM/yyyy") + $" από τις {reservation.CheckInTime} και μετά")
                .Replace("[CheckoutString]", reservation.CheckOut.ToString("dd/MM/yyyy") + $" έως τις {reservation.CheckOutTime}")
                .Replace("[NightsString]", $"{(reservation.CheckOut.DayNumber - reservation.CheckIn.DayNumber).ToString()} νύχτες")
                .Replace("[TotalPartyString]", reservation.GetFullPartyDescription())
                .Replace("[Client LastName]", reservation.Customer.LastName)
                .Replace("[Client Name]", reservation.Customer.FirstName)
                .Replace("[Client Email]", reservation.Customer.Email)
                .Replace("[Client Phone]", reservation.Customer.Tel)
                .Replace("[CustomerNotes]", reservation.Customer.Notes)
                .Replace("[TotalAmount]", reservation.TotalAmount.ToString("F2", CultureInfo.InvariantCulture) + " €")
                .Replace("[PaidAmount]", paid.ToString("F2", CultureInfo.InvariantCulture) + " €")
                .Replace("[RemainingAmount]", (reservation.TotalAmount - paid).ToString("F2", CultureInfo.InvariantCulture) + " €");

            string RoomDetails = """
                       <div>
                       <p>
                		  <span class="value">[CountAndRoomType]</span>
                        </p>
                        <p>
                          <span class="label">Διατροφή:</span>
                          <span class="value">[BoardType]</span>
                        </p>
                        <p>
                          <span class="label">Πολιτική ακύρωσης:</span>
                          <span class="value">[CancelationPolicy]</span>
                        </p>
                        <p>
                          <span class="label">Κόστος δωματίων:</span>
                          <span class="value">[RoomCost]</span>
                        </p>
                        <p>
                          <span class="label">Σύνθεση:</span>
                          <span class="value">[PartyDesc]</span>
                        </p>
                        <p></p>
                        <br/>
                      </div>
                """;

            string FinalRoomDetails = "";

            foreach (var rate in reservation.Rates)
            {
                var cancelationInfo = rate.CancelationInfo ?? "Δεν υπάρχει πολιτική ακύρωσης";
                if (reservation.RemainingAmount > 0 && reservation.PartialPayment.nextPayments.Count > 0)
                {
                    cancelationInfo +=
                        $" έως {(reservation.PartialPayment.nextPayments.OrderBy(a => a.DueDate)
                        .First().DueDate?.ToString("dd/MM/yyyy HH:mm") ?? "error")}";
                }

                FinalRoomDetails += RoomDetails
                .Replace("[CountAndRoomType]", $"{rate.Quantity} x {rate.Name}")
                .Replace("[CancelationPolicy]", cancelationInfo)
                .Replace("[BoardType]", rate.BoardInfo)
                .Replace("[PartyDesc]", rate.GetPartyInfo())
                .Replace("[RoomCost]", rate.Price.ToString("F2", CultureInfo.InvariantCulture) + " €");
            }

            htmlContent = htmlContent
               .Replace("[RoomDetails]", FinalRoomDetails);


            // Email message setup
            var mailMessage = new MailMessage
            {
                From = new MailAddress("bookings@my-diakopes.gr"),
                Subject = "Επιβεβαίωση Κράτησης",
                Body = htmlContent, // sending actual HTML content
                IsBodyHtml = true,
            };

            mailMessage.Bcc.Add("achilleaskaragiannis@outlook.com");
            mailMessage.CC.Add("bookings@my-diakopes.gr");
            mailMessage.To.Add(reservation.Customer.Email);

            await _mailSender.SendMailAsync(mailMessage);
        }
    }
}
