using TravelBridge.Contracts.Plugin.Filters;

namespace TravelBridge.API.Contracts
{
    public class PluginSearchResponse
    {
        public string SearchTerm { get; set; }
        public IEnumerable<WebHotel> Results { get; set; }
        public int ResultsCount { get; set; }
        public List<Filter> Filters { get; set; }

        internal IEnumerable<WebHotel> CoverRequest(List<PartyItem> partyList)
        {
            List<WebHotel> invalid = [];
            foreach (var hotel in Results)
            {
                if (partyList.Sum(a => a.RoomsCount) > hotel.Rates.DistinctBy(h => h.Type).Sum(s => s.Remaining))
                {
                    invalid.Add(hotel);
                }
                else
                {
                    foreach (var party in partyList)
                    {
                        if (party.RoomsCount > (
                            hotel.Rates
                                .Where(r => r.SearchParty?.Equals(party) == true)
                                .GroupBy(r => r.Type) // or r.RateType if it's a property
                                .Select(g => g.First()) // take one rate per type
                                .Sum(s => s.Remaining)
                            )
                        )
                        {
                            invalid.Add(hotel);
                            break;
                        }
                    }
                }
            }

            return Results.Except(invalid);
        }
    }
}