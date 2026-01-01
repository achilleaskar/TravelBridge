namespace TravelBridge.Contracts.Responses
{
    /// <summary>
    /// Checkout page response.
    /// </summary>
    public class CheckoutResponse
    {
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? LabelErrorMessage { get; init; }
        public CheckoutHotelInfo? HotelData { get; init; }
        public string? CheckIn { get; init; }
        public string? CheckOut { get; init; }
        public string? CheckInTime { get; init; }
        public string? CheckOutTime { get; init; }
        public int Nights { get; init; }
        public IReadOnlyList<CheckoutRoomInfo> Rooms { get; init; } = [];
        public decimal TotalPrice { get; init; }
        public string? SelectedPeople { get; init; }
        public PartialPaymentInfo? PartialPayment { get; init; }
        public string? CouponUsed { get; init; }
        public bool CouponValid { get; init; }
        public string? CouponDiscount { get; init; }
    }

    /// <summary>
    /// Hotel info for checkout.
    /// </summary>
    public class CheckoutHotelInfo
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? Image { get; init; }
        public int? Rating { get; init; }
        public HotelOperation? Operation { get; init; }
    }

    /// <summary>
    /// Room info for checkout.
    /// </summary>
    public class CheckoutRoomInfo
    {
        public required string RateId { get; init; }
        public string? RoomType { get; init; }
        public string? RoomName { get; init; }
        public decimal TotalPrice { get; init; }
        public decimal NetPrice { get; init; }
        public int SelectedQuantity { get; init; }
        public CheckoutRateProperties? RateProperties { get; init; }
    }

    /// <summary>
    /// Rate properties for checkout.
    /// </summary>
    public class CheckoutRateProperties
    {
        public string? Board { get; init; }
        public int? BoardId { get; init; }
        public bool HasBoard { get; init; }
        public bool HasCancellation { get; init; }
        public string? CancellationName { get; init; }
        public string? CancellationExpiry { get; init; }
        public IReadOnlyList<CancellationFee>? CancellationFees { get; init; }
        public IReadOnlyList<ScheduledPayment>? Payments { get; init; }
        public PartyInfo? SearchParty { get; init; }
    }

    /// <summary>
    /// Partial payment information.
    /// </summary>
    public class PartialPaymentInfo
    {
        public decimal PrepayAmount { get; init; }
        public IReadOnlyList<NextPayment> NextPayments { get; init; } = [];
    }

    /// <summary>
    /// Next payment info.
    /// </summary>
    public class NextPayment
    {
        public DateTime? DueDate { get; init; }
        public decimal? Amount { get; init; }
    }

    /// <summary>
    /// Prepare payment response.
    /// </summary>
    public class PreparePaymentResponse
    {
        public string? OrderCode { get; init; }
        public string? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Successful payment response.
    /// </summary>
    public class PaymentSuccessResponse
    {
        public bool Success { get; init; }
        public string? Error { get; init; }
        public string? ErrorCode { get; init; }
        public PaymentSuccessData? Data { get; init; }
    }

    /// <summary>
    /// Payment success data.
    /// </summary>
    public class PaymentSuccessData
    {
        public int ReservationId { get; init; }
        public string? HotelName { get; init; }
        public string? CheckIn { get; init; }
        public string? CheckOut { get; init; }
    }
}
