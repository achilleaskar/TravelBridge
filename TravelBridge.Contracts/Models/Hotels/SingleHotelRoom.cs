namespace TravelBridge.Contracts.Models.Hotels
{
    public class SingleHotelRoom
    {
        public string Type { get; set; }

        public string RoomName { get; set; }

        public List<SingleHotelRate> Rates { get; set; }

        public int RatesCount { get; set; }
    }
}
