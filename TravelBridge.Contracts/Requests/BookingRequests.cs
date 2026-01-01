namespace TravelBridge.Contracts.Requests
{
    /// <summary>
    /// Prepare payment / create reservation request (from frontend).
    /// </summary>
    public class PreparePaymentRequest
    {
        public required string HotelId { get; init; }
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public int? Rooms { get; init; }
        public string? Children { get; init; }
        public string? CouponCode { get; init; }
        public int? Adults { get; init; }
        public string? Party { get; init; }
        public required string SelectedRates { get; init; }
        public required decimal TotalPrice { get; init; }
        public decimal? PrepayAmount { get; init; }
        public CustomerInfo? CustomerInfo { get; init; }
    }

    /// <summary>
    /// Customer information.
    /// </summary>
    public class CustomerInfo
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Requests { get; init; }
    }

    /// <summary>
    /// Payment callback info.
    /// </summary>
    public class PaymentCallbackRequest
    {
        public string? TransactionId { get; init; }
        public required string OrderCode { get; init; }
    }

    /// <summary>
    /// Apply coupon request.
    /// </summary>
    public class ApplyCouponRequest
    {
        public string? CouponCode { get; init; }
        public required ReservationDetails ReservationDetails { get; init; }
        public required CustomerFormData FormData { get; init; }
    }

    /// <summary>
    /// Reservation details for coupon.
    /// </summary>
    public class ReservationDetails
    {
        public required string HotelId { get; init; }
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public string? Children { get; init; }
        public string? Adults { get; init; }
        public string? Party { get; init; }
        public required string SelectedRates { get; init; }
        public decimal TotalPrice { get; init; }
    }

    /// <summary>
    /// Customer form data.
    /// </summary>
    public class CustomerFormData
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public string? Requests { get; init; }
    }
}
