namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    /// <summary>
    /// WebHotelier wire model for multi-availability data container.
    /// </summary>
    public class WHData
    {
        [JsonPropertyName("hotels")]
        public IEnumerable<WHWebHotel> Hotels { get; set; } = [];
    }
}
