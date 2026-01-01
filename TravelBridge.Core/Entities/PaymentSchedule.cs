namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Represents a scheduled payment - used for partial payments.
    /// Provider-agnostic model.
    /// </summary>
    public class ScheduledPayment
    {
        public DateTime? DueDate { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Represents a partial payment schedule with prepayment and future payments.
    /// Provider-agnostic model.
    /// </summary>
    public class PaymentSchedule
    {
        public decimal PrepayAmount { get; set; }
        public List<ScheduledPayment> NextPayments { get; set; } = [];

        /// <summary>
        /// Total amount including prepay and all scheduled payments.
        /// </summary>
        public decimal TotalAmount => PrepayAmount + NextPayments.Sum(p => p.Amount);

        /// <summary>
        /// Creates a full prepayment schedule (no future payments).
        /// </summary>
        public static PaymentSchedule FullPrepayment(decimal totalAmount)
        {
            return new PaymentSchedule
            {
                PrepayAmount = totalAmount,
                NextPayments = []
            };
        }
    }
}
