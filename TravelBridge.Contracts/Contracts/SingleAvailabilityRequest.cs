namespace TravelBridge.Contracts.Contracts
{
    /// <summary>
    /// Used in: SearchPluginEndpoints.GetSearchResults(), WebHotelierPropertiesService.GetHotelAvailabilityAsync()
    /// Received by: GET /api/plugin/submitSearch endpoint (via SubmitSearchParameters)
    /// Purpose: API request model for single hotel availability queries
    /// Implements: IParty interface for party information
    /// Note: Individual properties are passed to WebHotelier service, not the whole object
    /// </summary>
    public class SingleAvailabilityRequest
    {
        public string Party { get; set; } // [{"adults":2, "children":[2,6]},{"adults":3}]

        public string CheckIn { get; set; } // Check-in date (format: yyyy-MM-dd)

        public string CheckOut { get; set; } // Check-out date (format: yyyy-MM-dd)

        public string PropertyId { get; set; } // Check-out date (format: yyyy-MM-dd)
    }
}
