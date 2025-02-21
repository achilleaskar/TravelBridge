namespace TravelBridge.API.Models.WebHotelier
{
    public class PropertiesResponse
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public Hotel[] hotels { get; set; }
    }

    public class Hotel
    {
        public string code { get; set; }
        public string name { get; set; }
        public string type { get; set; }
        public Location location { get; set; }
    }

    public class Location
    {
        public string name { get; set; }
        public string country { get; set; }
    }
}