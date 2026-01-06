namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Well-known provider identifiers.
/// Provider ID format: "{providerId}-{value}" where providerId is an integer.
/// </summary>
public static class ProviderIds
{
    /// <summary>
    /// Owned inventory provider (future use).
    /// </summary>
    public const int Owned = 0;

    /// <summary>
    /// WebHotelier provider.
    /// </summary>
    public const int WebHotelier = 1;
}
