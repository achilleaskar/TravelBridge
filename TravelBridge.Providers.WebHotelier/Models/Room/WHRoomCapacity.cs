namespace TravelBridge.Providers.WebHotelier.Models.Room;

/// <summary>
/// WebHotelier wire model for room capacity.
/// </summary>
public class WHRoomCapacity
{
    [JsonPropertyName("min_pers")]
    public int MinPersons { get; set; }

    [JsonPropertyName("max_pers")]
    public int MaxPersons { get; set; }

    [JsonPropertyName("max_adults")]
    public int MaxAdults { get; set; }

    [JsonPropertyName("max_infants")]
    public int MaxInfants { get; set; }

    [JsonPropertyName("children_allowed")]
    public bool ChildrenAllowed { get; set; }

    [JsonPropertyName("count_infant")]
    public bool CountInfant { get; set; }
}
