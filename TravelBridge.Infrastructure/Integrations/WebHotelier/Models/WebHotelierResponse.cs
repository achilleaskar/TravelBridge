using System.Text.Json.Serialization;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Models
{
    /// <summary>
    /// Base response from WebHotelier API.
    /// </summary>
    public class WebHotelierResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string? ErrorMessage { get; set; }
    }
}
