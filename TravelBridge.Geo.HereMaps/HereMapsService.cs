using System.Text.Json;
using Microsoft.Extensions.Options;

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

    public async Task<IEnumerable<string>> GetLocationsAsync(string? param)
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
                return result.Items.Select(r => r.Address.Label);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(ex.ToString());
        }

        return [];
    }
}
