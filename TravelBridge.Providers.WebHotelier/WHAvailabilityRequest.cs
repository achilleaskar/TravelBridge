namespace TravelBridge.Providers.WebHotelier
{
    /// <summary>
    /// WebHotelier service parameter model for multi-property availability requests
    /// Internal use only - used to pass parameters to WebHotelier API calls
    /// </summary>
    public class WHAvailabilityRequest
    {
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public required string Party { get; init; }
        public required string Lat { get; init; }
        public required string Lon { get; init; }
        public required string BottomLeftLatitude { get; init; }
        public required string TopRightLatitude { get; init; }
        public required string BottomLeftLongitude { get; init; }
        public required string TopRightLongitude { get; init; }
        public required string SortBy { get; init; }
        public required string SortOrder { get; init; }
    }
}
