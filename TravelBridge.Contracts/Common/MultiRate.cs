namespace TravelBridge.Contracts.Common
{
    public class MultiRate : BaseRate
    {
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("retail")]
        public decimal? Retail { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("margin")]
        public decimal? Margin { get; set; }

        [JsonIgnore]
        public PartyItem? SearchParty { get; set; }

        [JsonPropertyName("remaining")]
        public int? Remaining { get; set; }
    }
}
