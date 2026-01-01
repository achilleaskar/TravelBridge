namespace TravelBridge.API.Models.DB
{
    public class NextPaymentDB : BaseModel
    {
        public DateTime? DueDate { get; set; }

        public decimal? Amount { get; set; }
    }
}
