namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for provider identification.
/// </summary>
public enum WHProvider
{
    WebHotelier = 1
}

/// <summary>
/// WebHotelier wire model for base hotel info.
/// </summary>
public class WHBaseHotelInfo
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public WHProvider Provider { get; set; }

    public string Id => $"{(int)Provider}-{Code}";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
