using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Contracts.Plugin.AutoComplete;
using TravelBridge.Contracts.Plugin.Filters;

namespace TravelBridge.Contracts.Providers;

/// <summary>
/// Unified interface for hotel availability providers.
/// Implementations include WebHotelier (external API) and OwnedInventory (internal database).
/// </summary>
public interface IHotelProvider
{
    /// <summary>
    /// Identifies which availability source this provider handles.
    /// </summary>
    AvailabilitySource Source { get; }

    #region Search Operations

    /// <summary>
    /// Search for hotels with availability in a geographic area.
    /// </summary>
    /// <param name="request">Search criteria including dates, location, and party configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Search results with available hotels and filters</returns>
    Task<HotelSearchResult> SearchHotelsAsync(HotelSearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search properties by name for autocomplete functionality.
    /// </summary>
    /// <param name="searchTerm">Partial name to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Matching hotels for autocomplete dropdown</returns>
    Task<IEnumerable<AutoCompleteHotel>> SearchPropertiesByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all properties from this provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>All hotels managed by this provider</returns>
    Task<IEnumerable<AutoCompleteHotel>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Single Hotel Operations

    /// <summary>
    /// Get availability details for a specific hotel.
    /// </summary>
    /// <param name="request">Request with hotel ID, dates, and party configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed availability with rooms and rates</returns>
    Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get static hotel information (description, photos, amenities).
    /// </summary>
    /// <param name="hotelId">Provider-specific hotel identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Hotel details</returns>
    Task<HotelInfoResult> GetHotelInfoAsync(string hotelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get room type information (description, photos, capacity).
    /// </summary>
    /// <param name="hotelId">Provider-specific hotel identifier</param>
    /// <param name="roomId">Room type identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Room type details</returns>
    Task<RoomInfoResult> GetRoomInfoAsync(string hotelId, string roomId, CancellationToken cancellationToken = default);

    #endregion

    #region Booking Operations

    /// <summary>
    /// Create a booking/reservation with the provider.
    /// </summary>
    /// <param name="request">Booking details including customer info and selected rates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Booking confirmation result</returns>
    Task<BookingResult> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an existing booking.
    /// </summary>
    /// <param name="providerReservationId">Provider-specific reservation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancellation was successful</returns>
    Task<bool> CancelBookingAsync(int providerReservationId, CancellationToken cancellationToken = default);

    #endregion
}

#region Request DTOs

/// <summary>
/// Request for searching hotels in a geographic area.
/// </summary>
public record HotelSearchRequest
{
    /// <summary>Check-in date (yyyy-MM-dd format)</summary>
    public required string CheckIn { get; init; }
    
    /// <summary>Check-out date (yyyy-MM-dd format)</summary>
    public required string CheckOut { get; init; }
    
    /// <summary>Party configuration as JSON (e.g., [{"adults":2,"children":[5,10]}])</summary>
    public required string Party { get; init; }
    
    /// <summary>Center latitude for distance calculations</summary>
    public required string Latitude { get; init; }
    
    /// <summary>Center longitude for distance calculations</summary>
    public required string Longitude { get; init; }
    
    /// <summary>Bounding box - bottom left latitude</summary>
    public required string BBoxBottomLeftLat { get; init; }
    
    /// <summary>Bounding box - top right latitude</summary>
    public required string BBoxTopRightLat { get; init; }
    
    /// <summary>Bounding box - bottom left longitude</summary>
    public required string BBoxBottomLeftLon { get; init; }
    
    /// <summary>Bounding box - top right longitude</summary>
    public required string BBoxTopRightLon { get; init; }
    
    /// <summary>Sort field (e.g., "PRICE", "DISTANCE", "POPULARITY")</summary>
    public string SortBy { get; init; } = "POPULARITY";
    
    /// <summary>Sort direction ("ASC" or "DESC")</summary>
    public string SortOrder { get; init; } = "DESC";
}

/// <summary>
/// Request for getting availability of a specific hotel.
/// </summary>
public record HotelAvailabilityRequest
{
    /// <summary>Provider-specific hotel identifier (without provider prefix)</summary>
    public required string HotelId { get; init; }
    
    /// <summary>Check-in date (yyyy-MM-dd format)</summary>
    public required string CheckIn { get; init; }
    
    /// <summary>Check-out date (yyyy-MM-dd format)</summary>
    public required string CheckOut { get; init; }
    
    /// <summary>Party configuration as JSON</summary>
    public required string Party { get; init; }
    
    /// <summary>Optional: specific rates to check (for checkout validation)</summary>
    public List<SelectedRateRequest>? SelectedRates { get; init; }
    
    /// <summary>Optional: coupon code to apply</summary>
    public string? CouponCode { get; init; }
}

/// <summary>
/// Represents a rate selection for availability check or booking.
/// </summary>
public record SelectedRateRequest
{
    public required string RateId { get; init; }
    public required string RoomId { get; init; }
    public required int Count { get; init; }
    public required string SearchParty { get; init; }
}

/// <summary>
/// Request for creating a booking.
/// </summary>
public record CreateBookingRequest
{
    /// <summary>Provider-specific hotel identifier</summary>
    public required string HotelId { get; init; }
    
    /// <summary>Check-in date</summary>
    public required DateOnly CheckIn { get; init; }
    
    /// <summary>Check-out date</summary>
    public required DateOnly CheckOut { get; init; }
    
    /// <summary>Selected rates to book</summary>
    public required List<BookingRateRequest> Rates { get; init; }
    
    /// <summary>Customer information</summary>
    public required BookingCustomerInfo Customer { get; init; }
}

/// <summary>
/// Rate details for booking request.
/// </summary>
public record BookingRateRequest
{
    public required string RateId { get; init; }
    public required int Quantity { get; init; }
    public required decimal NetPrice { get; init; }
    public required string Party { get; init; }
    public required int Adults { get; init; }
    public string? Children { get; init; }
}

/// <summary>
/// Customer information for booking.
/// </summary>
public record BookingCustomerInfo
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Notes { get; init; }
}

#endregion

#region Result DTOs

/// <summary>
/// Result from hotel search operation.
/// </summary>
public record HotelSearchResult
{
    public bool Success { get; init; } = true;
    public string? ErrorMessage { get; init; }
    public IEnumerable<WebHotel> Hotels { get; init; } = [];
    public List<Filter> Filters { get; init; } = [];
}

/// <summary>
/// Result from single hotel availability operation.
/// </summary>
public record HotelAvailabilityResult
{
    public bool Success { get; init; } = true;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public HotelAvailabilityData? Data { get; init; }
    public bool CouponValid { get; init; }
    public string? CouponDiscount { get; init; }
}

/// <summary>
/// Availability data for a single hotel.
/// </summary>
public record HotelAvailabilityData
{
    public required string HotelId { get; init; }
    public Provider Provider { get; init; }
    public Location? Location { get; init; }
    public List<SingleHotelRoom> Rooms { get; init; } = [];
    public List<Alternative> Alternatives { get; init; } = [];
}

/// <summary>
/// Result from hotel info operation.
/// </summary>
public record HotelInfoResult
{
    public bool Success { get; init; } = true;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public HotelData? Data { get; init; }
}

/// <summary>
/// Result from room info operation.
/// </summary>
public record RoomInfoResult
{
    public bool Success { get; init; } = true;
    public int HttpCode { get; init; } = 200;
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public RoomInfo? Data { get; init; }
}

/// <summary>
/// Result from booking operation.
/// </summary>
public record BookingResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    
    /// <summary>Provider-specific reservation IDs (one per rate/room booked)</summary>
    public List<int> ProviderReservationIds { get; init; } = [];
}

#endregion
