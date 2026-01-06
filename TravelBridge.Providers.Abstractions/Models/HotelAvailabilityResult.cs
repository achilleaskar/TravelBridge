namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Provider-neutral result for hotel availability.
/// </summary>
public sealed record HotelAvailabilityResult
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
    /// Availability data if the operation succeeded.
    /// </summary>
    public HotelAvailabilityData? Data { get; init; }

    /// <summary>
    /// Whether a coupon was applied and valid.
    /// </summary>
    public bool CouponValid { get; init; }

    /// <summary>
    /// Discount string if a coupon was applied.
    /// </summary>
    public string? CouponDiscount { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static HotelAvailabilityResult Success(HotelAvailabilityData data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HotelAvailabilityResult Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Provider-neutral hotel availability data.
/// </summary>
public sealed record HotelAvailabilityData
{
    public required string HotelCode { get; init; }
    public IReadOnlyList<AvailableRoomData> Rooms { get; init; } = [];
    public IReadOnlyList<AlternativeDateData> Alternatives { get; init; } = [];
}

/// <summary>
/// Provider-neutral available room data.
/// </summary>
public sealed record AvailableRoomData
{
    public required string RoomCode { get; init; }
    public required string RoomName { get; init; }
    public string? RoomType { get; init; }
    public IReadOnlyList<RoomRateData> Rates { get; init; } = [];
}

/// <summary>
/// Provider-neutral room rate data.
/// </summary>
public sealed record RoomRateData
{
    public required string RateId { get; init; }
    public required string RateName { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal NetPrice { get; init; }
    public int RemainingRooms { get; init; }
    public int? BoardTypeId { get; init; }
    public string? BoardName { get; init; }
    public bool HasCancellation { get; init; }
    public DateTime? CancellationDeadline { get; init; }
    public string? CancellationPolicy { get; init; }
    public RatePartyInfo? SearchParty { get; init; }
}

/// <summary>
/// Provider-neutral party info associated with a rate.
/// </summary>
public sealed record RatePartyInfo
{
    public int Adults { get; init; }
    public int[] ChildrenAges { get; init; } = [];
    public string? PartyJson { get; init; }
}

/// <summary>
/// Provider-neutral alternative date data.
/// </summary>
public sealed record AlternativeDateData
{
    public DateOnly CheckIn { get; init; }
    public DateOnly CheckOut { get; init; }
    public int Nights { get; init; }
    public decimal MinPrice { get; init; }
    public decimal NetPrice { get; init; }
}
