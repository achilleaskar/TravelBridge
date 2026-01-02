namespace TravelBridge.Providers.WebHotelier.Models.Room;

/// <summary>
/// WebHotelier wire model for room info.
/// </summary>
public class WHRoomInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("capacity")]
    public WHRoomCapacity Capacity { get; set; } = new();

    [JsonPropertyName("amenities")]
    public List<string> Amenities { get; set; } = [];

    [JsonPropertyName("photos")]
    public IEnumerable<WHPhotoInfo> PhotosItems { get; set; } = [];

    public IEnumerable<string> LargePhotos { get; set; } = [];
    public IEnumerable<string> MediumPhotos { get; set; } = [];
}
