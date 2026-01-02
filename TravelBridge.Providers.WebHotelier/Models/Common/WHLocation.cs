namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for location.
/// </summary>
public class WHLocation
{
    [JsonPropertyName("lat")]
    public decimal? Latitude { get; set; }

    [JsonPropertyName("lon")]
    public decimal? Longitude { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
