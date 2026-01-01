using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.API.Contracts.DTOs
{
    public class CheckoutHotelInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("image")]
        public string Image { get; set; }

        [JsonPropertyName("operation")]
        public HotelOperation Operation { get; set; }
    }
}
