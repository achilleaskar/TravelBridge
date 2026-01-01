using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Models;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Models.DB;
using TravelBridge.API.Models.Plugin.AutoComplete;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.API.Repositories;
using TravelBridge.Core.Interfaces;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Services.WebHotelier
{
    public class WebHotelierPropertiesService : IHotelProvider
    {
        private readonly HttpClient _httpClient;
        private readonly SmtpEmailSender _mailSender;
        private readonly WebHotelierApiOptions _options;
        private readonly IMemoryCache _cache;

        // Cache duration for hotel and room info (6 hours)
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        #region IHotelProvider Implementation

        public int ProviderId => (int)Provider.WebHotelier;
        public string ProviderName => "WebHotelier";

        public async Task<IEnumerable<ProviderHotelSearchResult>> SearchPropertiesAsync(string propertyName, CancellationToken cancellationToken = default)
        {
            var results = await SearchPropertyAsync(propertyName);
            return results.Select(h => new ProviderHotelSearchResult
            {
                Id = h.Id,
                Code = h.OrId,
                ProviderId = ProviderId,
                Name = h.Name,
                Location = h.Location,
                CountryCode = h.CountryCode,
                PropertyType = h.OriginalType
            });
        }

        public async Task<IReadOnlyList<ProviderHotelSearchResult>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
        {
            var results = await GetAllPropertiesAsync();
            return results.Select(h => new ProviderHotelSearchResult
            {
                Id = h.Id,
                Code = h.OrId,
                ProviderId = ProviderId,
                Name = h.Name,
                Location = h.Location,
                CountryCode = h.CountryCode,
                PropertyType = h.OriginalType
            }).ToList();
        }

        public async Task<ProviderHotelDetails> GetHotelInfoAsync(string hotelCode, CancellationToken cancellationToken = default)
        {
            var result = await GetHotelInfo(hotelCode);
            return new ProviderHotelDetails
            {
                Code = result.Data.Code,
                Name = result.Data.Name,
                Description = result.Data.Description,
                Rating = result.Data.Rating,
                PropertyType = result.Data.Type,
                Location = result.Data.Location != null ? new ProviderHotelLocation
                {
                    Latitude = (decimal)result.Data.Location.Latitude,
                    Longitude = (decimal)result.Data.Location.Longitude,
                    Name = result.Data.Location.Name,
                    Address = result.Data.Location.Address,
                    ZipCode = result.Data.Location.Zip,
                    CountryCode = result.Data.Location.Country
                } : null,
                Operation = result.Data.Operation != null ? new ProviderHotelOperation
                {
                    CheckInTime = result.Data.Operation.CheckinTime,
                    CheckOutTime = result.Data.Operation.CheckoutTime
                } : null,
                Photos = result.Data.LargePhotos
            };
        }

        public async Task<ProviderRoomDetails> GetRoomInfoAsync(string hotelCode, string roomCode, CancellationToken cancellationToken = default)
        {
            var result = await GetRoomInfo(hotelCode, roomCode);
            return new ProviderRoomDetails
            {
                Code = roomCode,
                Name = result.Data.Name,
                Description = result.Data.Description,
                MaxOccupancy = result.Data.Capacity?.MaxPersons,
                Photos = result.Data.LargePhotos,
                Amenities = result.Data.Amenities
            };
        }

        #endregion IHotelProvider Implementation

        public WebHotelierPropertiesService(
            IHttpClientFactory httpClientFactory, 
            SmtpEmailSender mailSender, 
            IOptions<WebHotelierApiOptions> options,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("WebHotelierApi");
            _mailSender = mailSender;
            _options = options.Value;
            _cache = cache;
        }

        public async Task<IEnumerable<AutoCompleteHotel>> SearchPropertyAsync(string propertyName)
        {
            try
            {
                var response = await _httpClient.GetAsync($"property?name={Uri.EscapeDataString(propertyName)}");

                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<PropertiesResponse>(jsonString);

                if (result?.data?.hotels.Length > 0)
                    return MapResultsToHotels(result.data.hotels);
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(ex.ToString());
            }
            return [];
        }

        public async Task<List<AutoCompleteHotel>> GetAllPropertiesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"property");

                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<PropertiesResponse>(jsonString);

                if (result?.data?.hotels.Length > 0)
                    return MapResultsToHotels(result.data.hotels).ToList();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException(ex.ToString());
            }
            return [];
        }

        public async Task<PluginSearchResponse> GetAvailabilityAsync(MultiAvailabilityRequest request)
        {
            try
            {
                List<PartyItem> partyList = GetPartyList(request);

                if (partyList.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API. Invalid parties");
                }

                Dictionary<PartyItem, Task<HttpResponseMessage>> tasks = new();
                foreach (var partyItem in partyList ?? new())
                {
                    tasks.Add(partyItem, _httpClient.GetAsync($"availability?party={partyItem.party}" +
                           $"&checkin={Uri.EscapeDataString(request.CheckIn)}" +
                           $"&checkout={Uri.EscapeDataString(request.CheckOut)}" +
                           $"&lat={Uri.EscapeDataString(request.Lat)}" +
                           $"&lon={Uri.EscapeDataString(request.Lon)}" +
                           $"&lat1={Uri.EscapeDataString(request.BottomLeftLatitude)}" +
                           $"&lat2={Uri.EscapeDataString(request.TopRightLatitude)}" +
                           $"&lon1={Uri.EscapeDataString(request.BottomLeftLongitude)}" +
                           $"&lon2={Uri.EscapeDataString(request.TopRightLongitude)}" +
                           $"&sort_by={Uri.EscapeDataString(request.SortBy)}" +
                           $"&sort_order={Uri.EscapeDataString(request.SortOrder)}&&payments=1"));
                }

                // Send GET request
                await Task.WhenAll(tasks.Values);

                Dictionary<PartyItem, MultiAvailabilityResponse> respones = new();

                foreach (var task in tasks)
                {
                    var result = task.Value.Result;
                    result.EnsureSuccessStatusCode();
                    var jsonString = await result.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<MultiAvailabilityResponse>(jsonString);
                    if (res == null)
                    {
                        return new PluginSearchResponse();
                    }
                    Provider Provider = Provider.WebHotelier;
                    // Deserialize the response JSON
                    foreach (WebHotel hotel in res.Data?.Hotels ?? [])
                    {
                        hotel.Id = $"{(int)Provider}-{hotel.Code}";
                        hotel.SearchParty = task.Key;
                        hotel.MinPrice = Math.Floor(hotel.GetMinPrice(out decimal salePrice));
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
                MergedRooms.Results = MergedRooms.CoverRequest(partyList!);
                return MergedRooms;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(SingleAvailabilityRequest req, DateTime checkin, ReservationsRepository? reservationsRepository, List<SelectedRate>? rates = null, string? couponCode = null)
        {
            try
            {
                rates?.ForEach(r => r.FillPartyFromId());

                List<PartyItem> partyList = rates == null ? GetPartyList(req) : GetPartyList(rates);

                if (partyList.IsNullOrEmpty())
                {
                    throw new InvalidOperationException($"Error calling WebHotelier API. Invalid parties");
                }

                Dictionary<PartyItem, Task<HttpResponseMessage>> tasks = new();
                foreach (var partyItem in partyList ?? new())
                {
                    // Build the query string from the availabilityRequest object
                    tasks.Add(partyItem, _httpClient.GetAsync($"availability/{req.PropertyId}?party={partyItem.party}" +
                           $"&checkin={Uri.EscapeDataString(req.CheckIn)}" +
                           $"&checkout={Uri.EscapeDataString(req.CheckOut)}"));
                }

                // Send GET request
                await Task.WhenAll(tasks.Values);

                SingleAvailabilityData? finalRes = null;
                foreach (var task in tasks.OrderBy(p => p.Key.adults))
                {
                    var result = task.Value.Result;
                    result.EnsureSuccessStatusCode();
                    var jsonString = await result.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<SingleAvailabilityData>(jsonString) ?? throw new InvalidOperationException("No Results");
                    if (res == null)
                    {
                        return new SingleAvailabilityResponse { ErrorMessage = "No response" };
                    }
                    finalRes?.Data?.Rates.AddRange(res.Data?.Rates ?? new List<HotelRate>());
                    finalRes ??= res;

                    Provider Provider = Provider.WebHotelier;
                    // Deserialize the response JSON
                    foreach (var rate in res.Data?.Rates ?? [])
                    {
                        rate.SearchParty = task.Key;
                        rate.Id += $"-{task.Key.adults}{(task.Key.children?.Length > 0 ? ("_" + string.Join("_", task.Key.children)) : "")}";
                    }
                }

                if (rates != null)
                {
                    //make sure i return proper rates and counts. test scenario with the same rate two times

                    rates.ForEach(selectedRate => selectedRate.rateId = (selectedRate.roomId != null && selectedRate.rateId == null) ? selectedRate.roomId : selectedRate.rateId);
                    finalRes.Data.Rates = finalRes.Data.Rates
                        .Where(r => rates.Any(sr => sr.rateId.Equals(r.Id.ToString()) && sr.count > 0)).ToList();

                    if (!finalRes.CoversRequest(partyList))
                    {
                        return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Not enough rooms", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
                    }
                }
                else if (finalRes?.Data?.Rates.IsNullOrEmpty() != false)
                {
                    finalRes.Alternatives = await GetAlternatives(partyList, req);
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
                    finalRes.Data = new HotelInfo
                    {
                        Code = req.PropertyId,
                        Provider = Provider.WebHotelier,
                        Rates = []
                    };
                }


                return finalRes?.MapToResponse(checkin, disc, couponType)
                    ?? new SingleAvailabilityResponse { ErrorCode = "Empty", ErrorMessage = "No records found", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
            catch (HttpRequestException ex)
            {
                return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Internal Error", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
        }

        private async Task<List<Alternative>> GetAlternatives(List<PartyItem>? partyList, SingleAvailabilityRequest req)
        {
            var from = DateTime.Parse(req.CheckIn).AddDays(-14);
            var to = DateTime.Parse(req.CheckOut).AddDays(14);

            if (from < DateTime.Today.AddDays(1))
            {
                from = DateTime.Today.AddDays(1);
            }

            Dictionary<PartyItem, Task<HttpResponseMessage>> tasks = new();
            foreach (var partyItem in partyList ?? new())
            {
                // Build the query string from the availabilityRequest object
                tasks.Add(partyItem, _httpClient.GetAsync($"availability/{req.PropertyId}/flexible-calendar?party={partyItem.party}" +
                       $"&startDate={from:yyyy-MM-dd}" +
                       $"&endDate={to:yyyy-MM-dd}"));
            }

            // Send GET request
            await Task.WhenAll(tasks.Values);
            Dictionary<PartyItem, List<Alternative>> alterDatesDict = new();
            SingleAvailabilityData? finalRes = null;
            foreach (var task in tasks.OrderBy(p => p.Key.adults))
            {
                var result = task.Value.Result;
                result.EnsureSuccessStatusCode();
                var jsonString = await result.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<AlternativeDaysData>(jsonString) ?? throw new InvalidOperationException("No Results");
                if (res == null)
                {
                    return [];
                }
                var alterDates = res.Data?.days?.Where(d => d.status == "AVL" || d.status == "MIN") ?? [];
                alterDatesDict.Add(task.Key, GetAlterDates(alterDates, req.CheckIn, req.CheckOut));
            }

            return KeepCommon(alterDatesDict);
        }

        private static List<Alternative> KeepCommon(Dictionary<PartyItem, List<Alternative>> alterDatesDict)
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
                .Select(g => new Alternative
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

        private static List<Alternative> GetAlterDates(IEnumerable<AlternativeDayInfo> alterDates, string checkIn, string checkOut)
        {
            try
            {
                List<Alternative> results = new List<Alternative>();
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
                    Alternative alt = new Alternative()
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

        private static List<PartyItem> GetPartyList(List<SelectedRate> rates)
        {
            var partyList = new List<PartyItem>();

            foreach (var selectedRate in rates.GroupBy(g => g.searchParty))
            {
                var item = JsonSerializer.Deserialize<PartyItem>(selectedRate.Key.TrimStart('[').TrimEnd(']')) ?? throw new InvalidDataException("Invalid Party");
                item.RoomsCount = selectedRate.Count();
                item.party = selectedRate.Key;
                partyList.Add(item);
            }
            return partyList;
        }

        private static PluginSearchResponse MergeResponses(Dictionary<PartyItem, MultiAvailabilityResponse> respones)
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
                        Results = respones.First().Value?.Data?.Hotels?
                        .Where(h => !string.IsNullOrWhiteSpace(h.PhotoL) && h.MinPrice > 0)
                        ?? []
                    };
                }

                if (respones.Any(r => r.Value?.Data?.Hotels?.IsNullOrEmpty() != false))
                {
                    throw new InvalidOperationException($"No results");
                }

                //keep only hotels that exist in all results
                var GroupedHotels = respones.SelectMany(h => h.Value.Data.Hotels)
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
                    temp.Rates = hotels.SelectMany(h => h.Rates).ToList();
                    temp.MinPrice = hotels.Sum(h => h.MinPrice * h.SearchParty.RoomsCount);
                    temp.MinPricePerDay = hotels.Sum(h => h.MinPricePerDay * h.SearchParty.RoomsCount);
                    temp.SalePrice = hotels.Sum(h => h.SalePrice * h.SearchParty.RoomsCount);

                    FinalObjects.Add(temp);
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

        private static IEnumerable<AutoCompleteHotel> MapResultsToHotels(Hotel[] hotels)
        {
            //TODO:maybe use custom hotel codes stored in our DB for security and privacy
            //TODO: handle error
            return hotels.Select(f =>
            new AutoCompleteHotel(f.code,
            Provider.WebHotelier,
            f.name,
            f.location.name,
            f.location.country,
            f.type));
        }

        internal async Task<HotelInfoResponse> GetHotelInfo(string hotelId)
        {
            // Try to get from cache first
            var cacheKey = $"hotel_info_{hotelId}";
            if (_cache.TryGetValue(cacheKey, out HotelInfoResponse? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                // Build the query string from the availabilityRequest object
                var url = $"/property/{hotelId}";

                // Send GET request
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON
                var jsonString = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<HotelInfoResponse>(jsonString) ?? throw new InvalidOperationException("Hotel not Found");
                res.Data.LargePhotos = res.Data.PhotosItems.Select(p => p.Large);
                res.Data.PhotosItems = [];

                // Cache the result for 6 hours
                _cache.Set(cacheKey, res, CacheDuration);

                return res;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task<RoomInfoRespone> GetRoomInfo(string hotelId, string roomcode)
        {
            // Try to get from cache first
            var cacheKey = $"room_info_{hotelId}_{roomcode}";
            if (_cache.TryGetValue(cacheKey, out RoomInfoRespone? cachedResult) && cachedResult != null)
            {
                return cachedResult;
            }

            try
            {
                // Build the query string from the availabilityRequest object
                var url = $"/room/{hotelId}/{roomcode}";

                // Send GET request
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON
                var jsonString = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<RoomInfoRespone>(jsonString) ?? throw new InvalidOperationException("Hotel not Found");
                res.Data.LargePhotos = res.Data.PhotosItems.Select(p => p.Large);
                res.Data.MediumPhotos = res.Data.PhotosItems.Select(p => p.Medium);
                //res.Data.PhotosItems = res.Data.PhotosItems.Select(a => new PhotoInfo { Medium = a.Medium });


                // Cache the result for 6 hours
                _cache.Set(cacheKey, res, CacheDuration);

                return res;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        private static List<PartyItem> GetPartyList(IParty request)
        {
            return JsonSerializer.Deserialize<List<PartyItem>>(request.Party).GroupBy(g => g)
                    .Select(g => new PartyItem
                    {
                        adults = g.Key.adults,
                        children = g.Key.children,
                        RoomsCount = g.Count(),
                        party = JsonSerializer.Serialize(new List<PartyItem> { g.Key })
                    }).ToList();
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
                        { "cardNumber", _options.GuaranteeCard.Number },
                        { "cardType", _options.GuaranteeCard.Type },
                        { "cardName", _options.GuaranteeCard.Name },
                        { "cardMonth", _options.GuaranteeCard.Month },
                        { "cardYear", _options.GuaranteeCard.Year },
                        { "cardCVV", _options.GuaranteeCard.CVV }
                    };

                    if (!await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Running, BookingStatus.Pending))
                    {
                        throw new InvalidOperationException($"Error updating reservation rate status");
                    }

                    HttpResponseMessage response = await CreateBooking(reservation, parameters);
                    var jsonString = await response.Content.ReadAsStringAsync();

                    // Read response as JSON string
                    if (response.IsSuccessStatusCode)
                    {
                        var res = JsonSerializer.Deserialize<BookingResponse>(jsonString) ?? throw new InvalidOperationException("Invalid Booking Response : " + jsonString);
                        if (!await repo.UpdateReservationRateStatusConfirmed(rate.Id, BookingStatus.Confirmed, res.data.res_id))
                        {
                            throw new InvalidOperationException($"Error updating reservation rate status to Confirmed. {rate.Id}");
                        }
                    }
                    else
                    {
                        if (!await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Error, BookingStatus.Running))
                        {
                            throw new InvalidOperationException($"Error updating reservation rate status from running to error. {rate.Id}");
                        }

                        throw new InvalidOperationException($"Error calling WebHotelier API: {response.StatusCode} - {jsonString}");
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

        private async Task<HttpResponseMessage> CreateBooking(Reservation reservation, Dictionary<string, string> parameters)
        {
            // Build the query string from the availabilityRequest object
            var url = $"/book/{reservation.HotelCode!.Split('-')[1]}";

            // Serialize parameters to URL-encoded form data
            var content = new FormUrlEncodedContent(parameters);

            // Set the Accept header to receive JSON response
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Send POST request with parameters in the body
            var response = await _httpClient.PostAsync(url, content);
            return response;
        }

        public async Task<bool> CancelBooking(Reservation reservation, Repositories.ReservationsRepository repo)
        {
            bool allOk = true;
            foreach (var rate in reservation.Rates)
            {
                if (rate.ProviderResId > 0 == true)
                {
                    // Build the query string from the availabilityRequest object
                    var url = $"/purge/{rate.ProviderResId}";

                    // Send GET request
                    var response = await _httpClient.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        if (!await repo.UpdateReservationStatus(reservation.Id, BookingStatus.Cancelled))
                        {
                            throw new InvalidOperationException($"Error updating reservation status to canceled");
                            //await SendConfirmationEmail(reservation);
                        }
                    }
                    else
                    {
                        allOk = false;
                        if (!await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Running, BookingStatus.Error))
                        {
                            throw new InvalidOperationException($"Error updating reservation rate status");
                        }
                        throw new InvalidOperationException($"Error calling WebHotelier API: {response.StatusCode} - {response.ReasonPhrase} - {await response.Content.ReadAsStringAsync()}");
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

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException($"Resource {resourceName} not found.");

                using StreamReader reader = new(stream);
                htmlContent = reader.ReadToEnd();
            }
            var paid = reservation.Payments.Where(p => p.PaymentStatus == PaymentStatus.Success).Sum(a => a.Amount);
            htmlContent = htmlContent
                .Replace("[Client Full Name]", $"{reservation.Customer.LastName} {reservation.Customer.FirstName}")
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

        /// <summary>
        /// Sends an urgent notification email to admins when payment succeeded but booking failed.
        /// This is a critical scenario where the customer paid but has no booking.
        /// </summary>
        public async Task SendBookingErrorNotificationAsync(Reservation reservation, decimal paidAmount, string errorMessage)
        {
            var customerNotes = reservation.Customer?.Notes ?? "-";
            var htmlContent = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <style>
                                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                .alert {{ background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; padding: 15px; border-radius: 5px; margin-bottom: 20px; }}
                                .info {{ background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 15px; }}
                                .label {{ font-weight: bold; color: #495057; }}
                                h1 {{ color: #dc3545; }}
                                h2 {{ color: #495057; border-bottom: 1px solid #dee2e6; padding-bottom: 10px; }}
                            </style>
                        </head>
                        <body>
                            <div class=""container"">
                                <div class=""alert"">
                                    <h1>⚠️ ΠΡΟΣΟΧΗ: Αποτυχία Κράτησης μετά από Επιτυχή Πληρωμή</h1>
                                    <p>Ο πελάτης πλήρωσε αλλά η κράτηση στο WebHotelier απέτυχε. Απαιτείται άμεση ενέργεια!</p>
                                </div>

                                <h2>Στοιχεία Κράτησης</h2>
                                <div class=""info"">
                                    <p><span class=""label"">Αριθμός Κράτησης:</span> {reservation.Id}</p>
                                    <p><span class=""label"">Ξενοδοχείο:</span> {reservation.HotelName} ({reservation.HotelCode})</p>
                                    <p><span class=""label"">Check-in:</span> {reservation.CheckIn:dd/MM/yyyy}</p>
                                    <p><span class=""label"">Check-out:</span> {reservation.CheckOut:dd/MM/yyyy}</p>
                                    <p><span class=""label"">Συνολικό Ποσό:</span> {reservation.TotalAmount:F2} €</p>
                                    <p><span class=""label"">Ποσό που Πληρώθηκε:</span> {paidAmount:F2} €</p>
                                </div>

                                <h2>Στοιχεία Πελάτη</h2>
                                <div class=""info"">
                                    <p><span class=""label"">Όνομα:</span> {reservation.Customer?.FirstName} {reservation.Customer?.LastName}</p>
                                    <p><span class=""label"">Email:</span> {reservation.Customer?.Email}</p>
                                    <p><span class=""label"">Τηλέφωνο:</span> {reservation.Customer?.Tel}</p>
                                    <p><span class=""label"">Σημειώσεις:</span> {customerNotes}</p>
                                </div>

                                <h2>Σφάλμα</h2>
                                <div class=""info"" style=""background-color: #fff3cd;"">
                                    <p><span class=""label"">Μήνυμα Σφάλματος:</span></p>
                                    <pre style=""white-space: pre-wrap; word-wrap: break-word;"">{errorMessage}</pre>
                                </div>

                                <h2>Απαιτούμενες Ενέργειες</h2>
                                <ol>
                                    <li>Επικοινωνήστε με τον πελάτη για να τον ενημερώσετε</li>
                                    <li>Δημιουργήστε την κράτηση χειροκίνητα στο WebHotelier</li>
                                    <li>Ή προχωρήστε σε επιστροφή χρημάτων μέσω Viva Wallet</li>
                                </ol>

                                <p style=""color: #6c757d; font-size: 12px; margin-top: 30px;"">
                                    Αυτό το email στάλθηκε αυτόματα από το σύστημα TravelBridge στις {DateTime.Now:dd/MM/yyyy HH:mm:ss}
                                </p>
                            </div>
                        </body>
                        </html>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress("bookings@my-diakopes.gr"),
                Subject = $"🚨 ΕΠΕΙΓΟΝ: Αποτυχία Κράτησης #{reservation.Id} - Πληρωμή Ολοκληρώθηκε",
                Body = htmlContent,
                IsBodyHtml = true,
            };

            // Send to all admin addresses
            mailMessage.To.Add("bookings@my-diakopes.gr");
            mailMessage.To.Add("achilleaskaragiannis@outlook.com");

            try
            {
                await _mailSender.SendMailAsync(mailMessage);
            }
            catch (Exception)
            {
                // Log but don't throw - we don't want email failure to mask the original error
                // The booking error is already being returned to the user
            }
        }
    }
}