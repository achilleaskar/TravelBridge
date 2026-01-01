namespace TravelBridge.Contracts.Common.Board
{
    public class Board
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
