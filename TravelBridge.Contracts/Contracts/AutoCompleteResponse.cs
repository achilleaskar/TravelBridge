using TravelBridge.Contracts.Plugin.AutoComplete;

namespace TravelBridge.Contracts.Contracts
{
    /// <summary>
    /// Used in: SearchPluginEndpoints.GetAutocompleteResults()
    /// Returned by: GET /api/plugin/autocomplete endpoint
    /// Purpose: API response model that combines hotel and location autocomplete results
    /// Contains: AutoCompleteHotel[] (from WebHotelier) and AutoCompleteLocation[] (from MapBox)
    /// </summary>
    public class AutoCompleteResponse
    {
        public IEnumerable<AutoCompleteHotel> Hotels { get; set; }
        public IEnumerable<AutoCompleteLocation> Locations { get; set; }
    }
}
