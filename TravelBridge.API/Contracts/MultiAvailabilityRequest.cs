namespace TravelBridge.API.Contracts
{
    public class MultiAvailabilityRequest
    {
        public string Party { get; set; } // [{"adults":2, "children":[2,6]},{"adults":3}]

        public string CheckIn { get; set; } // Check-in date (format: yyyy-MM-dd)

        public string CheckOut { get; set; } // Check-out date (format: yyyy-MM-dd)

        public string BottomLeftLatitude { get; set; } // Bottom left latitude

        public string TopRightLatitude { get; set; } // Top right latitude

        public string BottomLeftLongitude { get; set; } // Bottom left longitude

        public string TopRightLongitude { get; set; } // Top right longitude

        public string Lat { get; set; } // center latitude

        public string Lon { get; set; } // center longitude

        public string SortBy { get; set; } // center latitude

        public string SortOrder { get; set; }// center longitude
    }
}
