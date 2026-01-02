namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for cancellation fees.
/// </summary>
public class WHCancellationFee
{
    [JsonConverter(typeof(WHNullableDateTimeConverter))]
    [JsonPropertyName("after")]
    public DateTime? After { get; set; }

    [JsonPropertyName("fee")]
    public decimal? Fee { get; set; }
}
