namespace TravelBridge.Providers.WebHotelier
{
    /// <summary>
    /// WebHotelier service parameter model for single hotel availability requests
    /// Internal use only - used to pass parameters to WebHotelier API calls
    /// </summary>
    public class WHSingleAvailabilityRequest
    {
        public required string PropertyId { get; init; }
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public required string? Party { get; init; }
    }
}
