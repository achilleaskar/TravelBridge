using System.Text.Json;
using Microsoft.Extensions.Options;
using TravelBridge.API.Models;
using TravelBridge.API.Models.Apis;
using TravelBridge.API.Models.ExternalModels;
using TravelBridge.API.Models.Plugin.AutoComplete;

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

        public async Task<IEnumerable<AutoCompleteLocation>> GetLocations(string? param, string? lang)
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
                        return MapResultsToLocations(result.Features);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException(ex.ToString());
                }
            }
            return [];
        }

        private static IEnumerable<AutoCompleteLocation> MapResultsToLocations(List<Feature> features)
        {
            //TODO: handle error
            return features.Select(f =>
            new AutoCompleteLocation(
                f.Properties.NamePreferred,
                f.Properties.Context.Region.Name,
                $"[{string.Join(",", f.Properties.Bbox)}]-{f.Properties.Coordinates.Latitude}-{f.Properties.Coordinates.Longitude}",
                f.Properties.Context.Country.CountryCode,
                AutoCompleteType.location));
        }
    }
}
