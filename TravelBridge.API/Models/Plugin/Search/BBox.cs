using System.Text.Json.Serialization;

namespace TravelBridge.API.Models.Plugin.Search
{
    public class BBox
    {
        [JsonPropertyName("lat1")]
        public string BottomLeftLatitude { get; set; } // Bottom left latitude

        [JsonPropertyName("lat2")]
        public string TopRightLatitude { get; set; } // Top right latitude

        [JsonPropertyName("lon1")]
        public string BottomLeftLongitude { get; set; } // Bottom left longitude

        [JsonPropertyName("lon2")]
        public string TopRightLongitude { get; set; } // Top right longitude
    }
}
