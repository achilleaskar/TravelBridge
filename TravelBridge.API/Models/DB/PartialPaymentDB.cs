using TravelBridge.API.Contracts;

namespace TravelBridge.API.Models.DB
{
    public class PartialPaymentDB : BaseModel
    {
        public PartialPaymentDB()
        {

        }
        public PartialPaymentDB(PartialPayment partialPayment)
        {
            nextPayments = partialPayment.nextPayments.Select(p =>
            new NextPaymentDB
            {
                Amount = p.Amount,
                DueDate = p.DueDate
            }).ToList();
            prepayAmount = partialPayment.prepayAmount;
        }

        public List<NextPaymentDB> nextPayments { get; set; }
        public decimal prepayAmount { get; set; }
    }
}
