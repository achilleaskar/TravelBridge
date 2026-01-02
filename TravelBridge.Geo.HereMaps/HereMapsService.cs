using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Plugin.AutoComplete;

namespace TravelBridge.Geo.HereMaps;

public class HereMapsService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string CountryCodes = "CYP,GRC";
    private const int Limit = 20;

    public HereMapsService(IHttpClientFactory httpClientFactory, IOptions<HereMapsApiOptions> options)
    {
        _httpClient = httpClientFactory.CreateClient("HereMapsApi");
        _apiKey = options.Value.ApiKey;
    }

    /// <summary>
    /// Get location suggestions from HERE Maps API
    /// </summary>
    /// <returns>List of AutoCompleteLocation objects</returns>
    public async Task<IEnumerable<AutoCompleteLocation>> GetLocationsAsync(string? param)
    {
        if (param is null)
        {
            return [];
        }

        try
        {
            var response = await _httpClient.GetAsync(
                $"autocomplete" +
                $"?q={param}" +
                $"&in=countryCode:{CountryCodes}" +
                $"&limit={Limit}" +
                $"&apiKey={_apiKey}");

            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<HereMapsAutoCompleteResponse>(jsonString);

            if (result?.Items.Count > 0)
            {
                return MapToAutoCompleteLocations(result.Items);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(ex.ToString());
        }

        return [];
    }

    private static IEnumerable<AutoCompleteLocation> MapToAutoCompleteLocations(List<Item> items)
    {
        return items.Select(item => new AutoCompleteLocation(
            item.Address.Label,
            item.Address.State ?? item.Address.County ?? "",
            "", // HereMaps autocomplete doesn't return bbox/coordinates
            item.Address.CountryName ?? "GR",
            AutoCompleteType.location));
    }
}
