namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Interface for hotel provider operations (search, info, availability).
    /// Implementations: WebHotelierPropertiesService, future: Booking.com, Expedia, etc.
    /// </summary>
    /// <remarks>
    /// This interface defines read-only operations that all hotel providers must support.
    /// Booking operations are handled separately as they have provider-specific logic.
    /// </remarks>
    public interface IHotelProvider
    {
        /// <summary>
        /// Gets the unique provider identifier (e.g., 1 for WebHotelier).
        /// </summary>
        int ProviderId { get; }

        /// <summary>
        /// Gets the provider name for display purposes.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Searches for properties by name.
        /// </summary>
        /// <param name="propertyName">The property name to search for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection of matching hotels.</returns>
        Task<IEnumerable<ProviderHotelSearchResult>> SearchPropertiesAsync(string propertyName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available properties from this provider.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all hotels.</returns>
        Task<IReadOnlyList<ProviderHotelSearchResult>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed hotel information.
        /// </summary>
        /// <param name="hotelCode">The provider-specific hotel code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Hotel information.</returns>
        Task<ProviderHotelDetails> GetHotelInfoAsync(string hotelCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets detailed room information.
        /// </summary>
        /// <param name="hotelCode">The provider-specific hotel code.</param>
        /// <param name="roomCode">The room type code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Room information.</returns>
        Task<ProviderRoomDetails> GetRoomInfoAsync(string hotelCode, string roomCode, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Provider-agnostic hotel search result.
    /// </summary>
    public class ProviderHotelSearchResult
    {
        public required string Id { get; init; }
        public required string Code { get; init; }
        public required int ProviderId { get; init; }
        public required string Name { get; init; }
        public string? Location { get; init; }
        public string? CountryCode { get; init; }
        public string? PropertyType { get; init; }
    }

    /// <summary>
    /// Provider-agnostic hotel details.
    /// </summary>
    public class ProviderHotelDetails
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public int? Rating { get; init; }
        public string? PropertyType { get; init; }
        public ProviderHotelLocation? Location { get; init; }
        public ProviderHotelOperation? Operation { get; init; }
        public IEnumerable<string>? Photos { get; init; }
        public IEnumerable<string>? Facilities { get; init; }
    }

    /// <summary>
    /// Hotel location information.
    /// </summary>
    public class ProviderHotelLocation
    {
        public decimal? Latitude { get; init; }
        public decimal? Longitude { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
        public string? ZipCode { get; init; }
        public string? CountryCode { get; init; }
    }

    /// <summary>
    /// Hotel operation hours.
    /// </summary>
    public class ProviderHotelOperation
    {
        public string? CheckInTime { get; init; }
        public string? CheckOutTime { get; init; }
    }

    /// <summary>
    /// Provider-agnostic room details.
    /// </summary>
    public class ProviderRoomDetails
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Description { get; init; }
        public int? MaxOccupancy { get; init; }
        public IEnumerable<string>? Photos { get; init; }
        public IEnumerable<string>? Amenities { get; init; }
    }
}
