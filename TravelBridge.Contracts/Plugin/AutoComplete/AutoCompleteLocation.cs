namespace TravelBridge.Contracts.Plugin.AutoComplete
{
    /// <summary>
    /// Used in: SearchPluginEndpoints.GetAutocompleteResults()
    /// Created by: MappingExtensions.MapToAutoCompleteLocations() - maps from MapBox Feature[]
    /// Returned in: AutoCompleteResponse (GET /api/plugin/autocomplete)
    /// Source: MapBoxService.GetLocations() retrieves locations from MapBox API
    /// Purpose: API response model representing a location in autocomplete results
    /// </summary>
    public class AutoCompleteLocation(string name, string region, string bBox, string countryCode, AutoCompleteType? type)
    {
        public string Name { get; set; } = name;
        public string Region { get; set; } = region;
        public string Id { get; set; } = bBox;

        public string CountryCode { get; set; } = countryCode;
        public string Type { get; set; } = AutoCompleteType.location.ToString();
    }
}
