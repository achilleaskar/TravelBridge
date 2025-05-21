using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class BookingResponse: BaseWebHotelierResponse
    {
        public BookingData data { get; set; }

        public class BookingData
        {
            public string summaryUrl { get; set; }
            public int res_id { get; set; }
            public string email { get; set; }
            public string result { get; set; }
        }
    }
}
