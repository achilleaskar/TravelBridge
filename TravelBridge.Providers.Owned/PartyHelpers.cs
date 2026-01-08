using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Providers.Owned;

/// <summary>
/// Helper methods for working with party configurations in the Owned provider.
/// Aligned with the actual PartyConfiguration model.
/// </summary>
public static class PartyHelpers
{
    /// <summary>
    /// Calculate total requested rooms from party configuration.
    /// For Owned provider, this is simply the count of rooms in the party.
    /// </summary>
    /// <param name="party">The party configuration</param>
    /// <returns>Total number of rooms requested</returns>
    public static int GetRequestedRooms(PartyConfiguration party)
    {
        if (party?.Rooms == null || party.Rooms.Count == 0)
            throw new ArgumentException("Party must contain at least one room", nameof(party));

        return party.RoomCount; // Uses built-in RoomCount property
    }

    /// <summary>
    /// Get adults count from the first room in the party.
    /// Used for building rate IDs which represent a single room's occupancy.
    /// </summary>
    /// <param name="party">The party configuration</param>
    /// <returns>Number of adults in the first room</returns>
    public static int GetAdults(PartyConfiguration party)
    {
        if (party?.Rooms == null || party.Rooms.Count == 0)
            throw new ArgumentException("Party must contain at least one room", nameof(party));

        return party.Rooms[0].Adults;
    }

    /// <summary>
    /// Get children ages from the first room in the party.
    /// Used for building rate IDs which represent a single room's occupancy.
    /// </summary>
    /// <param name="party">The party configuration</param>
    /// <returns>Array of children ages from the first room</returns>
    public static int[] GetChildrenAges(PartyConfiguration party)
    {
        if (party?.Rooms == null || party.Rooms.Count == 0)
            throw new ArgumentException("Party must contain at least one room", nameof(party));

        return party.Rooms[0].ChildrenAges ?? Array.Empty<int>();
    }

    /// <summary>
    /// Build the party suffix for rate IDs.
    /// Format: "{adults}[_{childAge1}_{childAge2}...]"
    /// Examples: "2", "2_5_10", "10_3_7_9"
    /// </summary>
    /// <param name="party">The party configuration</param>
    /// <returns>Party suffix string compatible with FillPartyFromId parsing</returns>
    public static string GetPartySuffix(PartyConfiguration party)
    {
        var adults = GetAdults(party);
        var childrenAges = GetChildrenAges(party);

        if (childrenAges.Length == 0)
            return adults.ToString();

        return $"{adults}_{string.Join("_", childrenAges)}";
    }

    /// <summary>
    /// Build a complete rate ID for an owned room type.
    /// Format: "rt_{roomTypeId}-{adults}[_{childAges}]"
    /// Examples: "rt_5-2", "rt_5-2_5_10"
    /// 
    /// This format is compatible with the existing FillPartyFromId() parsing logic.
    /// </summary>
    /// <param name="roomTypeId">The room type database ID</param>
    /// <param name="party">The party configuration</param>
    /// <returns>Complete rate ID string</returns>
    public static string BuildRateId(int roomTypeId, PartyConfiguration party)
    {
        var partySuffix = GetPartySuffix(party);
        return $"rt_{roomTypeId}-{partySuffix}";
    }

    /// <summary>
    /// Convert party configuration to JSON string representation.
    /// Used for populating SearchParty.PartyJson in rate responses.
    /// </summary>
    /// <param name="party">The party configuration</param>
    /// <returns>JSON string or null if conversion fails</returns>
    public static string? ToPartyJson(PartyConfiguration party)
    {
        if (party?.Rooms == null || party.Rooms.Count == 0)
            return null;

        try
        {
            var rooms = party.Rooms.Select(r => new
            {
                adults = r.Adults,
                children = r.ChildrenAges.Length > 0 ? r.ChildrenAges : null
            }).ToList();

            return System.Text.Json.JsonSerializer.Serialize(rooms);
        }
        catch
        {
            return null;
        }
    }
}
