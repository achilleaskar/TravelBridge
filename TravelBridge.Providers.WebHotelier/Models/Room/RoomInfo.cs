using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.Providers.WebHotelier.Models.Room
{
    public class RoomInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("capacity")]
        public RoomCapacity Capacity { get; set; }

        [JsonPropertyName("amenities")]
        public List<string> Amenities { get; set; }

        [JsonPropertyName("photos")]
        public IEnumerable<PhotoInfo> PhotosItems { get; set; }

        public IEnumerable<string> LargePhotos { get; set; }
        public IEnumerable<string> MediumPhotos { get; set; }
    }
}
