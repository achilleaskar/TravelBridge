namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Interface for geocoding/location services.
    /// Implementations: MapBoxService, HereMapsService, future: Google Maps, etc.
    /// </summary>
    public interface IGeocodingProvider
    {
        /// <summary>
        /// Gets the provider name for display purposes.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Searches for locations by query string.
        /// </summary>
        /// <param name="query">Search query (city name, address, etc.).</param>
        /// <param name="language">Language code (e.g., "el", "en").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of matching locations.</returns>
        Task<IEnumerable<GeoLocation>> SearchLocationsAsync(string query, string? language = null, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents a geographic location from geocoding.
    /// </summary>
    public class GeoLocation
    {
        /// <summary>
        /// Location name (city, neighborhood, etc.).
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Region/state name.
        /// </summary>
        public string? Region { get; init; }

        /// <summary>
        /// Country code (ISO 2-letter).
        /// </summary>
        public string? CountryCode { get; init; }

        /// <summary>
        /// Latitude coordinate.
        /// </summary>
        public decimal? Latitude { get; init; }

        /// <summary>
        /// Longitude coordinate.
        /// </summary>
        public decimal? Longitude { get; init; }

        /// <summary>
        /// Bounding box for area searches [west, south, east, north].
        /// </summary>
        public decimal[]? BoundingBox { get; init; }

        /// <summary>
        /// Unique identifier for this location (provider-specific format).
        /// </summary>
        public string? Id { get; init; }

        /// <summary>
        /// Type of location (city, neighborhood, region, etc.).
        /// </summary>
        public string? LocationType { get; init; }
    }
}
