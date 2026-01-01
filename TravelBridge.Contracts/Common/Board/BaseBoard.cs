namespace TravelBridge.Contracts.Common.Board
{
    public abstract class BaseBoard
    {
        [JsonPropertyName("board")]
        public int? BoardType { get; set; }
    }
}
