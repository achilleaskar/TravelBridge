namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Partial payment entity - stores partial payment schedule.
    /// </summary>
    public class PartialPaymentEntity : BaseEntity
    {
        public decimal PrepayAmount { get; set; }

        // Navigation properties
        public ICollection<NextPaymentEntity> NextPayments { get; set; } = [];
    }

    /// <summary>
    /// Next payment entity - stores scheduled future payment.
    /// </summary>
    public class NextPaymentEntity : BaseEntity
    {
        public DateTime? DueDate { get; set; }
        public decimal? Amount { get; set; }

        // Foreign key
        public int PartialPaymentId { get; set; }

        // Navigation
        public PartialPaymentEntity? PartialPayment { get; set; }
    }
}
