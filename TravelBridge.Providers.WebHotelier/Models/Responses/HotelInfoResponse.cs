using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class HotelInfoResponse
    {
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMsg { get; set; }

        [JsonPropertyName("data")]
        public HotelData Data { get; set; }
    }
}
