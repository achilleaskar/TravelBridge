namespace TravelBridge.API.Contracts.DTOs
{
    public class CheckoutRoomInfo
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("roomName")]
        public string RoomName { get; set; }

        [JsonPropertyName("rateId")]
        public string RateId { get; set; }

        [JsonPropertyName("selectedQuantity")]
        public int SelectedQuantity { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("rateProperties")]
        public CheckoutRateProperties RateProperties { get; set; }
        
        [JsonIgnore]
        public decimal NetPrice { get; set; }
    }
}
