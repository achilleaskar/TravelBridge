using TravelBridge.Providers.WebHotelier.Models.Room;

namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class RoomInfoResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("data")]
        public RoomInfo Data { get; set; }
    }
}
