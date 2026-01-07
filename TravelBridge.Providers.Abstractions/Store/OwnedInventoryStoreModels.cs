namespace TravelBridge.Providers.Abstractions.Store;

/// <summary>
/// Provider-facing DTO for owned hotel data.
/// Decouples provider from EF entities.
/// </summary>
public sealed record OwnedHotelStoreModel
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Type { get; init; }
    public int? Rating { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? City { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? CheckInTime { get; init; }
    public string? CheckOutTime { get; init; }
    public List<OwnedRoomTypeStoreModel> RoomTypes { get; init; } = [];
}

/// <summary>
/// Provider-facing DTO for owned room type data.
/// </summary>
public sealed record OwnedRoomTypeStoreModel
{
    public int Id { get; init; }
    public int HotelId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int MaxAdults { get; init; }
    public int MaxChildren { get; init; }
    public int MaxTotalOccupancy { get; init; }
    public decimal BasePricePerNight { get; init; }
    public int DefaultTotalUnits { get; init; }
}

/// <summary>
/// Provider-facing DTO for owned inventory daily data.
/// </summary>
public sealed record OwnedInventoryDailyStoreModel
{
    public int RoomTypeId { get; init; }
    public DateOnly Date { get; init; }
    public int TotalUnits { get; init; }
    public int ClosedUnits { get; init; }
    public int HeldUnits { get; init; }
    public int ConfirmedUnits { get; init; }
    public decimal? PricePerNight { get; init; }
    
    /// <summary>
    /// Computed available units: TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits.
    /// </summary>
    public int AvailableUnits => TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits;
}
