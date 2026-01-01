using System.Text.Json.Serialization;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Models
{
    /// <summary>
    /// WebHotelier properties search response.
    /// </summary>
    public class WHPropertiesResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHPropertiesData? Data { get; set; }
    }

    public class WHPropertiesData
    {
        [JsonPropertyName("hotels")]
        public WHHotelBasic[]? Hotels { get; set; }
    }

    public class WHHotelBasic
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("location")]
        public WHHotelLocation? Location { get; set; }
    }

    public class WHHotelLocation
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }
}
