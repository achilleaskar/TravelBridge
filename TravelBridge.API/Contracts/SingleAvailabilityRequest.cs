namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityRequest:IParty
    {
        public string Party { get; set; } // [{"adults":2, "children":[2,6]},{"adults":3}]

        public string CheckIn { get; set; } // Check-in date (format: yyyy-MM-dd)

        public string CheckOut { get; set; } // Check-out date (format: yyyy-MM-dd)

        public string PropertyId { get; set; } // Check-out date (format: yyyy-MM-dd)
    }
}