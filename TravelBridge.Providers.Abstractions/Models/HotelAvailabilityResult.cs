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
    
    /// <summary>
    /// Hotel name - needed to build SingleHotelAvailabilityInfo without another API call.
    /// </summary>
    public string? HotelName { get; init; }
    
    /// <summary>
    /// Hotel location - needed to build SingleHotelAvailabilityInfo.
    /// </summary>
    public AvailabilityLocationData? Location { get; init; }
    
    public IReadOnlyList<AvailableRoomData> Rooms { get; init; } = [];
    public IReadOnlyList<AlternativeDateData> Alternatives { get; init; } = [];
}

/// <summary>
/// Provider-neutral location data for availability responses.
/// </summary>
public sealed record AvailabilityLocationData
{
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? Name { get; init; }
}

/// <summary>
/// Provider-neutral pricing breakdown data.
/// Matches the structure needed by existing GetTotalPrice/GetSalePrice logic.
/// </summary>
public sealed record PricingInfoData
{
    public decimal Discount { get; init; }
    public decimal ExcludedCharges { get; init; }
    public decimal Extras { get; init; }
    public decimal Margin { get; init; }
    public decimal StayPrice { get; init; }
    public decimal Taxes { get; init; }
    public decimal TotalPrice { get; init; }
}

/// <summary>
/// Provider-neutral cancellation fee data.
/// </summary>
public sealed record CancellationFeeData
{
    public DateTime? After { get; init; }
    public decimal? Fee { get; init; }
}

/// <summary>
/// Provider-neutral payment schedule data.
/// </summary>
public sealed record PaymentData
{
    public DateTime? DueDate { get; init; }
    public decimal? Amount { get; init; }
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
/// Provider-neutral room rate data with full details needed for SingleAvailabilityResponse mapping.
/// </summary>
public sealed record RoomRateData
{
    /// <summary>
    /// The room code this rate belongs to.
    /// Used for grouping rates by room.
    /// </summary>
    public required string RoomCode { get; init; }

    public required string RateId { get; init; }
    public required string RateName { get; init; }
    
    /// <summary>
    /// Rate description for display.
    /// </summary>
    public string? RateDescription { get; init; }
    
    // Legacy convenience totals (kept for backward compatibility)
    public decimal TotalPrice { get; init; }
    public decimal NetPrice { get; init; }
    
    /// <summary>
    /// Full pricing breakdown - needed by GetTotalPrice/GetSalePrice logic.
    /// </summary>
    public PricingInfoData? Pricing { get; init; }
    
    /// <summary>
    /// Full retail pricing breakdown - needed by GetTotalPrice/GetSalePrice logic.
    /// </summary>
    public PricingInfoData? Retail { get; init; }
    
    public int RemainingRooms { get; init; }
    public int? BoardTypeId { get; init; }
    public string? BoardName { get; init; }
    
    public bool HasCancellation { get; init; }
    public DateTime? CancellationDeadline { get; init; }
    public string? CancellationPolicy { get; init; }
    public int? CancellationPolicyId { get; init; }
    public string? CancellationPenalty { get; init; }
    
    /// <summary>
    /// Cancellation fees schedule.
    /// </summary>
    public IReadOnlyList<CancellationFeeData> CancellationFees { get; init; } = [];
    
    /// <summary>
    /// Payment schedule.
    /// </summary>
    public IReadOnlyList<PaymentData> Payments { get; init; } = [];
    
    public string? PaymentPolicy { get; init; }
    public int? PaymentPolicyId { get; init; }
    
    public string? Status { get; init; }
    public string? StatusDescription { get; init; }
    
    /// <summary>
    /// Profit percentage - calculated during price processing.
    /// </summary>
    public decimal ProfitPerc { get; init; }
    
    public RatePartyInfo? SearchParty { get; init; }
}

/// <summary>
/// Provider-neutral party info associated with a rate.
/// </summary>
public sealed record RatePartyInfo
{
    public int Adults { get; init; }
    public int[] ChildrenAges { get; init; } = [];
    /// <summary>
    /// Number of rooms for this party configuration.
    /// Used for price accumulation, not included in RateId (to keep RateId compatible with booking flow).
    /// </summary>
    public int RoomsCount { get; init; } = 1;
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
