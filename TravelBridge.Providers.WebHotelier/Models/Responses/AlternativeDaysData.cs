namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire response for alternative dates.
/// </summary>
public class WHAlternativeDaysData : WHBaseResponse
{
    [JsonPropertyName("data")]
    public WHAlternativesInfo? Data { get; set; }

    public List<WHAlternative> Alternatives { get; internal set; } = [];
}
