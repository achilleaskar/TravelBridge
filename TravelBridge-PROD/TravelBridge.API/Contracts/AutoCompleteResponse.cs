using TravelBridge.API.Models.Plugin.AutoComplete;

namespace TravelBridge.API.Contracts
{
    public class AutoCompleteResponse
    {
        public IEnumerable<AutoCompleteHotel> Hotels { get; set; }
        public IEnumerable<AutoCompleteLocation> Locations { get; set; }
    }
}
