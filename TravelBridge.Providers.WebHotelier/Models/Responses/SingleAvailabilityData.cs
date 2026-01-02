namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire response for single hotel availability.
/// </summary>
public class WHSingleAvailabilityData : WHBaseResponse
{
    [JsonPropertyName("data")]
    public WHHotelInfo? Data { get; set; }

    public List<WHAlternative> Alternatives { get; set; } = [];

    public bool CoversRequest(List<WHPartyItem>? partyList)
    {
        if (partyList == null || Data?.Rates == null)
            return false;

        if (partyList.Sum(a => a.RoomsCount) > Data.Rates.DistinctBy(h => h.Type).Sum(s => s.RemainingRooms))
        {
            return false;
        }

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
        return true;
    }
}
