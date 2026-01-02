namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for base rate properties.
/// </summary>
public abstract class WHBaseRate : WHBaseBoard
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("room")]
    public string RoomName { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public string RateName { get; set; } = string.Empty;

    [JsonPropertyName("rate_desc")]
    public string RateDescription { get; set; } = string.Empty;

    [JsonPropertyName("payment_policy")]
    public string PaymentPolicy { get; set; } = string.Empty;

    [JsonPropertyName("payment_policy_id")]
    public int? PaymentPolicyId { get; set; }

    [JsonPropertyName("cancellation_policy")]
    public string CancellationPolicy { get; set; } = string.Empty;

    [JsonPropertyName("cancellation_policy_id")]
    public int? CancellationPolicyId { get; set; }

    [JsonPropertyName("cancellation_penalty")]
    public string CancellationPenalty { get; set; } = string.Empty;

    [JsonConverter(typeof(WHNullableDateTimeConverter))]
    [JsonPropertyName("cancellation_expiry")]
    public DateTime? CancellationExpiry { get; set; }
}
