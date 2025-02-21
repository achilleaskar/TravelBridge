using System.Text.Json.Serialization;
using TravelBridge.API.Models;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityResponse
    {
        public int HttpCode { get; set; }

        public string ErrorCode { get; set; }

        public string ErrorMessage { get; set; }

        public SingleHotelAvailabilityInfo Data { get; set; }
    }

    public class SingleHotelAvailabilityInfo
    {
        public string Code { internal get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

        public string Id => $"{(int)Provider}-{Code}";

        public string Name { get; set; }

        public Location Location { get; set; }

        public IEnumerable<SingleHotelRoom> Rooms { get; set; }
    }

    public class SingleHotelRoom
    {
        public string Type { get; set; }

        public string RoomName { get; set; }

        public List<SingleHotelRate> Rates { get; set; }

        public int RatesCount { get;  set; }
    }

    public class SingleHotelRate
    {
        public int Id { get; set; }

        public string RateName { get; set; }

        public string RateDescription { get; set; }

        public string PaymentPolicy { get; set; }

        public int PaymentPolicyId { get; set; }

        public string CancellationPolicy { get; set; }

        public int CancellationPolicyId { get; set; }

        public string CancellationPenalty { get; set; }

        public DateTime? CancellationExpiry { get; set; }

        public int? BoardType { get; set; } 

        public PricingInfo Pricing { get; set; }

        public PricingInfo Retail { get; set; }

        public string Status { get; set; }

        public string StatusDescription { get; set; }

        public List<RateLabel> Labels { get; set; }
    }
}