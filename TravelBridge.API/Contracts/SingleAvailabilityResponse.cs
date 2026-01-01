using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityResponse : BaseWebHotelierResponse
    {
        public SingleHotelAvailabilityInfo Data { get; set; }
        public bool CouponValid { get; set; }
        public string? CouponDiscount { get; set; }

        internal bool CoversRequest(List<General.SelectedRate> partyList)
        {
            if (partyList.Sum(a => a.count) > Data.Rooms.Sum(s => s.Rates.First().RemainingRooms))
            {
                return false;
            }
            else
            {
                foreach (var party in partyList)
                {
                    int sum = 0;
                    foreach (var room in Data.Rooms)
                    {
                        foreach (var rate in room.Rates)
                        {
                            var partyItem = JsonSerializer.Deserialize<List<PartyItem>>(party.searchParty)?.FirstOrDefault() ?? throw new ArgumentException("Invalid party data format. Ensure it's valid JSON.");
                            partyItem.party = party.searchParty;

                            if (rate.SearchParty?.Equals(partyItem) == true)
                            {
                                sum += rate.RemainingRooms ?? 0;
                                break;
                            }
                        }

                    }

                    if (party.count > sum)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}