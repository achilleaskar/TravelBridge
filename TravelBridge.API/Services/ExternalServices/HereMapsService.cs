using Microsoft.Extensions.Options;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Models.ExternalModels;

namespace TravelBridge.API.Services.ExternalServices
{
    public class HereMapsService
    {
        public HereMapsService(IHttpClientFactory httpClientFactory, IOptions<HereMapsApiOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("HereMapsApi");
            _apiKey = options.Value.ApiKey;
        }

        private const string countryCodes = "CYP,GRC";
        private const int limit = 20;
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public async Task<IEnumerable<string>> GetLocations(string? param)
        {
            if (param is not null)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"autocomplete" +
                        $"?q={param}" +
                        $"&in=countryCode:{countryCodes}" +
                        $"&limit={limit}" +
                        $"&apiKey={_apiKey}");

                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<HereMapsAutoCompleteResponse>(jsonString);

                    if (result?.Items.Count > 0)
                        return result.Items.Select(r => r.Address.Label);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException(ex.ToString());
                }
            }
            return [];
        }
    }
}