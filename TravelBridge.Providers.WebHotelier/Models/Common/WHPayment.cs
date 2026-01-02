namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// Internal WebHotelier wire model for payment schedules.
/// Maps to/from WebHotelier API JSON.
/// </summary>
public class WHPayment
{
    [JsonPropertyName("due")]
    public DateTime? DueDate { get; set; }

    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }
}
