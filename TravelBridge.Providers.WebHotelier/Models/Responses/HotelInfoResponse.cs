namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    /// <summary>
    /// WebHotelier wire response for hotel info.
    /// </summary>
    public class WHHotelInfoResponse : WHBaseResponse
    {
        [JsonPropertyName("data")]
        public WHHotelData? Data { get; set; }
    }
}
