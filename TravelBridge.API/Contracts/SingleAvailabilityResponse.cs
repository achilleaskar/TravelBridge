using TravelBridge.API.Contracts.DTOs;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityResponse : BaseWebHotelierResponse
    {
        public SingleHotelAvailabilityInfo Data { get; set; }
        public bool CouponValid { get; set; }
        public string? CouponDiscount { get; set; }
    }
}