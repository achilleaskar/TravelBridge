namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    /// <summary>
    /// WebHotelier wire response for booking.
    /// </summary>
    public class WHBookingResponse : WHBaseResponse
    {
        public WHBookingData? data { get; set; }
    }

    /// <summary>
    /// WebHotelier wire model for booking data.
    /// </summary>
    public class WHBookingData
    {
        public string summaryUrl { get; set; } = string.Empty;
        public int res_id { get; set; }
        public string email { get; set; } = string.Empty;
        public string result { get; set; } = string.Empty;
    }
}
