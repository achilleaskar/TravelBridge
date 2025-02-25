using System.Text.Json.Serialization;

namespace TravelBridge.API.Models.WebHotelier
{
    public abstract class BaseBoard
    {
        [JsonPropertyName("board")]
        public int? BoardType { get; set; }
    }
}