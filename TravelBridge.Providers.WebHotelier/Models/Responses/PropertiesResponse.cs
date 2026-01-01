namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class PropertiesResponse
    {
        public PropertiesData data { get; set; }
    }

    public class PropertiesData
    {
        public Hotel[] hotels { get; set; }
    }

    public class Hotel
    {
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public HotelLocation location { get; set; }
    }

    public class HotelLocation
    {
        public string name { get; set; }
        public string country { get; set; }
    }
}
