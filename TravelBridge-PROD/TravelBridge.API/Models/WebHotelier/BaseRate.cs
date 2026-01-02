using System.Text.Json.Serialization;
using TravelBridge.API.Helpers.Converters;

namespace TravelBridge.API.Models.WebHotelier
{
    public abstract class BaseRate : BaseBoard
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("room")]
        public string RoomName { get; set; }

        [JsonPropertyName("rate")]
        public string RateName { get; set; }

        [JsonPropertyName("rate_desc")]
        public string RateDescription{ get; set; }

        [JsonPropertyName("payment_policy")]
        public string PaymentPolicy { get; set; }

        [JsonPropertyName("payment_policy_id")]
        public int? PaymentPolicyId { get; set; }

        [JsonPropertyName("cancellation_policy")]
        public string CancellationPolicy { get; set; }

        [JsonPropertyName("cancellation_policy_id")]
        public int? CancellationPolicyId { get; set; }

        [JsonPropertyName("cancellation_penalty")]
        public string CancellationPenalty { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("cancellation_expiry")]
        public DateTime? CancellationExpiry { get; set; }

        //[JsonPropertyName("labels")]
        //public IEnumerable<RateLabel> Labels { get; set; }
    }

    public class RateLabel
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}
