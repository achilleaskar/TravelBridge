using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Payment entity - stores payment transaction information.
    /// </summary>
    public class PaymentEntity : BaseEntity
    {
        public DateTime DateFinalized { get; set; }
        public PaymentProvider PaymentProvider { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public decimal Amount { get; set; }
        public string? TransactionId { get; set; }
        public string? OrderCode { get; set; }

        // Foreign keys
        public int? CustomerId { get; set; }
        public int? ReservationId { get; set; }

        // Navigation properties
        public CustomerEntity? Customer { get; set; }
        public ReservationEntity? Reservation { get; set; }
    }
}
