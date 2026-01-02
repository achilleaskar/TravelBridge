namespace TravelBridge.API.Models.Plugin.AutoComplete
{
    public class AutoCompleteLocation(string name, string region, string bBox, string countryCode, AutoCompleteType? type)
    {
        public string Name { get; set; } = name;
        public string Region { get; set; } = region;
        public string Id { get; set; } = bBox;

        public string CountryCode { get; set; } = countryCode;
        public string Type { get; set; } = AutoCompleteType.location.ToString();
    }
}
