using System.Globalization;
using System.Text.Json.Serialization;
using TravelBridge.API.Helpers;
using TravelBridge.API.Models.WebHotelier;

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
        public PartialPayment PartialPayment { get; private set; }



        internal void MergePayments(List<General.SelectedRate> selectedrates)
        {
            if (!DateTime.TryParseExact(CheckIn, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime checkinDate))
            {
                // Handle parse failure here
            }

            TotalPrice = Rooms.Sum(r => r.TotalPrice);
            Payments = Rooms.SelectMany(r => r.RateProperties.Payments).ToList();
            PartialPayment = General.FillPartialPayment(Payments, checkinDate) ?? throw new InvalidOperationException("Payments calculation failure.");
            Payments = [];
            if ((PartialPayment.prepayAmount + PartialPayment.nextPayments.Sum(a => a.Amount)) != TotalPrice)
            {
                throw new InvalidOperationException("Payments calculation failure.");
            }
        }
    }

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
    }

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
