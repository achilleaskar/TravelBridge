using System.Text.Json;
using Microsoft.Extensions.Options;

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
    /// Get location features from MapBox API
    /// </summary>
    /// <returns>List of Feature objects from MapBox API</returns>
    public async Task<List<Feature>> GetLocationsAsync(string? param, string? lang)
    {
        if (param is null)
        {
            return [];
        }

        try
        {
            // Language can be el or en or both for database storing purposes
            var response = await _httpClient.GetAsync(
                $"search/geocode/v6/forward" +
                $"?q={Uri.EscapeDataString(param)}" +
                $"&country=gr,cy" +
                $"&limit={Limit}" +
                $"&types=neighborhood,region,country,place,district,locality" +
                $"&language=el" +
                $"&autocomplete=true" +
                $"&access_token={_apiKey}" +
                $"&permanent=false");

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<MapBoxAutoCompleteResponse>(jsonString);

            if (result?.Features?.Count > 0)
            {
                return result.Features;
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(ex.ToString());
        }

        return [];
    }
}
