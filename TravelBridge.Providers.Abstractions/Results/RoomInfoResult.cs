namespace TravelBridge.Providers.Abstractions.Results;

/// <summary>
/// Result of a room info query.
/// This is an internal result type - the API endpoint maps this to RoomInfoResponse (Contracts).
/// </summary>
public class RoomInfoResult
{
    /// <summary>
    /// Provider source for this result.
    /// </summary>
    public AvailabilitySource Source { get; set; }

    /// <summary>
    /// Room type code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Room type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Room description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Maximum adults.
    /// </summary>
    public int MaxAdults { get; set; }

    /// <summary>
    /// Maximum children.
    /// </summary>
    public int MaxChildren { get; set; }

    /// <summary>
    /// Maximum total occupancy.
    /// </summary>
    public int MaxOccupancy { get; set; }

    /// <summary>
    /// Room size in square meters.
    /// </summary>
    public decimal? SizeSquareMeters { get; set; }

    /// <summary>
    /// Bed configuration description.
    /// </summary>
    public string? BedConfiguration { get; set; }

    /// <summary>
    /// Photo URLs.
    /// </summary>
    public List<string> Photos { get; set; } = [];

    /// <summary>
    /// Room amenities/facilities.
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
    public static RoomInfoResult Failure(string errorCode, string errorMessage) => new()
    {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
