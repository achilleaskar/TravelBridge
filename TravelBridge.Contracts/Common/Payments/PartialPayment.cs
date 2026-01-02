namespace TravelBridge.Contracts.Common.Payments
{
    public class PartialPayment
    {
        public List<PaymentWH> nextPayments { get; set; }
        public decimal prepayAmount { get; set; }
    }
}
