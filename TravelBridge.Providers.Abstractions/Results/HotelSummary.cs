namespace TravelBridge.Providers.Abstractions.Results;

/// <summary>
/// Summary info for a hotel in autocomplete/listing results.
/// This is an internal result type - the API endpoint maps this to AutoCompleteHotel (Contracts).
/// </summary>
public class HotelSummary
{
    /// <summary>
    /// Composite hotel ID (e.g., "wh:VAROSRESID" or "owned:123").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Provider-specific hotel code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Hotel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Star rating (1-5, null if not rated).
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Hotel type (e.g., "Hotel", "Apartment", "Villa").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// City/location name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Photo URL.
    /// </summary>
    public string? Photo { get; set; }

    /// <summary>
    /// Location coordinates.
    /// </summary>
    public HotelLocation? Location { get; set; }

    /// <summary>
    /// Provider source for this hotel.
    /// </summary>
    public AvailabilitySource Source { get; set; }
}
