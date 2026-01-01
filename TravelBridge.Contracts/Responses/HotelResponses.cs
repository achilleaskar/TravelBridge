namespace TravelBridge.Contracts.Responses
{
    /// <summary>
    /// Hotel detail response.
    /// </summary>
    public class HotelDetailResponse
    {
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public HotelInfo? Hotel { get; init; }
        public IReadOnlyList<RoomAvailability> Rooms { get; init; } = [];
        public IReadOnlyList<AlternativeDate> Alternatives { get; init; } = [];
    }

    /// <summary>
    /// Full hotel information.
    /// </summary>
    public class HotelInfo
    {
        public required string Id { get; init; }
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public string? Type { get; init; }
        public int? Rating { get; init; }
        public HotelLocation? Location { get; init; }
        public HotelOperation? Operation { get; init; }
        public IReadOnlyList<string> Photos { get; init; } = [];
        public IReadOnlyList<string> Facilities { get; init; } = [];
        public IReadOnlyList<string> MappedTypes { get; init; } = [];
        public IReadOnlyList<BoardInfo> Boards { get; init; } = [];
        public string? BoardsText { get; init; }
        public bool HasBoards { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MinPricePerNight { get; init; }
        public decimal? SalePrice { get; init; }
        public string? CustomInfo { get; init; }
    }

    /// <summary>
    /// Hotel location details.
    /// </summary>
    public class HotelLocation
    {
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
        public string? ZipCode { get; init; }
        public string? Country { get; init; }
    }

    /// <summary>
    /// Hotel check-in/out times.
    /// </summary>
    public class HotelOperation
    {
        public string? CheckInTime { get; init; }
        public string? CheckOutTime { get; init; }
    }

    /// <summary>
    /// Room availability info.
    /// </summary>
    public class RoomAvailability
    {
        public required string RoomType { get; init; }
        public required string RoomName { get; init; }
        public IReadOnlyList<RoomRate> Rates { get; init; } = [];
    }

    /// <summary>
    /// Room rate details.
    /// </summary>
    public class RoomRate
    {
        public required string Id { get; init; }
        public decimal TotalPrice { get; init; }
        public decimal NetPrice { get; init; }
        public decimal? SalePrice { get; init; }
        public int? RemainingRooms { get; init; }
        public int? BoardType { get; init; }
        public RateProperties? Properties { get; init; }
        public PartyInfo? SearchParty { get; init; }
    }

    /// <summary>
    /// Rate properties (board, cancellation, etc.).
    /// </summary>
    public class RateProperties
    {
        public string? Board { get; init; }
        public int? BoardId { get; init; }
        public bool HasBoard { get; init; }
        public bool HasCancellation { get; init; }
        public string? CancellationName { get; init; }
        public string? CancellationExpiry { get; init; }
        public IReadOnlyList<CancellationFee>? CancellationFees { get; init; }
        public IReadOnlyList<ScheduledPayment>? Payments { get; init; }
    }

    /// <summary>
    /// Cancellation fee info.
    /// </summary>
    public class CancellationFee
    {
        public DateTime? After { get; init; }
        public decimal? Fee { get; init; }
    }

    /// <summary>
    /// Scheduled payment info.
    /// </summary>
    public class ScheduledPayment
    {
        public DateTime? DueDate { get; init; }
        public decimal? Amount { get; init; }
    }

    /// <summary>
    /// Party (guests) info for a room.
    /// </summary>
    public class PartyInfo
    {
        public int Adults { get; init; }
        public IReadOnlyList<int>? Children { get; init; }
        public int RoomsCount { get; init; }
        public string? PartyJson { get; init; }
    }

    /// <summary>
    /// Alternative date suggestion.
    /// </summary>
    public class AlternativeDate
    {
        public DateTime CheckIn { get; init; }
        public DateTime CheckOut { get; init; }
        public int Nights { get; init; }
        public decimal MinPrice { get; init; }
        public decimal NetPrice { get; init; }
    }

    /// <summary>
    /// Room detail response.
    /// </summary>
    public class RoomDetailResponse
    {
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public RoomInfo? Room { get; init; }
    }

    /// <summary>
    /// Room information.
    /// </summary>
    public class RoomInfo
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public RoomCapacity? Capacity { get; init; }
        public IReadOnlyList<string> Photos { get; init; } = [];
        public IReadOnlyList<string> Amenities { get; init; } = [];
    }

    /// <summary>
    /// Room capacity info.
    /// </summary>
    public class RoomCapacity
    {
        public int MinPersons { get; init; }
        public int MaxPersons { get; init; }
        public int MaxAdults { get; init; }
        public int MaxInfants { get; init; }
        public bool ChildrenAllowed { get; init; }
    }
}
