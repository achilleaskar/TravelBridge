namespace TravelBridge.Contracts.Common.Payments
{
    public class PaymentWH
    {
        [JsonPropertyName("due")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        public override string ToString()
        {
            return $"DueDate: {DueDate?.ToString("yyyy-MM-dd")}, " +
                   $"Amount: {Amount?.ToString("C", System.Globalization.CultureInfo.CurrentCulture)}";
        }
    }
}
