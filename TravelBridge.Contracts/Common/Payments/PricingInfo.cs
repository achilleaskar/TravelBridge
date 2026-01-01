namespace TravelBridge.Contracts.Common.Payments
{
    public class PricingInfo
    {
        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("excluded_charges")]
        public decimal ExcludedCharges { get; set; }

        [JsonPropertyName("extras")]
        public decimal Extras { get; set; }

        [JsonPropertyName("margin")]
        public decimal Margin { get; set; }

        [JsonPropertyName("stay")]
        public decimal StayPrice { get; set; }

        [JsonPropertyName("taxes")]
        public decimal Taxes { get; set; }

        [JsonPropertyName("price")]
        public decimal TotalPrice { get; set; }
    }
}
