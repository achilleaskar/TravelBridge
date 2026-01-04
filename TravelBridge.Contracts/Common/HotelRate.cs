using TravelBridge.Contracts.Common.Payments;
using TravelBridge.Contracts.Common.Policies;

namespace TravelBridge.Contracts.Common;

/// <summary>
/// Rate information for single hotel availability.
/// API contract type.
/// </summary>
public class HotelRate : BaseRate
{
    #region Fields

    public decimal totalPrice;

    #endregion Fields

    #region Properties

    [JsonPropertyName("cancellation_fees")]
    public IEnumerable<CancellationFee>? CancellationFees { get; set; }

    [JsonPropertyName("payments")]
    public List<PaymentWH>? Payments { get; set; }

    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntToStringJsonConverter))]
    public string? Id { get; set; }

    [JsonPropertyName("pricing")]
    public PricingInfo? Pricing { get; set; }

    [JsonPropertyName("remaining")]
    public int? RemainingRooms { get; set; }

    [JsonPropertyName("retail")]
    public PricingInfo? Retail { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("status_descr")]
    public string? StatusDescription { get; set; }

    #endregion Properties

    #region Methods

    public decimal ProfitPerc { get; set; }
    public PartyItem? SearchParty { get; set; }

    #endregion Methods
}
