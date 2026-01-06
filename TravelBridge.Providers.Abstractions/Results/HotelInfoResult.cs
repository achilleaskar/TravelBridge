namespace TravelBridge.Providers.Abstractions.Results;

/// <summary>
/// Result of a hotel info query.
/// This is an internal result type - the API endpoint maps this to HotelInfoResponse (Contracts).
/// </summary>
public class HotelInfoResult
{
    /// <summary>
    /// Provider source for this result.
    /// </summary>
    public AvailabilitySource Source { get; set; }

    /// <summary>
    /// Hotel code/ID in the provider's format.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Hotel name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hotel description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Star rating (1-5).
    /// </summary>
    public int? Rating { get; set; }

    /// <summary>
    /// Hotel type (e.g., "Hotel", "Apartment").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Hotel address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Country.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Location coordinates.
    /// </summary>
    public HotelLocation? Location { get; set; }

    /// <summary>
    /// Contact email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Contact phone.
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Hotel website.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Check-in time (e.g., "14:00").
    /// </summary>
    public string? CheckInTime { get; set; }

    /// <summary>
    /// Check-out time (e.g., "11:00").
    /// </summary>
    public string? CheckOutTime { get; set; }

    /// <summary>
    /// Photo URLs.
    /// </summary>
    public List<string> Photos { get; set; } = [];

    /// <summary>
    /// Amenities/facilities.
    /// </summary>
    public List<string> Amenities { get; set; } = [];

    /// <summary>
    /// Error code if the query failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if the query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(ErrorCode);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HotelInfoResult Failure(string errorCode, string errorMessage) => new()
    {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
