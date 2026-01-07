using TravelBridge.Providers.Abstractions;

namespace TravelBridge.API.Providers;

/// <summary>
/// Resolves hotel providers by their provider ID.
/// Builds a dictionary lookup once in constructor for O(1) resolution.
/// </summary>
public class HotelProviderResolver : IHotelProviderResolver
{
    private readonly Dictionary<int, IHotelProvider> _providers;
    private readonly ILogger<HotelProviderResolver> _logger;

    public HotelProviderResolver(IEnumerable<IHotelProvider> providers, ILogger<HotelProviderResolver> logger)
    {
        _logger = logger;
        
        // Build dictionary once in constructor - O(1) lookup thereafter
        _providers = new Dictionary<int, IHotelProvider>();
        
        foreach (var provider in providers)
        {
            if (_providers.ContainsKey(provider.ProviderId))
            {
                _logger.LogWarning("Duplicate provider registration for ProviderId: {ProviderId}. Keeping first registration.", 
                    provider.ProviderId);
                continue;
            }
            
            _providers[provider.ProviderId] = provider;
            _logger.LogInformation("Registered hotel provider: {ProviderType} with ProviderId: {ProviderId}", 
                provider.GetType().Name, provider.ProviderId);
        }
    }

    /// <inheritdoc />
    public IHotelProvider GetRequired(int providerId)
    {
        if (!_providers.TryGetValue(providerId, out var provider))
        {
            throw new NotSupportedException($"Hotel provider '{providerId}' is not supported.");
        }
        
        return provider;
    }

    /// <inheritdoc />
    public bool TryGet(int providerId, out IHotelProvider? provider)
    {
        return _providers.TryGetValue(providerId, out provider);
    }

    /// <inheritdoc />
    public IEnumerable<IHotelProvider> GetAll()
    {
        return _providers.Values;
    }
}
