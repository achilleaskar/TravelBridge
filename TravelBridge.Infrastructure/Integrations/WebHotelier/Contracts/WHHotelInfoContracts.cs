using System.Text.Json.Serialization;
using TravelBridge.Core.Entities;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts
{
    /// <summary>
    /// WebHotelier hotel info response.
    /// </summary>
    public class WHHotelInfoResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHHotelData? Data { get; set; }
    }

    public class WHHotelData
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonIgnore]
        public string Id => $"{(int)HotelProvider.WebHotelier}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("location")]
        public WHLocation? Location { get; set; }

        [JsonPropertyName("operation")]
        public WHOperation? Operation { get; set; }

        [JsonPropertyName("facilities")]
        public IEnumerable<string>? Facilities { get; set; }

        [JsonPropertyName("photos")]
        public IEnumerable<WHPhoto>? Photos { get; set; }

        [JsonIgnore]
        public IEnumerable<string>? LargePhotos { get; set; }
    }

    /// <summary>
    /// WebHotelier room info response.
    /// </summary>
    public class WHRoomInfoResponse : WebHotelierResponse
    {
        [JsonPropertyName("data")]
        public WHRoomData? Data { get; set; }
    }

    public class WHRoomData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("capacity")]
        public WHRoomCapacity? Capacity { get; set; }

        [JsonPropertyName("amenities")]
        public List<string>? Amenities { get; set; }

        [JsonPropertyName("photos")]
        public IEnumerable<WHPhoto>? Photos { get; set; }

        [JsonIgnore]
        public IEnumerable<string>? LargePhotos { get; set; }

        [JsonIgnore]
        public IEnumerable<string>? MediumPhotos { get; set; }
    }

    public class WHRoomCapacity
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
    }
}
