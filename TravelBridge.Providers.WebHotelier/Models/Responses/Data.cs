
namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class Data
    {
        [JsonPropertyName("hotels")]
        public IEnumerable<WebHotel> Hotels { get; set; }
    }
}
