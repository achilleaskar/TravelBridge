using System.Text.Json.Serialization;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class PreparePaymentResponse : BaseWebHotelierResponse
    {
        [JsonPropertyName("orderCode")]
        public string OrderCode { get; set; }
    }
}