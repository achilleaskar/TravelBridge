namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Query for single hotel availability.
/// Provider-neutral - maps from API request at endpoint, maps to provider-specific request in provider.
/// </summary>
/// <param name="ProviderHotelId">Hotel ID in the provider's format (e.g., "VAROSRESID" for WebHotelier)</param>
/// <param name="CheckIn">Check-in date</param>
/// <param name="CheckOut">Check-out date</param>
/// <param name="Parties">Room configurations (adults + children per room)</param>
/// <param name="CouponCode">Optional coupon code for discounts</param>
/// <param name="SelectedRates">Optional pre-selected rates (for checkout flow)</param>
public record AvailabilityQuery(
    string ProviderHotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    List<PartyConfiguration> Parties,
    string? CouponCode = null,
    List<SelectedRateInfo>? SelectedRates = null
);

/// <summary>
/// Information about a pre-selected rate (used in checkout flow).
/// </summary>
/// <param name="RateId">Rate identifier</param>
/// <param name="RoomId">Room type identifier</param>
/// <param name="Count">Number of rooms of this type</param>
/// <param name="SearchParty">Party configuration as JSON string</param>
public record SelectedRateInfo(
    string? RateId,
    string? RoomId,
    int Count,
    string SearchParty
);
