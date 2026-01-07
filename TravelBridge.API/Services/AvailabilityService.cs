using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Providers;
using TravelBridge.API.Repositories;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.API.Services;

/// <summary>
/// Coordinates availability requests between providers and business logic.
/// Calls provider layer for raw data, then applies business logic (coupons, pricing).
/// </summary>
public class AvailabilityService : IAvailabilityService
{
    private readonly IHotelProviderResolver _providerResolver;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IHotelProviderResolver providerResolver,
        ILogger<AvailabilityService> logger)
    {
        _providerResolver = providerResolver;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SingleAvailabilityResponse> GetHotelAvailabilityAsync(
        string hotelId,
        int providerId,
        DateTime checkIn,
        DateTime checkOut,
        string partyJson,
        ReservationsRepository? reservationsRepository,
        string? couponCode = null)
    {
        _logger.LogDebug("GetHotelAvailabilityAsync started - HotelId: {HotelId}, ProviderId: {ProviderId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}",
            hotelId, providerId, checkIn, checkOut);

        // Verify provider is supported and get the provider instance
        if (!_providerResolver.TryGet(providerId, out var provider))
        {
            _logger.LogWarning("GetHotelAvailabilityAsync: Provider {ProviderId} not supported", providerId);
            throw new NotSupportedException($"Provider {providerId} is not supported.");
        }

        // Parse party JSON to provider-neutral PartyConfiguration
        var partyConfig = ProviderToContractsMapper.ParsePartyConfiguration(partyJson);

        // Build provider-neutral query
        var query = new HotelAvailabilityQuery
        {
            HotelId = hotelId,
            CheckIn = DateOnly.FromDateTime(checkIn),
            CheckOut = DateOnly.FromDateTime(checkOut),
            Party = partyConfig
        };

        // Call provider for raw availability data
        _logger.LogDebug("Calling provider {ProviderId} for availability", providerId);
        var providerResult = await provider.GetHotelAvailabilityAsync(query);

        if (!providerResult.IsSuccess)
        {
            _logger.LogWarning("Provider {ProviderId} returned error: {ErrorCode} - {ErrorMessage}",
                providerId, providerResult.ErrorCode, providerResult.ErrorMessage);
            return new SingleAvailabilityResponse
            {
                ErrorCode = providerResult.ErrorCode,
                ErrorMessage = providerResult.ErrorMessage,
                Data = new SingleHotelAvailabilityInfo { Rooms = [] }
            };
        }

        // Check if no rates were found - fetch alternatives
        var hasRates = providerResult.Data?.Rooms?.Any(r => r.Rates.Count > 0) == true;
        if (!hasRates)
        {
            _logger.LogDebug("No rates found, fetching alternatives for HotelId: {HotelId}", hotelId);
            var alternativesQuery = new AlternativesQuery
            {
                HotelId = hotelId,
                CheckIn = DateOnly.FromDateTime(checkIn),
                CheckOut = DateOnly.FromDateTime(checkOut),
                Party = partyConfig,
                SearchRangeDays = 14
            };

            var alternativesResult = await provider.GetAlternativesAsync(alternativesQuery);
            if (alternativesResult.IsSuccess && alternativesResult.Alternatives.Count > 0)
            {
                _logger.LogDebug("Found {Count} alternatives for HotelId: {HotelId}", 
                    alternativesResult.Alternatives.Count, hotelId);
                
                // Update provider result with alternatives
                providerResult = providerResult with
                {
                    Data = providerResult.Data! with
                    {
                        Alternatives = alternativesResult.Alternatives
                    }
                };
            }
        }

        // Apply coupon logic if provided
        decimal disc = 0;
        CouponType couponType = CouponType.none;
        if (reservationsRepository != null && !string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await reservationsRepository.RetrieveCoupon(couponCode.ToUpper());
            if (coupon != null && coupon.CouponType == CouponType.percentage)
            {
                disc = coupon.Percentage / 100m;
                couponType = CouponType.percentage;
                _logger.LogInformation("Applied percentage coupon - Code: {CouponCode}, Discount: {Discount}%",
                    couponCode, coupon.Percentage);
            }
            else if (coupon != null && coupon.CouponType == CouponType.flat)
            {
                disc = coupon.Amount;
                couponType = CouponType.flat;
                _logger.LogInformation("Applied flat coupon - Code: {CouponCode}, Amount: {Amount}",
                    couponCode, coupon.Amount);
            }
        }

        // Map provider result to SingleAvailabilityResponse using existing pricing logic
        var response = providerResult.MapToResponse(checkIn, disc, couponType, providerId);

        _logger.LogDebug("GetHotelAvailabilityAsync completed - HotelId: {HotelId}, RoomsCount: {RoomsCount}, AlternativesCount: {AlternativesCount}",
            hotelId, response.Data?.Rooms?.Count ?? 0, response.Data?.Alternatives?.Count ?? 0);

        return response;
    }
}
