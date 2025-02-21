using System.Text.Json.Serialization;
using TravelBridge.API.Helpers.Converters;
using TravelBridge.API.Models;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityData
    {
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("data")]
        public HotelInfo Data { get; set; }
    }

    public class HotelInfo
    {
        [JsonPropertyName("code")]
        public string Code { internal get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; }


        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("rates")]
        public List<HotelRate> Rates { get; set; }
    }

    public class HotelRate
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("room")]
        public string RoomName { get; set; }

        [JsonPropertyName("rate")]
        public string RateName { get; set; }

        [JsonPropertyName("rate_desc")]
        public string RateDescription { get; set; }

        [JsonPropertyName("payment_policy")]
        public string PaymentPolicy { get; set; }

        [JsonPropertyName("payment_policy_id")]
        public int PaymentPolicyId { get; set; }

        [JsonPropertyName("cancellation_policy")]
        public string CancellationPolicy { get; set; }

        [JsonPropertyName("cancellation_policy_id")]
        public int CancellationPolicyId { get; set; }

        [JsonPropertyName("cancellation_penalty")]
        public string CancellationPenalty { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("cancellation_expiry")]
        public DateTime? CancellationExpiry { get; set; }

        [JsonPropertyName("board")]
        public int BoardType { get; set; }

        [JsonPropertyName("pricing")]
        public PricingInfo Pricing { get; set; }

        [JsonPropertyName("retail")]
        public PricingInfo Retail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_descr")]
        public string StatusDescription { get; set; }

        [JsonPropertyName("labels")]
        public List<RateLabel> Labels { get; set; }
    }

    public class PricingInfo
    {
        [JsonPropertyName("stay")]
        public decimal StayPrice { get; set; }

        [JsonPropertyName("extras")]
        public decimal Extras { get; set; }

        [JsonPropertyName("taxes")]
        public decimal Taxes { get; set; }

        [JsonPropertyName("excluded_charges")]
        public decimal ExcludedCharges { get; set; }

        [JsonPropertyName("price")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }
        
        [JsonPropertyName("margin")]
        public decimal Margin { get; set; }
    }

    public class RateLabel
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}