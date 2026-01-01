namespace TravelBridge.Contracts.Common
{
    /// <summary>
    /// USAGE: Internal processing - parsed from API request string parameter
    /// SHOULD SPLIT: NO - This is internal processing model, not directly from API or provider
    /// CREATED BY: SearchPluginEndpoints.TryGetBBox() parsing bbox query parameter
    /// USED IN: SearchPluginEndpoints to create MultiAvailabilityRequest for WebHotelier
    /// NOTE: API receives bbox as string "[lon1,lat1,lon2,lat2]-lat-lon", this parses it
    /// </summary>
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
