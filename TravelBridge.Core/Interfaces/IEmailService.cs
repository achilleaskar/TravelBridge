namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Interface for email sending services.
    /// Implementation in Infrastructure layer.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlBody">HTML body content.</param>
        /// <param name="cc">CC recipients (optional).</param>
        /// <param name="bcc">BCC recipients (optional).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendEmailAsync(
            string to,
            string subject,
            string htmlBody,
            IEnumerable<string>? cc = null,
            IEnumerable<string>? bcc = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a booking confirmation email.
        /// </summary>
        /// <param name="notification">Booking notification details.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendBookingConfirmationAsync(
            BookingNotification notification,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends an error notification to admins.
        /// </summary>
        /// <param name="subject">Error subject.</param>
        /// <param name="errorDetails">Error details HTML.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SendErrorNotificationAsync(
            string subject,
            string errorDetails,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Booking notification details for email.
    /// </summary>
    public class BookingNotification
    {
        /// <summary>
        /// Customer full name.
        /// </summary>
        public required string CustomerName { get; init; }

        /// <summary>
        /// Customer email.
        /// </summary>
        public required string CustomerEmail { get; init; }

        /// <summary>
        /// Customer phone.
        /// </summary>
        public string? CustomerPhone { get; init; }

        /// <summary>
        /// Hotel name.
        /// </summary>
        public required string HotelName { get; init; }

        /// <summary>
        /// Reservation confirmation codes.
        /// </summary>
        public IEnumerable<string>? ConfirmationCodes { get; init; }

        /// <summary>
        /// Check-in date.
        /// </summary>
        public required DateOnly CheckIn { get; init; }

        /// <summary>
        /// Check-out date.
        /// </summary>
        public required DateOnly CheckOut { get; init; }

        /// <summary>
        /// Check-in time.
        /// </summary>
        public string? CheckInTime { get; init; }

        /// <summary>
        /// Check-out time.
        /// </summary>
        public string? CheckOutTime { get; init; }

        /// <summary>
        /// Number of nights.
        /// </summary>
        public int Nights { get; init; }

        /// <summary>
        /// Total amount.
        /// </summary>
        public decimal TotalAmount { get; init; }

        /// <summary>
        /// Amount paid.
        /// </summary>
        public decimal PaidAmount { get; init; }

        /// <summary>
        /// Remaining amount.
        /// </summary>
        public decimal RemainingAmount { get; init; }

        /// <summary>
        /// Party description.
        /// </summary>
        public string? PartyDescription { get; init; }

        /// <summary>
        /// Customer notes/requests.
        /// </summary>
        public string? CustomerNotes { get; init; }

        /// <summary>
        /// Room details for the email.
        /// </summary>
        public IEnumerable<RoomNotificationDetail>? Rooms { get; init; }
    }

    /// <summary>
    /// Room detail for notification.
    /// </summary>
    public class RoomNotificationDetail
    {
        /// <summary>
        /// Room name.
        /// </summary>
        public required string RoomName { get; init; }

        /// <summary>
        /// Quantity booked.
        /// </summary>
        public int Quantity { get; init; }

        /// <summary>
        /// Board type description.
        /// </summary>
        public string? BoardType { get; init; }

        /// <summary>
        /// Cancellation policy.
        /// </summary>
        public string? CancellationPolicy { get; init; }

        /// <summary>
        /// Price for this room.
        /// </summary>
        public decimal Price { get; init; }

        /// <summary>
        /// Party description for this room.
        /// </summary>
        public string? PartyDescription { get; init; }
    }
}
