using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TravelBridge.Infrastructure.Integrations.ExternalServices
{
    /// <summary>
    /// Configuration options for HERE Maps API.
    /// </summary>
    public class HereMapsOptions
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }

    /// <summary>
    /// HERE Maps autocomplete response models.
    /// </summary>
    public class HereMapsAutoCompleteResponse
    {
        public List<HereMapsItem> Items { get; set; } = [];
    }

    public class HereMapsItem
    {
        public string? Title { get; set; }
        public string? ResultType { get; set; }
        public string? LocalityType { get; set; }
        public HereMapsAddress Address { get; set; } = new();
    }

    public class HereMapsAddress
    {
        public string Label { get; set; } = "";
        public string? CountryName { get; set; }
        public string? State { get; set; }
        public string? County { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
    }

    /// <summary>
    /// HERE Maps geocoding service for location autocomplete.
    /// </summary>
    public class HereMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string CountryCodes = "CYP,GRC";
        private const int Limit = 20;

        public HereMapsService(IHttpClientFactory httpClientFactory, IOptions<HereMapsOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("HereMapsApi");
            _apiKey = options.Value.ApiKey;
        }

        /// <summary>
        /// Gets location autocomplete results from HERE Maps.
        /// </summary>
        public async Task<IEnumerable<string>> GetLocations(string? searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            try
            {
                var response = await _httpClient.GetAsync(
                    $"autocomplete" +
                    $"?q={Uri.EscapeDataString(searchTerm)}" +
                    $"&in=countryCode:{CountryCodes}" +
                    $"&limit={Limit}" +
                    $"&apiKey={_apiKey}"
                );

                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<HereMapsAutoCompleteResponse>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result?.Items.Count > 0)
                    return result.Items.Select(r => r.Address.Label);
            }
            catch (HttpRequestException)
            {
                // Log and return empty - don't fail the entire request
            }

            return [];
        }
    }
}
