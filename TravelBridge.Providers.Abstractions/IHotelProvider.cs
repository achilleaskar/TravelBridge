using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Interface for hotel providers (WebHotelier, Owned, etc.).
/// Each provider implementation knows how to fetch hotel data from its specific source.
/// </summary>
public interface IHotelProvider
{
    /// <summary>
    /// The unique identifier for this provider.
    /// Use <see cref="ProviderIds"/> constants.
    /// </summary>
    int ProviderId { get; }

    /// <summary>
    /// Gets hotel information.
    /// </summary>
    /// <param name="query">The hotel info query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The hotel information result.</returns>
    Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets room information.
    /// </summary>
    /// <param name="query">The room info query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The room information result.</returns>
    Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets hotel availability for the specified dates and party (single hotel).
    /// </summary>
    /// <param name="query">The availability query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The availability result.</returns>
    Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches for available hotels in a geographic area (multi-hotel search).
    /// </summary>
    /// <param name="query">The search query with bounding box, dates, and party.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The search result with available hotels.</returns>
    Task<SearchAvailabilityResult> SearchAvailabilityAsync(SearchAvailabilityQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets alternative available dates for a hotel when the requested dates have no availability.
    /// </summary>
    /// <param name="query">The alternatives query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The alternatives result with suggested dates.</returns>
    Task<AlternativesResult> GetAlternativesAsync(AlternativesQuery query, CancellationToken cancellationToken = default);
}
