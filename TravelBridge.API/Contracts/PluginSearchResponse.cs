using TravelBridge.API.Models.Plugin.Filters;

namespace TravelBridge.API.Contracts
{
    public class PluginSearchResponse
    {
        public string SearchTerm { get; set; }
        public IEnumerable<WebHotel> Results { get; set; }
        public int ResultsCount { get; set; }
        public List<Filter> Filters { get; set; }
    }
}