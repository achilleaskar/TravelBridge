using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.Contracts.Models.Hotels;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Services;

/// <summary>
/// Handles availability-related business logic for search results and single hotel availability.
/// Extracted from PluginSearchResponse and SingleAvailabilityResponse DTOs to keep them as pure data containers.
/// </summary>
public static class AvailabilityProcessor
{
    /// <summary>
    /// Filters hotels that don't have enough rooms to cover the requested party configuration.
    /// </summary>
    /// <param name="response">The plugin search response containing hotel results</param>
    /// <param name="partyList">The party configuration requested</param>
    /// <returns>Filtered list of hotels that can accommodate the request</returns>
    public static IEnumerable<WebHotel> FilterHotelsByAvailability(PluginSearchResponse response, List<PartyItem> partyList)
    {
        if (response.Results == null)
        {
            return [];
        }

        List<WebHotel> invalid = [];
        foreach (var hotel in response.Results)
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
                            .GroupBy(r => r.Type)
                            .Select(g => g.First())
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

        return response.Results.Except(invalid);
    }

    /// <summary>
    /// Checks if the single hotel availability response has enough rooms to cover the selected rates.
    /// </summary>
    /// <param name="response">The single availability response</param>
    /// <param name="selectedRates">The list of selected rates with counts</param>
    /// <returns>True if there are enough rooms available, false otherwise</returns>
    public static bool HasSufficientAvailability(SingleAvailabilityResponse response, List<SelectedRate> selectedRates)
    {
        if (response.Data?.Rooms == null)
        {
            return false;
        }

        if (selectedRates.Sum(a => a.count) > response.Data.Rooms.Sum(s => s.Rates.FirstOrDefault()?.RemainingRooms ?? 0))
        {
            return false;
        }

        foreach (var party in selectedRates)
        {
            int sum = 0;
            foreach (var room in response.Data.Rooms)
            {
                foreach (var rate in room.Rates)
                {
                    var partyItem = JsonSerializer.Deserialize<List<PartyItem>>(party.searchParty)?.FirstOrDefault();
                    if (partyItem == null)
                    {
                        throw new ArgumentException("Invalid party data format. Ensure it's valid JSON.");
                    }
                    
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

        return true;
    }
}
