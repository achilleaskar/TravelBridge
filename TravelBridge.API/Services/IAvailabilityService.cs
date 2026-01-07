using TravelBridge.API.Contracts;
using TravelBridge.API.Repositories;

namespace TravelBridge.API.Services;

/// <summary>
/// Provider-neutral availability service interface.
/// Coordinates between providers and applies business logic (coupons, alternatives, pricing).
/// </summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Gets hotel availability with all business logic applied.
    /// </summary>
    /// <param name="hotelId">Provider-specific hotel ID (without prefix).</param>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="checkIn">Check-in date.</param>
    /// <param name="checkOut">Check-out date.</param>
    /// <param name="partyJson">Party configuration JSON.</param>
    /// <param name="reservationsRepository">Repository for coupon lookup.</param>
    /// <param name="couponCode">Optional coupon code.</param>
    /// <returns>Availability response with pricing and alternatives.</returns>
    Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(
        string hotelId,
        int providerId,
        DateTime checkIn,
        DateTime checkOut,
        string partyJson,
        ReservationsRepository? reservationsRepository,
        string? couponCode = null);
}
