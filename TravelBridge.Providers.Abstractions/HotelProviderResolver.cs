namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Resolves the appropriate <see cref="IHotelProvider"/> based on <see cref="AvailabilitySource"/>.
/// 
/// Usage:
/// 1. Register all IHotelProvider implementations in DI
/// 2. Inject HotelProviderResolver where needed
/// 3. Call GetProvider(source) or GetProvider(compositeHotelId)
/// 
/// Design: Uses IEnumerable injection pattern for easy extensibility.
/// Adding a new provider only requires registering it in DI.
/// </summary>
public class HotelProviderResolver
{
    private readonly Dictionary<AvailabilitySource, IHotelProvider> _providers;

    /// <summary>
    /// Creates a resolver from a collection of providers.
    /// Typically injected via DI: services.AddSingleton&lt;HotelProviderResolver&gt;()
    /// </summary>
    /// <param name="providers">All registered IHotelProvider implementations</param>
    public HotelProviderResolver(IEnumerable<IHotelProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        _providers = providers.ToDictionary(p => p.Source);

        if (_providers.Count == 0)
        {
            throw new InvalidOperationException(
                "No IHotelProvider implementations registered. " +
                "Register at least one provider in the DI container.");
        }
    }

    /// <summary>
    /// Gets the provider for the specified source.
    /// </summary>
    /// <param name="source">The availability source</param>
    /// <returns>The provider for the source</returns>
    /// <exception cref="InvalidOperationException">If no provider is registered for the source</exception>
    public IHotelProvider GetProvider(AvailabilitySource source)
    {
        if (!_providers.TryGetValue(source, out var provider))
        {
            throw new InvalidOperationException(
                $"No IHotelProvider registered for source '{source}'. " +
                $"Available sources: {string.Join(", ", _providers.Keys)}");
        }

        return provider;
    }

    /// <summary>
    /// Gets the provider for the specified composite hotel ID.
    /// Extracts the source from the hotel ID and returns the appropriate provider.
    /// </summary>
    /// <param name="hotelId">Composite hotel ID (e.g., "wh:VAROSRESID")</param>
    /// <returns>The provider for the hotel</returns>
    public IHotelProvider GetProvider(CompositeHotelId hotelId)
    {
        return GetProvider(hotelId.Source);
    }

    /// <summary>
    /// Gets the provider for the specified composite hotel ID string.
    /// Parses the string and returns the appropriate provider.
    /// </summary>
    /// <param name="compositeHotelIdString">Composite hotel ID string (e.g., "wh:VAROSRESID" or "1-VAROSRESID")</param>
    /// <returns>The provider for the hotel</returns>
    public IHotelProvider GetProvider(string compositeHotelIdString)
    {
        var hotelId = CompositeHotelId.Parse(compositeHotelIdString);
        return GetProvider(hotelId);
    }

    /// <summary>
    /// Tries to get the provider for the specified source.
    /// </summary>
    /// <param name="source">The availability source</param>
    /// <param name="provider">The provider if found</param>
    /// <returns>True if a provider was found</returns>
    public bool TryGetProvider(AvailabilitySource source, out IHotelProvider? provider)
    {
        return _providers.TryGetValue(source, out provider);
    }

    /// <summary>
    /// Checks if a provider is registered for the specified source.
    /// </summary>
    /// <param name="source">The availability source</param>
    /// <returns>True if a provider is registered</returns>
    public bool HasProvider(AvailabilitySource source)
    {
        return _providers.ContainsKey(source);
    }

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    public IEnumerable<IHotelProvider> GetAllProviders()
    {
        return _providers.Values;
    }

    /// <summary>
    /// Gets all registered sources.
    /// </summary>
    public IEnumerable<AvailabilitySource> GetRegisteredSources()
    {
        return _providers.Keys;
    }
}
