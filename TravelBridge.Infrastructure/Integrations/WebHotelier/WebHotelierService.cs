using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TravelBridge.Core.Entities;
using TravelBridge.Core.Interfaces;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier
{
    /// <summary>
    /// Configuration options for WebHotelier API.
    /// </summary>
    public class WebHotelierOptions
    {
        public string BaseUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public GuaranteeCardOptions GuaranteeCard { get; set; } = new();
    }

    public class GuaranteeCardOptions
    {
        public string Number { get; set; } = "";
        public string Type { get; set; } = "";
        public string Name { get; set; } = "";
        public string Month { get; set; } = "";
        public string Year { get; set; } = "";
        public string CVV { get; set; } = "";
    }

    /// <summary>
    /// WebHotelier API integration service.
    /// Implements IHotelProvider for provider-agnostic hotel operations.
    /// </summary>
    public class WebHotelierService : IHotelProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly WebHotelierOptions _options;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

        public WebHotelierService(
            IHttpClientFactory httpClientFactory,
            IOptions<WebHotelierOptions> options,
            IMemoryCache cache)
        {
            _httpClient = httpClientFactory.CreateClient("WebHotelierApi");
            _options = options.Value;
            _cache = cache;
        }

        #region IHotelProvider Implementation

        public int ProviderId => (int)HotelProvider.WebHotelier;
        public string ProviderName => "WebHotelier";

        public async Task<IEnumerable<ProviderHotelSearchResult>> SearchPropertiesAsync(string propertyName, CancellationToken cancellationToken = default)
        {
            var response = await SearchPropertiesInternalAsync(propertyName);
            return response.Select(h => new ProviderHotelSearchResult
            {
                Id = $"{ProviderId}-{h.Code}",
                Code = h.Code,
                ProviderId = ProviderId,
                Name = h.Name,
                Location = h.Location?.Name,
                CountryCode = h.Location?.Country,
                PropertyType = h.Type
            });
        }

        public async Task<IReadOnlyList<ProviderHotelSearchResult>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
        {
            var response = await GetAllPropertiesInternalAsync();
            return response.Select(h => new ProviderHotelSearchResult
            {
                Id = $"{ProviderId}-{h.Code}",
                Code = h.Code,
                ProviderId = ProviderId,
                Name = h.Name,
                Location = h.Location?.Name,
                CountryCode = h.Location?.Country,
                PropertyType = h.Type
            }).ToList();
        }

        public async Task<ProviderHotelDetails> GetHotelInfoAsync(string hotelCode, CancellationToken cancellationToken = default)
        {
            var result = await GetHotelInfoInternalAsync(hotelCode);
            return new ProviderHotelDetails
            {
                Code = result.Code,
                Name = result.Name,
                Description = result.Description,
                Rating = result.Rating,
                PropertyType = result.Type,
                Location = result.Location != null ? new ProviderHotelLocation
                {
                    Latitude = (decimal)result.Location.Latitude,
                    Longitude = (decimal)result.Location.Longitude,
                    Name = result.Location.Name,
                    Address = result.Location.Address,
                    ZipCode = result.Location.Zip,
                    CountryCode = result.Location.Country
                } : null,
                Operation = result.Operation != null ? new ProviderHotelOperation
                {
                    CheckInTime = result.Operation.CheckinTime,
                    CheckOutTime = result.Operation.CheckoutTime
                } : null,
                Photos = result.LargePhotos
            };
        }

        public async Task<ProviderRoomDetails> GetRoomInfoAsync(string hotelCode, string roomCode, CancellationToken cancellationToken = default)
        {
            var result = await GetRoomInfoInternalAsync(hotelCode, roomCode);
            return new ProviderRoomDetails
            {
                Code = roomCode,
                Name = result.Name,
                Description = result.Description,
                MaxOccupancy = result.Capacity?.MaxPersons,
                Photos = result.LargePhotos,
                Amenities = result.Amenities
            };
        }

        #endregion

        #region Internal API Methods

        private async Task<IEnumerable<WHHotelBasic>> SearchPropertiesInternalAsync(string propertyName)
        {
            var response = await _httpClient.GetAsync($"property?name={Uri.EscapeDataString(propertyName)}");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WHPropertiesResponse>(jsonString);

            return result?.Data?.Hotels ?? [];
        }

        private async Task<IEnumerable<WHHotelBasic>> GetAllPropertiesInternalAsync()
        {
            var response = await _httpClient.GetAsync("property");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WHPropertiesResponse>(jsonString);

            return result?.Data?.Hotels ?? [];
        }

        private async Task<WHHotelData> GetHotelInfoInternalAsync(string hotelId)
        {
            var cacheKey = $"wh_hotel_{hotelId}";
            if (_cache.TryGetValue(cacheKey, out WHHotelData? cached) && cached != null)
                return cached;

            var response = await _httpClient.GetAsync($"/property/{hotelId}");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WHHotelInfoResponse>(jsonString)
                ?? throw new InvalidOperationException("Hotel not found");

            if (result.Data != null)
            {
                result.Data.LargePhotos = result.Data.Photos?.Select(p => p.Large ?? "") ?? [];
                _cache.Set(cacheKey, result.Data, CacheDuration);
            }

            return result.Data ?? throw new InvalidOperationException("Hotel not found");
        }

        private async Task<WHRoomData> GetRoomInfoInternalAsync(string hotelId, string roomCode)
        {
            var cacheKey = $"wh_room_{hotelId}_{roomCode}";
            if (_cache.TryGetValue(cacheKey, out WHRoomData? cached) && cached != null)
                return cached;

            var response = await _httpClient.GetAsync($"/room/{hotelId}/{roomCode}");
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WHRoomInfoResponse>(jsonString)
                ?? throw new InvalidOperationException("Room not found");

            if (result.Data != null)
            {
                result.Data.LargePhotos = result.Data.Photos?.Select(p => p.Large ?? "") ?? [];
                result.Data.MediumPhotos = result.Data.Photos?.Select(p => p.Medium ?? "") ?? [];
                _cache.Set(cacheKey, result.Data, CacheDuration);
            }

            return result.Data ?? throw new InvalidOperationException("Room not found");
        }

        #endregion
    }
}
