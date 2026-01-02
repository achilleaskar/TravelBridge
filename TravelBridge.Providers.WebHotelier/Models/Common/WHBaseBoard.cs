namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for board type.
/// </summary>
public abstract class WHBaseBoard
{
    [JsonPropertyName("board")]
    public int? BoardType { get; set; }
}
