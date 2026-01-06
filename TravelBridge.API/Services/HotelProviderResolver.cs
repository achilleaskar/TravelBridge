using TravelBridge.Contracts.Providers;

namespace TravelBridge.API.Services;

/// <summary>
/// Resolves which IHotelProvider implementation to use for a given hotel.
/// 
/// Phase 1: All hotels are routed to WebHotelier. In future phases, this will:
/// - Check if hotel exists in OwnedHotel table → use OwnedInventoryProvider
/// - Otherwise → use WebHotelierProvider
/// 
/// The resolver accepts both AvailabilitySource (explicit) and hotelId parameters
/// because the same hotel ID might exist in multiple providers.
/// </summary>
public class HotelProviderResolver
{
    private readonly IEnumerable<IHotelProvider> _providers;
    private readonly ILogger<HotelProviderResolver> _logger;

    public HotelProviderResolver(
        IEnumerable<IHotelProvider> providers,
        ILogger<HotelProviderResolver> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// Get a provider by explicit availability source.
    /// Use this when you know exactly which provider you need.
    /// </summary>
    /// <param name="source">The availability source to get provider for</param>
    /// <returns>The matching provider</returns>
    /// <exception cref="InvalidOperationException">If no provider is registered for the source</exception>
    public IHotelProvider GetProvider(AvailabilitySource source)
    {
        var provider = _providers.FirstOrDefault(p => p.Source == source);
        
        if (provider == null)
        {
            _logger.LogError("No provider registered for source: {Source}", source);
            throw new InvalidOperationException($"No provider registered for availability source: {source}");
        }

        _logger.LogDebug("Resolved provider {ProviderType} for source {Source}", 
            provider.GetType().Name, source);
        
        return provider;
    }

    /// <summary>
    /// Get a provider for a specific hotel ID and explicit source.
    /// Use this when you have a composite hotel ID (e.g., "1-HOTELCODE") or need to specify the source.
    /// </summary>
    /// <param name="source">The availability source</param>
    /// <param name="hotelId">The hotel identifier (provider-specific part only)</param>
    /// <returns>The matching provider</returns>
    public IHotelProvider GetProvider(AvailabilitySource source, string hotelId)
    {
        _logger.LogDebug("Resolving provider for source {Source}, hotel {HotelId}", source, hotelId);
        return GetProvider(source);
    }

    /// <summary>
    /// Parse a composite hotel ID (e.g., "1-HOTELCODE" or "2-VAROSRESID") and resolve the provider.
    /// The prefix number maps to AvailabilitySource enum.
    /// </summary>
    /// <param name="compositeHotelId">Hotel ID in format "{SourceId}-{ProviderHotelId}"</param>
    /// <returns>Tuple of (provider, provider-specific hotel ID)</returns>
    /// <exception cref="ArgumentException">If the hotel ID format is invalid</exception>
    public (IHotelProvider Provider, string HotelId) ResolveFromCompositeId(string compositeHotelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(compositeHotelId);

        var parts = compositeHotelId.Split('-', 2);
        if (parts.Length != 2)
        {
            _logger.LogWarning("Invalid composite hotel ID format: {HotelId}", compositeHotelId);
            throw new ArgumentException(
                $"Invalid hotel ID format: '{compositeHotelId}'. Expected format: '{{SourceId}}-{{HotelId}}'",
                nameof(compositeHotelId));
        }

        if (!int.TryParse(parts[0], out var sourceId))
        {
            _logger.LogWarning("Invalid source ID in hotel ID: {HotelId}", compositeHotelId);
            throw new ArgumentException(
                $"Invalid source ID '{parts[0]}' in hotel ID. Must be a number.",
                nameof(compositeHotelId));
        }

        if (!Enum.IsDefined(typeof(AvailabilitySource), sourceId))
        {
            _logger.LogWarning("Unknown availability source {SourceId} in hotel ID: {HotelId}", sourceId, compositeHotelId);
            throw new ArgumentException(
                $"Unknown availability source: {sourceId}. Valid sources: {string.Join(", ", Enum.GetNames<AvailabilitySource>())}",
                nameof(compositeHotelId));
        }

        var source = (AvailabilitySource)sourceId;
        var hotelId = parts[1];

        _logger.LogDebug("Parsed composite ID {CompositeId} -> Source: {Source}, HotelId: {HotelId}",
            compositeHotelId, source, hotelId);

        return (GetProvider(source), hotelId);
    }

    /// <summary>
    /// Get all registered providers.
    /// Useful for operations that need to aggregate results from multiple providers (e.g., search).
    /// </summary>
    public IEnumerable<IHotelProvider> GetAllProviders() => _providers;

    /// <summary>
    /// Check if a provider is registered for the given source.
    /// </summary>
    public bool HasProvider(AvailabilitySource source) 
        => _providers.Any(p => p.Source == source);
}
