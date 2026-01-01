using TravelBridge.Contracts.Contracts;

namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class AlternativeDaysData : BaseWebHotelierResponse
    {
        [JsonPropertyName("data")]
        public AlternativesInfo Data { get; set; }

        public List<Alternative> Alternatives { get; internal set; }
    }
}
