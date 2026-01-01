namespace TravelBridge.Contracts.Models.Hotels
{
    public class BaseHotelInfo
    {
        [JsonPropertyName("code")]
        public string Code {  get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
