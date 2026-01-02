namespace TravelBridge.Contracts.Common
{
    public class Location
    {
        [JsonPropertyName("lat")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
