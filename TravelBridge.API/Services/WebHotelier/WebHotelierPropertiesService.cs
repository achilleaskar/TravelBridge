using System.Text.Json;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Models;
using TravelBridge.API.Models.Plugin.AutoComplete;
using TravelBridge.API.Models.WebHotelier;

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
                // Build the query string from the availabilityRequest object
                var url = $"availability?party={Uri.EscapeDataString(request.Party)}" +
                           $"&checkin={Uri.EscapeDataString(request.CheckIn)}" +
                           $"&checkout={Uri.EscapeDataString(request.CheckOut)}" +
                           $"&lat={Uri.EscapeDataString(request.Lat)}" +
                           $"&lon={Uri.EscapeDataString(request.Lon)}" +
                           $"&lat1={Uri.EscapeDataString(request.BottomLeftLatitude)}" +
                           $"&lat2={Uri.EscapeDataString(request.TopRightLatitude)}" +
                           $"&lon1={Uri.EscapeDataString(request.BottomLeftLongitude)}" +
                           $"&lon2={Uri.EscapeDataString(request.TopRightLongitude)}" +
                           $"&sort_by={Uri.EscapeDataString(request.SortBy)}" +
                           $"&sort_order={Uri.EscapeDataString(request.SortOrder)}";

                // Send GET request
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON
                var jsonString = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<MultiAvailabilityResponse>(jsonString);

                Provider Provider = Provider.WebHotelier;

                foreach (WebHotel hotel in res?.Data.Hotels ?? [])
                {
                    hotel.Id = $"{(int)Provider}-{hotel.Code}";
                    hotel.MinPrice = Math.Ceiling(hotel.GetMinPrice(out decimal salePrice));
                    hotel.SalePrice = salePrice;
                }

                return new PluginSearchResponse
                {
                    Results = res?.Data?.Hotels?
                    .Where(h => !string.IsNullOrWhiteSpace(h.PhotoL) && h.MinPrice > 0)
                    ?? []
                };
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
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


        internal async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(SingleAvailabilityRequest req)
        {
            try
            {
                // Build the query string from the availabilityRequest object
                var url = $"availability/{req.PropertyId}?party={Uri.EscapeDataString(req.Party)}" +
                           $"&checkin={Uri.EscapeDataString(req.CheckIn)}" +
                           $"&checkout={Uri.EscapeDataString(req.CheckOut)}";

                // Send GET request
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                // Deserialize the response JSON
                var jsonString = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<SingleAvailabilityData>(jsonString) ?? throw new InvalidOperationException("No Results");

                return res?.MapToResponse() ?? new SingleAvailabilityResponse { ErrorCode = "Empty", ErrorMessage = "No records found", Data = new SingleHotelAvailabilityInfo { Rooms = [] } };
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
            }
        }
    }
}