
namespace TravelBridge.API.Contracts
{
    public class HotelInfoFullResponse
    {

        public string ErrorCode { get; set; }

        public string ErrorMsg { get; set; }

        
        public HotelData HotelData { get; set; }

        public IEnumerable<SingleHotelRoom> Rooms { get; set; }
        public List<Alternative> Alternatives { get; set; }
    }
}