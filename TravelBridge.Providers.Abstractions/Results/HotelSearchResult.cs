namespace TravelBridge.Providers.Abstractions.Results;

/// <summary>
/// Result of a multi-hotel search operation.
/// This is an internal result type - the API endpoint maps this to PluginSearchResponse (Contracts).
/// </summary>
public class HotelSearchResult
{
    /// <summary>
    /// List of hotels matching the search criteria.
    /// </summary>
    public List<HotelSearchItem> Hotels { get; set; } = [];

    /// <summary>
    /// Total count of results (before pagination).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Error code if the search failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if the search failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the search was successful.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(ErrorCode);

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static HotelSearchResult Success(List<HotelSearchItem> hotels) => new()
    {
        Hotels = hotels,
        TotalCount = hotels.Count
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HotelSearchResult Failure(string errorCode, string errorMessage) => new()
    {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// A hotel item in search results.
/// Contains summary info + availability/pricing for the search dates.
/// </summary>
public class HotelSearchItem
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
    /// Minimum price for the search dates.
    /// </summary>
    public decimal MinPrice { get; set; }

    /// <summary>
    /// Minimum price per night.
    /// </summary>
    public decimal MinPricePerNight { get; set; }

    /// <summary>
    /// Original/sale price (before discount).
    /// </summary>
    public decimal? SalePrice { get; set; }

    /// <summary>
    /// Distance from search center in km.
    /// </summary>
    public decimal? Distance { get; set; }

    /// <summary>
    /// Medium-sized photo URL.
    /// </summary>
    public string? PhotoMedium { get; set; }

    /// <summary>
    /// Large photo URL.
    /// </summary>
    public string? PhotoLarge { get; set; }

    /// <summary>
    /// Hotel location coordinates.
    /// </summary>
    public HotelLocation? Location { get; set; }

    /// <summary>
    /// Available rates for the search dates.
    /// </summary>
    public List<RateInfo> Rates { get; set; } = [];

    /// <summary>
    /// Board types available (e.g., "RO", "BB", "HB").
    /// </summary>
    public List<BoardInfo> Boards { get; set; } = [];

    /// <summary>
    /// Provider source for this hotel.
    /// </summary>
    public AvailabilitySource Source { get; set; }
}

/// <summary>
/// Hotel location coordinates.
/// </summary>
public class HotelLocation
{
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}

/// <summary>
/// Rate information in search results.
/// </summary>
public class RateInfo
{
    public string Id { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string RatePlan { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int? RemainingRooms { get; set; }
    public string? BoardType { get; set; }
}

/// <summary>
/// Board type information.
/// </summary>
public class BoardInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
