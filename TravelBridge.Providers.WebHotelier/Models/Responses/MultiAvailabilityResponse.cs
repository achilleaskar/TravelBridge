using TravelBridge.Contracts.Contracts;

namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class MultiAvailabilityResponse : BaseWebHotelierResponse
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }
}
