namespace TravelBridge.Providers.Abstractions.Results;

/// <summary>
/// Result of a single hotel availability query.
/// This is an internal result type - the API endpoint maps this to SingleAvailabilityResponse (Contracts).
/// </summary>
public class HotelAvailabilityResult
{
    /// <summary>
    /// Provider source for this result.
    /// </summary>
    public AvailabilitySource Source { get; set; }

    /// <summary>
    /// Hotel code/ID in the provider's format.
    /// </summary>
    public string HotelCode { get; set; } = string.Empty;

    /// <summary>
    /// Available rooms with rates.
    /// </summary>
    public List<RoomAvailability> Rooms { get; set; } = [];

    /// <summary>
    /// Alternative date suggestions if requested dates have no availability.
    /// </summary>
    public List<AlternativeDate> Alternatives { get; set; } = [];

    /// <summary>
    /// Error code if the query failed.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Error message if the query failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether coupon was applied.
    /// </summary>
    public bool CouponValid { get; set; }

    /// <summary>
    /// Coupon discount description.
    /// </summary>
    public string? CouponDiscount { get; set; }

    /// <summary>
    /// Whether the query was successful.
    /// </summary>
    public bool IsSuccess => string.IsNullOrEmpty(ErrorCode);

    /// <summary>
    /// Gets the minimum price across all rooms.
    /// </summary>
    public decimal GetMinPrice()
    {
        if (Rooms.Count == 0) return 0;
        return Rooms.SelectMany(r => r.Rates).Min(r => r.TotalPrice);
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static HotelAvailabilityResult Success(string hotelCode, List<RoomAvailability> rooms, AvailabilitySource source) => new()
    {
        HotelCode = hotelCode,
        Rooms = rooms,
        Source = source
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HotelAvailabilityResult Failure(string errorCode, string errorMessage) => new()
    {
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Room type with available rates.
/// </summary>
public class RoomAvailability
{
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
    /// Available rates for this room type.
    /// </summary>
    public List<RoomRate> Rates { get; set; } = [];

    /// <summary>
    /// Room photos.
    /// </summary>
    public List<string> Photos { get; set; } = [];
}

/// <summary>
/// A specific rate for a room type.
/// </summary>
public class RoomRate
{
    /// <summary>
    /// Rate ID (unique within this provider).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Rate plan name (e.g., "Non-Refundable", "Flexible").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Board type code (e.g., "RO", "BB", "HB", "FB", "AI").
    /// </summary>
    public string BoardCode { get; set; } = string.Empty;

    /// <summary>
    /// Board type name (e.g., "Room Only", "Bed & Breakfast").
    /// </summary>
    public string BoardName { get; set; } = string.Empty;

    /// <summary>
    /// Total price for all nights.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Net price (before markup).
    /// </summary>
    public decimal NetPrice { get; set; }

    /// <summary>
    /// Original/retail price (for showing discounts).
    /// </summary>
    public decimal? RetailPrice { get; set; }

    /// <summary>
    /// Remaining rooms available at this rate.
    /// </summary>
    public int? RemainingRooms { get; set; }

    /// <summary>
    /// Whether this rate is refundable.
    /// </summary>
    public bool IsRefundable { get; set; }

    /// <summary>
    /// Cancellation policy description.
    /// </summary>
    public string? CancellationPolicy { get; set; }

    /// <summary>
    /// Party configuration this rate is for.
    /// </summary>
    public string? SearchPartyJson { get; set; }

    /// <summary>
    /// Price breakdown by date.
    /// </summary>
    public List<DailyPrice> DailyPrices { get; set; } = [];

    /// <summary>
    /// Payment information.
    /// </summary>
    public PaymentInfo? Payment { get; set; }
}

/// <summary>
/// Price for a single night.
/// </summary>
public class DailyPrice
{
    public DateOnly Date { get; set; }
    public decimal Price { get; set; }
}

/// <summary>
/// Payment information for a rate.
/// </summary>
public class PaymentInfo
{
    /// <summary>
    /// Amount due now.
    /// </summary>
    public decimal DueNow { get; set; }

    /// <summary>
    /// Amount due at hotel.
    /// </summary>
    public decimal DueAtHotel { get; set; }

    /// <summary>
    /// Whether partial payment is allowed.
    /// </summary>
    public bool AllowPartialPayment { get; set; }

    /// <summary>
    /// Minimum deposit percentage.
    /// </summary>
    public decimal? MinDepositPercent { get; set; }
}

/// <summary>
/// Alternative date suggestion.
/// </summary>
public class AlternativeDate
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal MinPrice { get; set; }
    public decimal NetPrice { get; set; }
}
