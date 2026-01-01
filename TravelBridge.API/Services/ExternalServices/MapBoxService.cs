using Microsoft.Extensions.Options;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Models.ExternalModels;

namespace TravelBridge.API.Services.ExternalServices
{
    public class MapBoxService
    {
        public MapBoxService(IHttpClientFactory httpClientFactory, IOptions<MapBoxApiOptions> options)
        {
            _httpClient = httpClientFactory.CreateClient("MapBoxApi");
            _apiKey = options.Value.ApiKey;
        }

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly int limit = 10;

        /// <summary>
        /// Get location features from MapBox API
        /// </summary>
        /// <returns>List of Feature objects from MapBox API</returns>
        public async Task<List<Feature>> GetLocations(string? param, string? lang)
        {
            if (param is not null)
            {
                try
                {
                    //here we can use language = el or en or both for database storing purposes
                    var response = await _httpClient.GetAsync(
                                 $"search/geocode/v6/forward" +
                                 $"?q={Uri.EscapeDataString(param)}" +
                                 $"&country=gr,cy" +
                                 $"&limit={limit}" +
                                 $"&types=neighborhood,region,country,place,district,locality" +
                                 $"&language=el" +
                                 $"&autocomplete=true" +
                                 $"&access_token={_apiKey}" +
                                 $"&permanent=false"
                             );

                    response.EnsureSuccessStatusCode();
                    var jsonString = await response.Content.ReadAsStringAsync();

                    var result = JsonSerializer.Deserialize<MapBoxAutoCompleteResponse>(jsonString);

                    if (result?.Features?.Count > 0)
                        return result.Features;
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException(ex.ToString());
                }
                catch (Exception ex)
                {

                }
            }
            return [];
        }
    }
}