using System.Text.Json.Serialization;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts
{
    /// <summary>
    /// WebHotelier booking response.
    /// </summary>
    public class WHBookingResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHBookingData? Data { get; set; }
    }

    public class WHBookingData
    {
        [JsonPropertyName("res_id")]
        public int ResId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
