namespace TravelBridge.Providers.WebHotelier.Models.Hotel;

/// <summary>
/// WebHotelier wire model for hotel info with availability.
/// Used for single hotel availability responses.
/// </summary>
public class WHHotelInfo : WHBaseHotelInfo
{
    [JsonPropertyName("location")]
    public WHLocation Location { get; set; } = new();

    [JsonPropertyName("rates")]
    public List<WHHotelRate> Rates { get; set; } = new();
}
