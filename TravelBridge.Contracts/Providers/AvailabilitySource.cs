using System.ComponentModel;

namespace TravelBridge.Contracts.Providers;

/// <summary>
/// Identifies the source/provider for hotel availability data.
/// Used by HotelProviderResolver to determine which IHotelProvider implementation to use.
/// </summary>
public enum AvailabilitySource
{
    /// <summary>
    /// Hotel inventory managed internally in our database.
    /// Full control over room types, pricing, and availability.
    /// </summary>
    [Description("Owned Inventory")]
    Owned = 1,

    /// <summary>
    /// Hotel availability fetched from WebHotelier external API.
    /// </summary>
    [Description("WebHotelier")]
    WebHotelier = 2
}
