namespace TravelBridge.Contracts.Models.Hotels
{
    public class PhotoInfo
    {
        [JsonPropertyName("medium")]
        public string Medium { get; set; }

        [JsonPropertyName("large")]
        public string Large { get; set; }
    }
}
