namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Room/party configuration for availability searches.
/// Represents one room with its occupancy requirements.
/// </summary>
/// <param name="Adults">Number of adults (must be >= 1)</param>
/// <param name="ChildrenAges">Ages of children (empty array if no children)</param>
public record PartyConfiguration(int Adults, int[] ChildrenAges)
{
    /// <summary>
    /// Creates a party configuration for adults only.
    /// </summary>
    public static PartyConfiguration AdultsOnly(int adults) => new(adults, []);

    /// <summary>
    /// Total occupancy (adults + children).
    /// </summary>
    public int TotalOccupancy => Adults + ChildrenAges.Length;

    /// <summary>
    /// Number of children.
    /// </summary>
    public int ChildrenCount => ChildrenAges.Length;

    /// <summary>
    /// Converts to JSON string format used by WebHotelier.
    /// Example: {"adults":2,"children":[5,10]}
    /// </summary>
    public string ToJsonString()
    {
        if (ChildrenAges.Length == 0)
            return $"{{\"adults\":{Adults}}}";
        
        var childrenJson = string.Join(",", ChildrenAges);
        return $"{{\"adults\":{Adults},\"children\":[{childrenJson}]}}";
    }
}
