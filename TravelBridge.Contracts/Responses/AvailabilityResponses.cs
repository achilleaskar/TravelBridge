namespace TravelBridge.Contracts.Responses
{
    /// <summary>
    /// Standard API response wrapper.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    public class ApiResponse<T>
    {
        /// <summary>
        /// Whether the request was successful.
        /// </summary>
        public bool Success { get; init; }

        /// <summary>
        /// Response data (null if error).
        /// </summary>
        public T? Data { get; init; }

        /// <summary>
        /// Error code if failed.
        /// </summary>
        public string? ErrorCode { get; init; }

        /// <summary>
        /// Error message if failed.
        /// </summary>
        public string? ErrorMessage { get; init; }

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        public static ApiResponse<T> Ok(T data) => new()
        {
            Success = true,
            Data = data
        };

        /// <summary>
        /// Creates an error response.
        /// </summary>
        public static ApiResponse<T> Error(string errorCode, string message) => new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = message
        };
    }

    /// <summary>
    /// Hotel availability response.
    /// </summary>
    public class HotelAvailabilityResponse
    {
        /// <summary>
        /// Hotel identifier.
        /// </summary>
        public required string HotelId { get; init; }

        /// <summary>
        /// Hotel name.
        /// </summary>
        public string? HotelName { get; init; }

        /// <summary>
        /// Available rooms.
        /// </summary>
        public IReadOnlyList<RoomAvailabilityResponse>? Rooms { get; init; }

        /// <summary>
        /// Alternative dates if no availability.
        /// </summary>
        public IReadOnlyList<AlternativeDateResponse>? Alternatives { get; init; }

        /// <summary>
        /// Whether coupon was applied.
        /// </summary>
        public bool CouponApplied { get; init; }

        /// <summary>
        /// Coupon discount description (e.g., "-10%").
        /// </summary>
        public string? CouponDiscount { get; init; }
    }

    /// <summary>
    /// Room availability information.
    /// </summary>
    public class RoomAvailabilityResponse
    {
        /// <summary>
        /// Room type code.
        /// </summary>
        public required string RoomType { get; init; }

        /// <summary>
        /// Room name.
        /// </summary>
        public required string RoomName { get; init; }

        /// <summary>
        /// Available rates for this room.
        /// </summary>
        public IReadOnlyList<RateResponse>? Rates { get; init; }
    }

    /// <summary>
    /// Rate information.
    /// </summary>
    public class RateResponse
    {
        /// <summary>
        /// Rate ID.
        /// </summary>
        public required string RateId { get; init; }

        /// <summary>
        /// Rate name/description.
        /// </summary>
        public string? RateName { get; init; }

        /// <summary>
        /// Total price for the stay.
        /// </summary>
        public decimal TotalPrice { get; init; }

        /// <summary>
        /// Original/sale price (if discounted).
        /// </summary>
        public decimal? SalePrice { get; init; }

        /// <summary>
        /// Board type (e.g., "Breakfast included").
        /// </summary>
        public string? BoardType { get; init; }

        /// <summary>
        /// Cancellation policy description.
        /// </summary>
        public string? CancellationPolicy { get; init; }

        /// <summary>
        /// Number of remaining rooms.
        /// </summary>
        public int? RemainingRooms { get; init; }
    }

    /// <summary>
    /// Alternative date suggestion.
    /// </summary>
    public class AlternativeDateResponse
    {
        /// <summary>
        /// Alternative check-in date.
        /// </summary>
        public required DateOnly CheckIn { get; init; }

        /// <summary>
        /// Alternative check-out date.
        /// </summary>
        public required DateOnly CheckOut { get; init; }

        /// <summary>
        /// Number of nights.
        /// </summary>
        public int Nights { get; init; }

        /// <summary>
        /// Minimum price for this period.
        /// </summary>
        public decimal MinPrice { get; init; }
    }

    /// <summary>
    /// Booking confirmation response.
    /// </summary>
    public class BookingConfirmationResponse
    {
        /// <summary>
        /// Internal reservation ID.
        /// </summary>
        public required int ReservationId { get; init; }

        /// <summary>
        /// Provider confirmation codes.
        /// </summary>
        public IReadOnlyList<string>? ConfirmationCodes { get; init; }

        /// <summary>
        /// Booking status.
        /// </summary>
        public required string Status { get; init; }

        /// <summary>
        /// Total amount charged.
        /// </summary>
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Amount paid so far.
        /// </summary>
        public decimal PaidAmount { get; init; }

        /// <summary>
        /// Remaining amount to pay.
        /// </summary>
        public decimal RemainingAmount { get; init; }
    }
}
