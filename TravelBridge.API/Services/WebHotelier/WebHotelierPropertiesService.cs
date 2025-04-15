using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Models;
using TravelBridge.API.Models.DB;
using TravelBridge.API.Models.Plugin.AutoComplete;
using TravelBridge.API.Models.WebHotelier;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Services.WebHotelier
{
    public class WebHotelierPropertiesService
    {
        private readonly HttpClient _httpClient;

        public WebHotelierPropertiesService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("WebHotelierApi");
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

        internal async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(SingleAvailabilityRequest req, DateTime checkin, List<SelectedRate>? rates = null)
        {
            try
            {
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
                    }
                }

                if (rates != null)
                {
                    //make sure i return proper rates and counts. test scenario with the same rate two times

                    rates.ForEach(selectedRate => selectedRate.rateId = (selectedRate.roomId != null && selectedRate.rateId == null) ? selectedRate.roomId : selectedRate.rateId);
                    finalRes.Data.Rates = finalRes.Data.Rates
                        .Where(r => rates.Any(sr => sr.rateId.Equals(r.Id.ToString()) && r.SearchParty.party.Equals(sr.searchParty) && sr.count > 0)).ToList();
                }

                if (!finalRes.CoversRequest(partyList))
                {
                    return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Not enough rooms", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
                }

                return finalRes?.MapToResponse(checkin)
                    ?? new SingleAvailabilityResponse { ErrorCode = "Empty", ErrorMessage = "No records found", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
            catch (HttpRequestException ex)
            {
                return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Internal Error", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
        }

        private List<PartyItem> GetPartyList(List<SelectedRate> rates)
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

        private PluginSearchResponse MergeResponses(Dictionary<PartyItem, MultiAvailabilityResponse> respones)
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

                //step 1: keep only same hotels

                ////get 1st request ids
                //var commonHotelIds = new HashSet<string>(respones.First().Value.Data.Hotels.Select(h => h.Id));
                //// Intersect with the rest
                //foreach (var hotelList in respones.Skip(1).Select(a => a.Value.Data.Hotels))
                //{
                //    var currentIds = new HashSet<string>(hotelList.Select(h => h.Id));
                //    commonHotelIds.IntersectWith(currentIds);
                //}

                //step 2: filter hotels
                var GroupedHotels = respones.SelectMany(h => h.Value.Data.Hotels)
                     .GroupBy(h => h.Id).Where(h => h.Count() == respones.Count()).ToList()
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
                return res;
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        internal async Task<RoomInfoRespone> GetRoomInfo(string hotelId, string roomcode)
        {
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
                        party = JsonSerializer.Serialize(g)
                    }).ToList();
        }

        internal async Task CreateBooking(Reservation reservation)
        {
            var parameters = new Dictionary<string, string>
            {
                { "checkin", reservation.CheckIn.ToString("yyyy-MM-dd") },
                { "checkout", reservation.CheckOut.ToString("yyyy-MM-dd") },
                { "rate", "DBL" },
                { "rate_plan_code", "STANDARD" },
                { "arrival", "2025-06-12" },
                { "departure", "2025-06-15" },
                { "adults", "2" },
                { "children", "0" },
                { "client[first_name]", "John" },
                { "client[last_name]", "Doe" },
                { "client[email]", "john.doe@example.com" },
                { "client[phone]", "+123456789" },
                { "payments[method]", "cash" }
            };
        }
    }
}