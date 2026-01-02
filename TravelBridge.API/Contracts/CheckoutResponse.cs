using System.Globalization;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers;

namespace TravelBridge.API.Contracts
{
    public class CheckoutResponse : BaseWebHotelierResponse
    {
        [JsonPropertyName("hotelData")]
        public CheckoutHotelInfo HotelData { get; set; }

        [JsonPropertyName("checkIn")]
        public string CheckIn { get; set; }

        [JsonPropertyName("checkOut")]
        public string CheckOut { get; set; }

        [JsonPropertyName("nights")]
        public int Nights { get; set; }

        [JsonPropertyName("selectedPeople")]
        public string SelectedPeople { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("rooms")]
        public List<CheckoutRoomInfo> Rooms { get; set; }

        [JsonPropertyName("errorMessage")]
        public string LabelErrorMessage { get; set; }
        
        public List<PaymentWH> Payments { get; set; }
        public PartialPayment? PartialPayment { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }
        public string? CouponUsed { get; set; }
        public string? CouponDiscount { get; set; }
        public bool CouponValid { get; set; }
    }
}
