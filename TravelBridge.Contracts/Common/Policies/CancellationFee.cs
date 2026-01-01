namespace TravelBridge.Contracts.Common.Policies
{
    public class CancellationFee
    {
        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("after")]
        public DateTime? After { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }
    }
}
