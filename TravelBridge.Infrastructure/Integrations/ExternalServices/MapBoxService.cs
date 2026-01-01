using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace TravelBridge.Infrastructure.Integrations.ExternalServices
{
    /// <summary>
    /// Configuration options for MapBox API.
    /// </summary>
    public class MapBoxOptions
    {
        public string BaseUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }

    /// <summary>
    /// MapBox autocomplete response models.
    /// </summary>
    public class MapBoxAutoCompleteResponse
    {
        [JsonPropertyName("features")]
        public List<MapBoxFeature>? Features { get; set; }
    }

    public class MapBoxFeature
    {
        [JsonPropertyName("properties")]
        public MapBoxProperties? Properties { get; set; }
    }

    public class MapBoxProperties
    {
        [JsonPropertyName("name_preferred")]
        public string? NamePreferred { get; set; }

        [JsonPropertyName("feature_type")]
        public string? FeatureType { get; set; }

        [JsonPropertyName("coordinates")]
        public MapBoxCoordinates? Coordinates { get; set; }

        [JsonPropertyName("bbox")]
        public List<double>? Bbox { get; set; }

        [JsonPropertyName("context")]
        public MapBoxContext? Context { get; set; }
    }

    public class MapBoxCoordinates
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public class MapBoxContext
    {
        [JsonPropertyName("region")]
        public MapBoxRegion? Region { get; set; }

        [JsonPropertyName("country")]
        public MapBoxCountry? Country { get; set; }
    }

    public class MapBoxRegion
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    public class MapBoxCountry
    {
        [JsonPropertyName("country_code")]
        public string? CountryCode { get; set; }
    }

    /// <summary>
    /// Location autocomplete result from MapBox.
    /// </summary>
    public class LocationAutoCompleteResult
    {
        public string Name { get; set; } = "";
        public string Region { get; set; } = "";
        public string Id { get; set; } = "";
        public string CountryCode { get; set; } = "";
    }

    /// <summary>
    /// MapBox geocoding service for location autocomplete.
    /// </summary>
    public class MapBoxService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const int Limit = 10;

        public MapBoxService(IHttpClientFactory httpClientFactory, IOptions<MapBoxOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("MapBoxApi");
            _apiKey = options.Value.ApiKey;
        }

        /// <summary>
        /// Gets location autocomplete results from MapBox.
        /// </summary>
        public async Task<IEnumerable<LocationAutoCompleteResult>> GetLocations(string? searchTerm, string? language = "el")
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return [];

            try
            {
                var response = await _httpClient.GetAsync(
                    $"search/geocode/v6/forward" +
                    $"?q={Uri.EscapeDataString(searchTerm)}" +
                    $"&country=gr,cy" +
                    $"&limit={Limit}" +
                    $"&types=neighborhood,region,country,place,district,locality" +
                    $"&language={language ?? "el"}" +
                    $"&autocomplete=true" +
                    $"&access_token={_apiKey}" +
                    $"&permanent=false"
                );

                response.EnsureSuccessStatusCode();
                var jsonString = await response.Content.ReadAsStringAsync();

                var result = JsonSerializer.Deserialize<MapBoxAutoCompleteResponse>(jsonString);

                if (result?.Features?.Count > 0)
                    return MapResultsToLocations(result.Features);
            }
            catch (HttpRequestException)
            {
                // Log and return empty - don't fail the entire request
            }
            catch (Exception)
            {
                // Swallow other exceptions to match API behavior
            }

            return [];
        }

        private static IEnumerable<LocationAutoCompleteResult> MapResultsToLocations(List<MapBoxFeature> features)
        {
            return features
                .Where(f => f.Properties != null &&
                           (f.Properties.FeatureType == null || !f.Properties.FeatureType.Equals("country", StringComparison.OrdinalIgnoreCase)))
                .Select(f => new LocationAutoCompleteResult
                {
                    Name = f.Properties?.NamePreferred ?? "",
                    Region = f.Properties?.Context?.Region?.Name ?? "",
                    Id = f.Properties?.Bbox != null && f.Properties.Coordinates != null
                        ? $"[{string.Join(",", f.Properties.Bbox)}]-{f.Properties.Coordinates.Latitude}-{f.Properties.Coordinates.Longitude}"
                        : "",
                    CountryCode = f.Properties?.Context?.Country?.CountryCode ?? ""
                });
        }
    }
}
