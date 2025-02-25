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

        public IEnumerable<SingleHotelRoom> Rooms { get; set; }
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
        public PricingInfo Pricing { get; set; } // to be deleted

        public PricingInfo Retail { get; set; } // to be deleted

        public string Status { get; set; }

        public string StatusDescription { get; set; }

        public IEnumerable<RateLabel> Labels { get; set; }
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
        public DateTime? CancellationExpiry { get; set; }
        public bool HasBoard { get; set; }
        public string CancellationPolicy { get; set; }
        public List<StringAmount> CancellationFees { get; set; }
    }

    public class StringAmount
    {
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }
}