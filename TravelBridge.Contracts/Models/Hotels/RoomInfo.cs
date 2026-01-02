namespace TravelBridge.Contracts.Models.Hotels;

/// <summary>
/// Room information.
/// API contract type.
/// </summary>
public class RoomInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public RoomCapacity Capacity { get; set; } = new();

    [JsonPropertyName("amenities")]
    public List<string> Amenities { get; set; } = [];

    [JsonPropertyName("photos")]
    public IEnumerable<PhotoInfo> PhotosItems { get; set; } = [];

    public IEnumerable<string> LargePhotos { get; set; } = [];
    public IEnumerable<string> MediumPhotos { get; set; } = [];
}

/// <summary>
/// Room capacity information.
/// API contract type.
/// </summary>
public class RoomCapacity
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
