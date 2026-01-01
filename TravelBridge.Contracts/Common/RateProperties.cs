using TravelBridge.Contracts.Common.Policies;
using TravelBridge.Contracts.Common.Payments;

namespace TravelBridge.Contracts.Common
{
    public class RateProperties
    {
        public string Board { get; set; }
        public string RateName { get; set; }
        public bool HasCancellation { get; set; }
        public string CancellationName { get; set; }
        public string CancellationPenalty { get; set; }
        public string? CancellationExpiry { get; set; }
        public bool HasBoard { get; set; }
        public string CancellationPolicy { get; set; }
        public List<StringAmount> CancellationFees { get; set; }
        [JsonIgnore]
        public List<PaymentWH> Payments { get; set; }
        [JsonIgnore]
        public IEnumerable<CancellationFee> CancellationFeesOr { get; set; }
        [JsonIgnore]
        public List<PaymentWH> PaymentsOr { get; set; }
        public PartialPayment? PartialPayment { get; set; }
    }
}
