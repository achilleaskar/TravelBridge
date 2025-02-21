using System.Text.Json.Serialization;

namespace TravelBridge.API.Models.Plugin.AutoComplete
{
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