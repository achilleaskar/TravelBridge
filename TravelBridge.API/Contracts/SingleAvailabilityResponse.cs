using System.Text.Json.Serialization;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityResponse : BaseWebHotelierResponse
    {
        public SingleHotelAvailabilityInfo Data { get; set; }
    }

    public class SingleHotelAvailabilityInfo : BaseHotelInfo
    {
        public Location Location { get; set; }

        public List<SingleHotelRoom> Rooms { get; set; }
    }

    public class SingleHotelRoom
    {
        public string Type { get; set; }

        public string RoomName { get; set; }

        public List<SingleHotelRate> Rates { get; set; }

        public int RatesCount { get; set; }
    }

    public class SingleHotelRate : BaseBoard
    {
        public int Id { get; set; }

        [JsonIgnore]
        public PricingInfo Pricing { get; set; } // to be deleted

        [JsonIgnore]
        public PricingInfo Retail { get; set; } // to be deleted

        public int SelectedQuantity { get; set; }

        //public string Status { get; set; }

        //public string StatusDescription { get; set; }

        //public IEnumerable<RateLabel> Labels { get; set; }
        public int? RemainingRooms { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public RateProperties RateProperties { get; set; }
        public string RateDescription { get; set; }
    }

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
        public PartialPayment PartialPayment { get; set; }
    }

    public class StringAmount
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class PartialPayment
    {
        public List<PaymentWH> nextPayments { get; set; }
        public decimal prepayAmount { get; set; }
    }
}