namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Query for retrieving hotel information.
/// </summary>
public sealed record HotelInfoQuery
{
    /// <summary>
    /// The provider-specific hotel ID (without the provider prefix).
    /// This is the <see cref="CompositeId.Value"/> part.
    /// </summary>
    public required string HotelId { get; init; }
}

/// <summary>
/// Query for retrieving room information.
/// </summary>
public sealed record RoomInfoQuery
{
    /// <summary>
    /// The provider-specific hotel ID (without the provider prefix).
    /// </summary>
    public required string HotelId { get; init; }

    /// <summary>
    /// The room identifier.
    /// </summary>
    public required string RoomId { get; init; }
}

/// <summary>
/// Query for retrieving hotel availability.
/// </summary>
public sealed record HotelAvailabilityQuery
{
    /// <summary>
    /// The provider-specific hotel ID (without the provider prefix).
    /// </summary>
    public required string HotelId { get; init; }

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
    /// Optional coupon code to apply.
    /// </summary>
    public string? CouponCode { get; init; }

    /// <summary>
    /// Number of nights for the stay.
    /// </summary>
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
}

/// <summary>
/// Query for retrieving full hotel information including availability.
/// </summary>
public sealed record HotelFullInfoQuery
{
    /// <summary>
    /// The provider-specific hotel ID (without the provider prefix).
    /// </summary>
    public required string HotelId { get; init; }

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
    /// Number of nights for the stay.
    /// </summary>
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;
}
