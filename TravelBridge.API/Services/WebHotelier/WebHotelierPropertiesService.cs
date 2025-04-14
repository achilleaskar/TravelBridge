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
                var partyList = JsonSerializer.Deserialize<List<PartyItem>>(request.Party).GroupBy(g => g)
                        .Select(g => new PartyItem
                        {
                            adults = g.Key.adults,
                            children = g.Key.children,
                            RoomsCount = g.Count()
                        }).ToList();

                Dictionary<PartyItem, Task<HttpResponseMessage>> tasks = new();
                foreach (var partyItem in partyList ?? new())
                {
                    tasks.Add(partyItem, _httpClient.GetAsync($"availability?party={Uri.EscapeDataString(JsonSerializer.Serialize(partyItem.Key))}" +
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
                        continue;
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
                            hotel.SalePrice = 0;

                        foreach (var rate in hotel.Rates)
                        {
                            rate.SearchParty = task.Key;
                        }
                    }

                    respones.Add(task.Key, res);
                }


                return MergeResponses(respones); 
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }

        private PluginSearchResponse MergeResponses(Dictionary<PartyItem, MultiAvailabilityResponse> respones)
        {
            return new PluginSearchResponse
            {
                Results = res?.Data?.Hotels?
                   .Where(h => !string.IsNullOrWhiteSpace(h.PhotoL) && h.MinPrice > 0)
                   ?? []
            };
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

        internal async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(SingleAvailabilityRequest req, DateTime checkin, List<SelectedRate>? rates = null)
        {
            try
            {
                // Build the query string from the availabilityRequest object
                var url = $"availability/{req.PropertyId}?party={Uri.EscapeDataString(req.Party)}" +
                           $"&checkin={Uri.EscapeDataString(req.CheckIn)}" +
                           $"&checkout={Uri.EscapeDataString(req.CheckOut)}";

                // Send GET request
                var response = await _httpClient.GetAsync(url);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode) // Check if response is an error
                {
                    // Deserialize the response JSON
                    var res = JsonSerializer.Deserialize<SingleAvailabilityData>(jsonString) ?? throw new InvalidOperationException("No Results");


                    if (rates != null)
                    {
                        rates.ForEach(selectedRate => selectedRate.rateId = (selectedRate.roomId != null && selectedRate.rateId == null) ? selectedRate.roomId : selectedRate.rateId);
                        res.Data.Rates = res.Data.Rates.Where(r => rates.Any(sr => sr.rateId.Equals(r.Id.ToString()) && sr.count > 0)).ToList();
                    }

                    return res?.MapToResponse(checkin) ?? new SingleAvailabilityResponse { ErrorCode = "Empty", ErrorMessage = "No records found", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
                }
                else
                {
                    var errorResponse = JsonSerializer.Deserialize<BaseWebHotelierResponse>(jsonString);

                    if (errorResponse != null && errorResponse.ErrorCode == "INVALID_PARAM")
                    {

                    }
                    return new SingleAvailabilityResponse { ErrorCode = errorResponse.ErrorCode, ErrorMessage = errorResponse.ErrorMessage, Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
                }
            }
            catch (HttpRequestException ex)
            {
                return new SingleAvailabilityResponse { ErrorCode = "Error", ErrorMessage = "Internal Error", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
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