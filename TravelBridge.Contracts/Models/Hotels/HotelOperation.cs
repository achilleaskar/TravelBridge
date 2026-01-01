namespace TravelBridge.Contracts.Models.Hotels
{
    public class HotelOperation
    {
        [JsonPropertyName("checkout_time")]
        public string CheckoutTime { get; set; }

        [JsonPropertyName("checkin_time")]
        public string CheckinTime { get; set; }
    }
}
