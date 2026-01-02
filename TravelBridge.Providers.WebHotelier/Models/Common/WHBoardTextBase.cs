namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for board info.
/// </summary>
public class WHBoard
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// WebHotelier wire model for board text display.
/// </summary>
public class WHBoardTextBase
{
    public List<WHBoard> Boards { get; set; } = new();
    public string BoardsText { get; set; } = string.Empty;
    public bool HasBoards { get; set; }
}
