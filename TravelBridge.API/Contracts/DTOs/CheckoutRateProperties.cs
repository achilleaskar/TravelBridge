using TravelBridge.Contracts.Common.Payments;

namespace TravelBridge.API.Contracts.DTOs
{
    public class CheckoutRateProperties
    {
        [JsonPropertyName("board")]
        public string Board { get; set; }

        [JsonIgnore]
        public int? BoardId { get; set; }

        [JsonPropertyName("hasCancellation")]
        public bool HasCancellation { get; set; }

        [JsonPropertyName("cancellationName")]
        public string CancellationName { get; set; }

        [JsonPropertyName("cancellationExpiry")]
        public string? CancellationExpiry { get; set; }

        [JsonPropertyName("hasBoard")]
        public bool HasBoard { get; set; }
        public List<StringAmount> CancellationFees { get; set; }
        public List<PaymentWH> Payments { get; set; }
        public PartyItem SearchParty { get; set; }
    }
}
