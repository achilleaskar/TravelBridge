using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB
{
    public class PartialPaymentDB : BaseModel
    {
        public PartialPaymentDB()
        {

        }
        public PartialPaymentDB(PartialPayment partialPayment, decimal totalPrice)
        {
            nextPayments = partialPayment?.nextPayments.Select(p =>
            new NextPaymentDB
            {
                Amount = p.Amount,
                DueDate = p.DueDate
            }).ToList() ?? new List<NextPaymentDB>();
            prepayAmount = partialPayment?.prepayAmount ?? totalPrice;
        }

        public List<NextPaymentDB> nextPayments { get; set; }
        
        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal prepayAmount { get; set; }
    }
}
