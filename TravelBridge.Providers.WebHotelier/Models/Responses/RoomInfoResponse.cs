using TravelBridge.Providers.WebHotelier.Models.Room;

namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire response for room info.
/// </summary>
public class WHRoomInfoResponse : WHBaseResponse
{
    [JsonPropertyName("data")]
    public WHRoomInfo? Data { get; set; }
}
