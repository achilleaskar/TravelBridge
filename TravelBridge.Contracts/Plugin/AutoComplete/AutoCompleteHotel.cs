namespace TravelBridge.Contracts.Plugin.AutoComplete
{
    /// <summary>
    /// Used in: SearchPluginEndpoints.GetAutocompleteResults(), SearchPluginEndpoints.GetAllProperties()
    /// Created by: MappingExtensions.MapToAutoCompleteHotels() - maps from WebHotelier Hotel[]
    /// Returned in: AutoCompleteResponse (GET /api/plugin/autocomplete, GET /api/plugin/allproperties)
    /// Purpose: API response model representing a hotel in autocomplete results
    /// </summary>
    public class AutoCompleteHotel(string id, Provider provider, string name, string location, string countryCode, string type)
    {
        [JsonIgnore]
        public string OrId { get; set; } = id;

        [JsonIgnore]
        public Provider Provider { get; set; } = provider;

        public string Id => $"{(int)Provider}-{OrId}";

        public string Name { get; set; } = name;

        public string Location { get; set; } = location;

        public string CountryCode { get; set; } = countryCode;

        public string OriginalType { get; set; } = type;

        public HashSet<string> MappedTypes { get; set; }
    }
}
