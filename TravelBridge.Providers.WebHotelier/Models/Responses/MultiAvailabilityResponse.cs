namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire response for multi-availability search.
/// </summary>
public class WHMultiAvailabilityResponse : WHBaseResponse
{
    [JsonPropertyName("data")]
    public WHData? Data { get; set; }
}
