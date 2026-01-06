namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Resolves hotel providers by their provider ID.
/// Implementation should be in the API layer to manage DI registration.
/// </summary>
public interface IHotelProviderResolver
{
    /// <summary>
    /// Gets the provider for the specified provider ID.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <returns>The hotel provider.</returns>
    /// <exception cref="ArgumentException">Thrown when no provider is registered for the given ID.</exception>
    IHotelProvider GetRequired(int providerId);

    /// <summary>
    /// Attempts to get the provider for the specified provider ID.
    /// </summary>
    /// <param name="providerId">The provider identifier.</param>
    /// <param name="provider">When this method returns, contains the provider if found.</param>
    /// <returns>true if a provider was found; otherwise, false.</returns>
    bool TryGet(int providerId, out IHotelProvider? provider);

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    IEnumerable<IHotelProvider> GetAll();
}
