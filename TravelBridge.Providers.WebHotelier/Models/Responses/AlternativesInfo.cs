namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    /// <summary>
    /// WebHotelier wire model for alternatives info container.
    /// </summary>
    public class WHAlternativesInfo
    {
        public List<WHAlternativeDayInfo> days { get; set; } = [];
    }
}
