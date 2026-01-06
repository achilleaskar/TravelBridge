using TravelBridge.Providers.Abstractions.Queries;
using TravelBridge.Providers.Abstractions.Results;

namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Unified interface for all hotel availability providers.
/// Implementations: WebHotelierHotelProvider, OwnedInventoryHotelProvider (Phase 3)
/// 
/// Phase 1: Read-only operations (search, availability, info)
/// Phase 3: Will add booking operations (CreateBookingAsync, CancelBookingAsync)
/// </summary>
public interface IHotelProvider
{
    /// <summary>
    /// Identifies which provider this implementation represents.
    /// Used by <see cref="HotelProviderResolver"/> to route requests.
    /// </summary>
    AvailabilitySource Source { get; }

    #region Search Operations

    /// <summary>
    /// Search for hotels by location and date range.
    /// Returns hotels with availability and pricing.
    /// </summary>
    /// <param name="query">Search criteria including dates, location, and party configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Search results with hotels and filters</returns>
    Task<HotelSearchResult> SearchHotelsAsync(HotelSearchQuery query, CancellationToken ct = default);

    /// <summary>
    /// Search properties by name for autocomplete.
    /// </summary>
    /// <param name="searchTerm">Search term to match against property names</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Matching hotel summaries</returns>
    Task<IEnumerable<HotelSummary>> SearchPropertiesAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Get all properties from this provider.
    /// Used for property listings without search filters.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>All available hotel summaries</returns>
    Task<IEnumerable<HotelSummary>> GetAllPropertiesAsync(CancellationToken ct = default);

    #endregion

    #region Single Hotel Operations

    /// <summary>
    /// Get detailed availability for a single hotel.
    /// Includes room types, rates, and alternative dates.
    /// </summary>
    /// <param name="query">Query with hotel ID, dates, and party configuration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Detailed availability with rooms and rates</returns>
    Task<HotelAvailabilityResult> GetAvailabilityAsync(AvailabilityQuery query, CancellationToken ct = default);

    /// <summary>
    /// Get static hotel information (description, photos, amenities).
    /// </summary>
    /// <param name="query">Query with hotel ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Hotel information</returns>
    Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default);

    /// <summary>
    /// Get detailed room information (photos, amenities, capacity).
    /// </summary>
    /// <param name="query">Query with hotel ID and room ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Room information</returns>
    Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default);

    #endregion

    // NOTE: Booking operations will be added in Phase 3:
    // Task<BookingResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken ct = default);
    // Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken ct = default);
}
