using Microsoft.Extensions.Logging;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Contracts.Plugin.AutoComplete;
using TravelBridge.Contracts.Providers;

namespace TravelBridge.Providers.WebHotelier;

/// <summary>
/// WebHotelier implementation of IHotelProvider.
/// Wraps the WebHotelierClient to provide hotel availability from the WebHotelier external API.
/// 
/// Note: For Phase 1, the complex operations (SearchHotels, GetHotelAvailability, CreateBooking)
/// are still handled by WebHotelierPropertiesService in the API layer. These methods throw
/// NotImplementedException and the HotelProviderResolver routes to the existing service.
/// 
/// Simple operations (SearchPropertiesByName, GetAllProperties, GetHotelInfo, GetRoomInfo, CancelBooking)
/// are fully implemented here.
/// </summary>
public class WebHotelierHotelProvider : IHotelProvider
{
    private readonly WebHotelierClient _client;
    private readonly ILogger<WebHotelierHotelProvider> _logger;

    public WebHotelierHotelProvider(WebHotelierClient client, ILogger<WebHotelierHotelProvider> logger)
    {
        _client = client;
        _logger = logger;
    }

    /// <inheritdoc />
    public AvailabilitySource Source => AvailabilitySource.WebHotelier;

    #region Search Operations

    /// <inheritdoc />
    /// <remarks>
    /// Phase 1: Complex search with multi-party handling is in WebHotelierPropertiesService.
    /// This method is not used directly - HotelProviderResolver routes to the service.
    /// </remarks>
    public Task<HotelSearchResult> SearchHotelsAsync(HotelSearchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WebHotelierHotelProvider.SearchHotelsAsync called directly - should route through WebHotelierPropertiesService");
        
        throw new NotImplementedException(
            "SearchHotelsAsync complex logic is in WebHotelierPropertiesService. " +
            "Use HotelProviderResolver which delegates appropriately.");
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AutoCompleteHotel>> SearchPropertiesByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SearchPropertiesByNameAsync for term: {SearchTerm}", searchTerm);
        
        var hotels = await _client.SearchPropertiesAsync(searchTerm, cancellationToken);
        
        return hotels.Select(h => new AutoCompleteHotel(
            h.code,
            Provider.WebHotelier,
            h.name,
            h.location?.name ?? "",
            h.location?.country ?? "",
            h.type));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AutoCompleteHotel>> GetAllPropertiesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetAllPropertiesAsync called");
        
        var hotels = await _client.GetAllPropertiesAsync(cancellationToken);
        
        return hotels.Select(h => new AutoCompleteHotel(
            h.code,
            Provider.WebHotelier,
            h.name,
            h.location?.name ?? "",
            h.location?.country ?? "",
            h.type));
    }

    #endregion

    #region Single Hotel Operations

    /// <inheritdoc />
    /// <remarks>
    /// Phase 1: Complex availability with rate mapping and coupons is in WebHotelierPropertiesService.
    /// This method is not used directly - HotelProviderResolver routes to the service.
    /// </remarks>
    public Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WebHotelierHotelProvider.GetHotelAvailabilityAsync called directly - should route through WebHotelierPropertiesService");
        
        throw new NotImplementedException(
            "GetHotelAvailabilityAsync complex logic is in WebHotelierPropertiesService. " +
            "Use HotelProviderResolver which delegates appropriately.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Phase 1: Hotel info mapping is in WebHotelierPropertiesService.
    /// This method is not used directly - HotelProviderResolver routes to the service.
    /// </remarks>
    public Task<HotelInfoResult> GetHotelInfoAsync(string hotelId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WebHotelierHotelProvider.GetHotelInfoAsync called directly - should route through WebHotelierPropertiesService");
        
        throw new NotImplementedException(
            "GetHotelInfoAsync mapping logic is in WebHotelierPropertiesService. " +
            "Use HotelProviderResolver which delegates appropriately.");
    }

    /// <inheritdoc />
    /// <remarks>
    /// Phase 1: Room info mapping is in WebHotelierPropertiesService.
    /// This method is not used directly - HotelProviderResolver routes to the service.
    /// </remarks>
    public Task<RoomInfoResult> GetRoomInfoAsync(string hotelId, string roomId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WebHotelierHotelProvider.GetRoomInfoAsync called directly - should route through WebHotelierPropertiesService");
        
        throw new NotImplementedException(
            "GetRoomInfoAsync mapping logic is in WebHotelierPropertiesService. " +
            "Use HotelProviderResolver which delegates appropriately.");
    }

    #endregion

    #region Booking Operations

    /// <inheritdoc />
    /// <remarks>
    /// Phase 1: Booking with card processing and email is in WebHotelierPropertiesService.
    /// This method is not used directly - HotelProviderResolver routes to the service.
    /// </remarks>
    public Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("WebHotelierHotelProvider.CreateBookingAsync called directly - should route through WebHotelierPropertiesService");
        
        throw new NotImplementedException(
            "CreateBookingAsync complex logic is in WebHotelierPropertiesService. " +
            "Use HotelProviderResolver which delegates appropriately.");
    }

    /// <inheritdoc />
    public async Task<bool> CancelBookingAsync(int providerReservationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("CancelBookingAsync for reservation: {ReservationId}", providerReservationId);
        
        return await _client.CancelBookingAsync(providerReservationId, cancellationToken);
    }

    #endregion
}
