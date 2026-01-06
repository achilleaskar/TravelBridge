namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Identifies the source/provider for hotel availability data.
/// Used by <see cref="HotelProviderResolver"/> to route requests to the correct provider.
/// </summary>
public enum AvailabilitySource
{
    /// <summary>
    /// Our own managed inventory (hotels we own/manage directly).
    /// Data stored in local MySQL database.
    /// </summary>
    Owned = 0,

    /// <summary>
    /// External WebHotelier API provider.
    /// Data fetched from WebHotelier's availability API.
    /// </summary>
    WebHotelier = 1
}
