using TravelBridge.Contracts.Models.Hotels;


namespace TravelBridge.Providers.WebHotelier.Models.Hotel
{
    public class HotelInfo : BaseHotelInfo
    {
        #region Properties

        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("rates")]
        public List<HotelRate> Rates { get; set; }

        #endregion Properties
    }
}
