using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Plugin.AutoComplete;

namespace TravelBridge.Geo.Mapbox;

public class MapBoxService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const int Limit = 10;

    public MapBoxService(IHttpClientFactory httpClientFactory, IOptions<MapBoxApiOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("MapBoxApi");
        _apiKey = options.Value.ApiKey;
    }

    /// <summary>
    /// Get location suggestions from MapBox API
    /// </summary>
    /// <returns>List of AutoCompleteLocation objects</returns>
    public async Task<IEnumerable<AutoCompleteLocation>> GetLocationsAsync(string? param, string? lang)
    {
        if (param is null)
        {
            return [];
        }

        var language = string.IsNullOrWhiteSpace(lang) ? "el" : lang;

        try
        {
            var response = await _httpClient.GetAsync(
                $"search/geocode/v6/forward" +
                $"?q={Uri.EscapeDataString(param)}" +
                $"&country=gr,cy" +
                $"&limit={Limit}" +
                $"&types=neighborhood,region,country,place,district,locality" +
                $"&language={language}" +
                $"&autocomplete=true" +
                $"&access_token={_apiKey}" +
                $"&permanent=false");

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MapBoxAutoCompleteResponse>(jsonString);

            if (result?.Features?.Count > 0)
            {
                return MapToAutoCompleteLocations(result.Features);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(ex.ToString());
        }

        return [];
    }

    private static IEnumerable<AutoCompleteLocation> MapToAutoCompleteLocations(List<Feature> features)
    {
        return features
            .Where(f => f.Properties != null && (f.Properties.FeatureType == null || !f.Properties.FeatureType.Equals("country")))
            .Select(f => new AutoCompleteLocation(
                f.Properties.NamePreferred,
                f.Properties.Context.Region?.Name ?? "",
                $"[{string.Join(",", f.Properties.Bbox)}]-{f.Properties.Coordinates.Latitude}-{f.Properties.Coordinates.Longitude}",
                f.Properties.Context.Country.CountryCode,
                AutoCompleteType.location));
    }
}
