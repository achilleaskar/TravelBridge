using System.Text.Json.Serialization;

namespace TravelBridge.API.Models.WebHotelier
{
    public class BaseWebHotelierResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMessage { get; set; }
    }
}