namespace TravelBridge.Providers.WebHotelier.Models.Rate;

/// <summary>
/// WebHotelier wire model for multi-availability rate (simplified rate info).
/// </summary>
public class WHMultiRate : WHBaseRate
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
    public WHPartyItem? SearchParty { get; set; }

    [JsonPropertyName("remaining")]
    public int? Remaining { get; set; }
}
