using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.API.Contracts.DTOs
{
    public class SingleHotelAvailabilityInfo : BaseHotelInfo
    {
        public Location Location { get; set; }

        public List<SingleHotelRoom> Rooms { get; set; }
        public List<Alternative> Alternatives { get; set; }
    }
}
