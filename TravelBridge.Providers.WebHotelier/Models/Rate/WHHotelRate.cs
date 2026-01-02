namespace TravelBridge.Providers.WebHotelier.Models.Rate;

/// <summary>
/// WebHotelier wire model for hotel rate with full pricing.
/// Uses only internal WH types - no Contracts dependencies.
/// </summary>
public class WHHotelRate : WHBaseRate
{
    #region Fields

    public decimal totalPrice;

    #endregion Fields

    #region Properties

    [JsonPropertyName("cancellation_fees")]
    public IEnumerable<WHCancellationFee> CancellationFees { get; set; } = [];

    [JsonPropertyName("payments")]
    public List<WHPayment>? Payments { get; set; }

    [JsonPropertyName("id")]
    [JsonConverter(typeof(WHIntToStringJsonConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("pricing")]
    public WHPricingInfo Pricing { get; set; } = new();

    [JsonPropertyName("remaining")]
    public int? RemainingRooms { get; set; }

    [JsonPropertyName("retail")]
    public WHPricingInfo Retail { get; set; } = new();

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("status_descr")]
    public string StatusDescription { get; set; } = string.Empty;

    #endregion Properties

    #region Methods

    public decimal ProfitPerc { get; set; }
    public WHPartyItem? SearchParty { get; set; }

    #endregion Methods
}
