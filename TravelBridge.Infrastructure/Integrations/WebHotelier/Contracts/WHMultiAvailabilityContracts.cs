using System.Text.Json.Serialization;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts
{
    /// <summary>
    /// WebHotelier multi-hotel availability response.
    /// </summary>
    public class WHMultiAvailabilityResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHMultiAvailabilityData? Data { get; set; }
    }

    public class WHMultiAvailabilityData
    {
        [JsonPropertyName("hotels")]
        public List<WHHotelAvailability>? Hotels { get; set; }
    }

    public class WHHotelAvailability : WHBaseHotelInfo
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("photo_l")]
        public string? PhotoL { get; set; }

        [JsonPropertyName("location")]
        public WHLocation? Location { get; set; }

        [JsonPropertyName("rates")]
        public List<WHHotelRate> Rates { get; set; } = [];

        public WHPartyItem? SearchParty { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MinPricePerDay { get; set; }
        public decimal? SalePrice { get; set; }
    }

    public class WHHotelRate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("room")]
        public string? RoomType { get; set; }

        [JsonPropertyName("board")]
        public WHBoard? Board { get; set; }

        [JsonPropertyName("remaining")]
        public int? RemainingRooms { get; set; }

        [JsonPropertyName("pricing")]
        public WHPricingInfo? Pricing { get; set; }

        [JsonPropertyName("retail")]
        public WHPricingInfo? Retail { get; set; }

        [JsonPropertyName("payments")]
        public List<WHPayment>? Payments { get; set; }

        public WHPartyItem? SearchParty { get; set; }
    }
}
