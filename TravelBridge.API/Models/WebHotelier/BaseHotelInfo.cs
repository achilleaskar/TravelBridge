using System.Text.Json.Serialization;

namespace TravelBridge.API.Models.WebHotelier
{
    public class BaseHotelInfo
    {
        [JsonPropertyName("code")]
        public string Code { internal get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
