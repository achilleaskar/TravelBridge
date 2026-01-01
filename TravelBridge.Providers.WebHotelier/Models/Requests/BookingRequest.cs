namespace TravelBridge.Providers.WebHotelier.Models.Requests
{
    public class BookingRequest
    {
        [JsonPropertyName("search_id")]
        public string SearchId { get; set; }

        [JsonPropertyName("room_type_code")]
        public string RoomTypeCode { get; set; }

        [JsonPropertyName("rate_plan_code")]
        public string RatePlanCode { get; set; }

        [JsonPropertyName("arrival")]
        public string ArrivalDate { get; set; }

        [JsonPropertyName("departure")]
        public string DepartureDate { get; set; }

        [JsonPropertyName("adults")]
        public int Adults { get; set; }

        [JsonPropertyName("children")]
        public int Children { get; set; }

        [JsonPropertyName("client")]
        public ClientInfo Client { get; set; }

        [JsonPropertyName("payments")]
        public PaymentInfo Payments { get; set; }
    }

    public class ClientInfo
    {
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }
    }

    public class PaymentInfo
    {
        [JsonPropertyName("method")]
        public string Method { get; set; }
    }
}
