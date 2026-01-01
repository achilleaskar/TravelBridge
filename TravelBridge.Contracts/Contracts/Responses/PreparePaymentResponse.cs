

namespace TravelBridge.Contracts.Contracts.Responses
{
    public class PreparePaymentResponse : BaseWebHotelierResponse
    {
        [JsonPropertyName("orderCode")]
        public string OrderCode { get; set; }
    }
}
