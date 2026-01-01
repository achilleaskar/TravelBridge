using System.Text.Json.Serialization;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts
{
    /// <summary>
    /// WebHotelier single availability response.
    /// </summary>
    public class WHSingleAvailabilityResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHAvailabilityData? Data { get; set; }

        public List<WHAlternative>? Alternatives { get; set; }
    }

    public class WHAvailabilityData : WHBaseHotelInfo
    {
        [JsonPropertyName("rates")]
        public List<WHAvailabilityRate> Rates { get; set; } = [];
    }

    public class WHAvailabilityRate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("room")]
        public string? RoomType { get; set; }

        [JsonPropertyName("room_name")]
        public string? RoomName { get; set; }

        [JsonPropertyName("board")]
        public WHBoard? Board { get; set; }

        [JsonPropertyName("remaining")]
        public int? RemainingRooms { get; set; }

        [JsonPropertyName("pricing")]
        public WHPricingInfo? Pricing { get; set; }

        [JsonPropertyName("retail")]
        public WHPricingInfo? Retail { get; set; }

        [JsonPropertyName("cancellation")]
        public WHCancellation? Cancellation { get; set; }

        [JsonPropertyName("payments")]
        public List<WHPayment>? Payments { get; set; }

        public WHPartyItem? SearchParty { get; set; }
    }

    public class WHCancellation
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("penalty")]
        public string? Penalty { get; set; }

        [JsonPropertyName("policy")]
        public string? Policy { get; set; }

        [JsonPropertyName("expiry")]
        public string? Expiry { get; set; }

        [JsonPropertyName("fees")]
        public List<WHCancellationFee>? Fees { get; set; }
    }

    /// <summary>
    /// Alternative date with availability.
    /// </summary>
    public class WHAlternative
    {
        public DateTime CheckIn { get; set; }
        public DateTime Checkout { get; set; }
        public int Nights { get; set; }
        public decimal MinPrice { get; set; }
        public decimal NetPrice { get; set; }
    }

    /// <summary>
    /// Alternative days calendar response.
    /// </summary>
    public class WHAlternativeDaysResponse
    {
        [JsonPropertyName("data")]
        public WHAlternativeDaysData? Data { get; set; }
    }

    public class WHAlternativeDaysData
    {
        [JsonPropertyName("days")]
        public List<WHDayInfo>? Days { get; set; }
    }

    public class WHDayInfo
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("retail")]
        public decimal Retail { get; set; }

        [JsonPropertyName("min_stay")]
        public int MinStay { get; set; }

        [JsonIgnore]
        public DateTime DateOnly { get; set; }
    }
}
