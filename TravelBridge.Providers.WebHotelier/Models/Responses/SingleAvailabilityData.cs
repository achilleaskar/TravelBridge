using TravelBridge.Contracts.Contracts;

namespace TravelBridge.Providers.WebHotelier.Models.Responses
{
    public class SingleAvailabilityData : BaseWebHotelierResponse
    {
        [JsonPropertyName("data")]
        public HotelInfo Data { get; set; }

        public List<Alternative> Alternatives { get;  set; }

        public bool CoversRequest(List<PartyItem>? partyList)
        {
            if (partyList.Sum(a => a.RoomsCount) > Data.Rates.DistinctBy(h => h.Type).Sum(s => s.RemainingRooms))
            {
                return false;
            }
            else
            {
                foreach (var party in partyList)
                {
                    if (party.RoomsCount <= (
                        Data.Rates
                        .Where(r => r.SearchParty?.Equals(party) == true)
                        .GroupBy(r => r.Type)
                        .Select(g => g.First())
                        .Sum(s => s.RemainingRooms) ?? 0
                        )
                    )
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }
    }
}
