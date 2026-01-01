using TravelBridge.Contracts.Common.Payments;

namespace TravelBridge.Contracts.Models.Hotels
{
    public class SingleHotelRate : BaseBoard
    {
        public string Id { get; set; }

        [JsonIgnore]
        public PricingInfo Pricing { get; set; }

        [JsonIgnore]
        public PricingInfo Retail { get; set; }

        public int SelectedQuantity { get; set; }

        public int? RemainingRooms { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal SalePrice { get; set; }
        public RateProperties RateProperties { get; set; }
        public string RateDescription { get; set; }
        public PartyItem SearchParty { get;  set; }

        [JsonIgnore]
        public decimal NetPrice { get; set; }
    }
}
