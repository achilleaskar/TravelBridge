namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Bounding box for geographic search.
/// </summary>
public sealed record BoundingBox
{
    public required double BottomLeftLatitude { get; init; }
    public required double TopRightLatitude { get; init; }
    public required double BottomLeftLongitude { get; init; }
    public required double TopRightLongitude { get; init; }
}

/// <summary>
/// Query for searching hotel availability across multiple properties.
/// </summary>
public sealed record SearchAvailabilityQuery
{
    /// <summary>
    /// Check-in date.
    /// </summary>
    public required DateOnly CheckIn { get; init; }

    /// <summary>
    /// Check-out date.
    /// </summary>
    public required DateOnly CheckOut { get; init; }

    /// <summary>
    /// The party configuration (rooms and guests).
    /// </summary>
    public required PartyConfiguration Party { get; init; }

    /// <summary>
    /// Geographic bounding box for the search area.
    /// </summary>
    public required BoundingBox BoundingBox { get; init; }

    /// <summary>
    /// Center latitude of the search area.
    /// </summary>
    public required double CenterLatitude { get; init; }

    /// <summary>
    /// Center longitude of the search area.
    /// </summary>
    public required double CenterLongitude { get; init; }

    /// <summary>
    /// Sort field (e.g., "PRICE", "DISTANCE", "POPULARITY").
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// Sort order ("ASC" or "DESC").
    /// </summary>
    public string? SortOrder { get; init; }

    /// <summary>
    /// Number of nights for the stay.
    /// </summary>
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
}

/// <summary>
/// Provider-neutral result for multi-hotel availability search.
/// </summary>
public sealed record SearchAvailabilityResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// List of available hotels.
    /// </summary>
    public IReadOnlyList<HotelSummaryData> Hotels { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static SearchAvailabilityResult Success(IReadOnlyList<HotelSummaryData> hotels) => new()
    {
        IsSuccess = true,
        Hotels = hotels
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static SearchAvailabilityResult Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Provider-neutral hotel summary for search results.
/// </summary>
public sealed record HotelSummaryData
{
    /// <summary>
    /// Provider-specific hotel code (without prefix).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Hotel name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Hotel rating (stars).
    /// </summary>
    public int? Rating { get; init; }

    /// <summary>
    /// Minimum price for the stay.
    /// </summary>
    public decimal? MinPrice { get; init; }

    /// <summary>
    /// Minimum price per night.
    /// </summary>
    public decimal? MinPricePerNight { get; init; }

    /// <summary>
    /// Sale/crossed-out price (if applicable).
    /// </summary>
    public decimal? SalePrice { get; init; }

    /// <summary>
    /// Medium photo URL.
    /// </summary>
    public string? PhotoMedium { get; init; }

    /// <summary>
    /// Large photo URL.
    /// </summary>
    public string? PhotoLarge { get; init; }

    /// <summary>
    /// Distance from search center (in km or provider unit).
    /// </summary>
    public decimal? Distance { get; init; }

    /// <summary>
    /// Hotel location.
    /// </summary>
    public HotelLocationData? Location { get; init; }

    /// <summary>
    /// Original hotel type from provider.
    /// </summary>
    public string? OriginalType { get; init; }

    /// <summary>
    /// Available rates for this hotel.
    /// </summary>
    public IReadOnlyList<HotelRateSummary> Rates { get; init; } = [];

    /// <summary>
    /// Party info associated with this search result.
    /// </summary>
    public RatePartyInfo? SearchParty { get; init; }
}

/// <summary>
/// Provider-neutral rate summary for search results.
/// </summary>
public sealed record HotelRateSummary
{
    public required string RateId { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal NetPrice { get; init; }
    public int? BoardTypeId { get; init; }
    public string? BoardName { get; init; }
    public RatePartyInfo? SearchParty { get; init; }
}
