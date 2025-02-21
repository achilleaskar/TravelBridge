using System.Text.Json.Serialization;

namespace TravelBridge.API.Contracts
{
    public class RoomInfoRespone
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMessage { get; set; }

        [JsonPropertyName("data")]
        public RoomInfo Data { get; set; }
    }

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

        [JsonPropertyName("location")]
        public LocationInfo Location { get; set; }

        [JsonPropertyName("active")]
        public bool IsActive { get; set; }
    }

    public class RoomCapacity
    {
        [JsonPropertyName("min_pers")]
        public int MinPersons { get; set; }

        [JsonPropertyName("max_pers")]
        public int MaxPersons { get; set; }

        [JsonPropertyName("max_adults")]
        public int MaxAdults { get; set; }

        [JsonPropertyName("max_infants")]
        public int MaxInfants { get; set; }

        [JsonPropertyName("children_allowed")]
        public bool ChildrenAllowed { get; set; }

        [JsonPropertyName("count_infant")]
        public bool CountInfant { get; set; }
    }

    public class RoomPhoto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("xsmall")]
        public string XSmall { get; set; }

        [JsonPropertyName("small")]
        public string Small { get; set; }

        [JsonPropertyName("medium")]
        public string Medium { get; set; }

        [JsonPropertyName("large")]
        public string Large { get; set; }
    }
}