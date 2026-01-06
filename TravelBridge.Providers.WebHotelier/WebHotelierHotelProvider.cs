using Microsoft.Extensions.Logging;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Queries;
using TravelBridge.Providers.Abstractions.Results;
using TravelBridge.Providers.WebHotelier.Models.Hotel;
using TravelBridge.Providers.WebHotelier.Models.Rate;
using TravelBridge.Providers.WebHotelier.Models.Responses;

namespace TravelBridge.Providers.WebHotelier;

/// <summary>
/// IHotelProvider implementation for WebHotelier.
/// Wraps <see cref="WebHotelierClient"/> and maps between Abstractions types and WebHotelier wire types.
/// 
/// This provider handles all WebHotelier API communication and translates:
/// - Abstractions queries → WebHotelier requests
/// - WebHotelier responses → Abstractions results
/// </summary>
public class WebHotelierHotelProvider : IHotelProvider
{
    private readonly WebHotelierClient _client;
    private readonly ILogger<WebHotelierHotelProvider> _logger;

    public WebHotelierHotelProvider(WebHotelierClient client, ILogger<WebHotelierHotelProvider> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public AvailabilitySource Source => AvailabilitySource.WebHotelier;

    #region Search Operations

    /// <inheritdoc />
    public async Task<HotelSearchResult> SearchHotelsAsync(HotelSearchQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("SearchHotelsAsync started - CheckIn: {CheckIn}, CheckOut: {CheckOut}", 
            query.CheckIn, query.CheckOut);

        try
        {
            var results = new List<HotelSearchItem>();
            var nights = query.CheckOut.DayNumber - query.CheckIn.DayNumber;

            // Execute search for each party configuration
            foreach (var party in query.Parties)
            {
                var partyJson = party.ToJsonString();
                var request = MapToWHAvailabilityRequest(query, partyJson);
                
                var response = await _client.GetAvailabilityAsync(request, $"[{partyJson}]", ct);
                
                if (response?.Data?.Hotels != null)
                {
                    foreach (var hotel in response.Data.Hotels)
                    {
                        var existingHotel = results.FirstOrDefault(h => h.Code == hotel.Code);
                        if (existingHotel == null)
                        {
                            results.Add(MapToHotelSearchItem(hotel, nights));
                        }
                        // TODO: Merge rates for multi-party searches
                    }
                }
            }

            _logger.LogDebug("SearchHotelsAsync completed - Found {Count} hotels", results.Count);

            return HotelSearchResult.Success(results);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "SearchHotelsAsync failed - HTTP error");
            return HotelSearchResult.Failure("HTTP_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchHotelsAsync failed - Unexpected error");
            return HotelSearchResult.Failure("ERROR", "An unexpected error occurred");
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HotelSummary>> SearchPropertiesAsync(string searchTerm, CancellationToken ct = default)
    {
        _logger.LogDebug("SearchPropertiesAsync started - SearchTerm: {SearchTerm}", searchTerm);

        try
        {
            var hotels = await _client.SearchPropertiesAsync(searchTerm, ct);
            var results = hotels.Select(MapToHotelSummary).ToList();

            _logger.LogDebug("SearchPropertiesAsync completed - Found {Count} properties", results.Count);

            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "SearchPropertiesAsync failed - HTTP error");
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<HotelSummary>> GetAllPropertiesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("GetAllPropertiesAsync started");

        try
        {
            var hotels = await _client.GetAllPropertiesAsync(ct);
            var results = hotels.Select(MapToHotelSummary).ToList();

            _logger.LogDebug("GetAllPropertiesAsync completed - Found {Count} properties", results.Count);

            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetAllPropertiesAsync failed - HTTP error");
            return [];
        }
    }

    #endregion

    #region Single Hotel Operations

    /// <inheritdoc />
    public async Task<HotelAvailabilityResult> GetAvailabilityAsync(AvailabilityQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("GetAvailabilityAsync started - HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}", 
            query.ProviderHotelId, query.CheckIn, query.CheckOut);

        try
        {
            var rooms = new List<RoomAvailability>();
            var checkIn = query.CheckIn.ToString("yyyy-MM-dd");
            var checkOut = query.CheckOut.ToString("yyyy-MM-dd");

            // Execute search for each party configuration
            foreach (var party in query.Parties)
            {
                var partyJson = $"[{party.ToJsonString()}]";
                
                var response = await _client.GetSingleAvailabilityAsync(
                    query.ProviderHotelId, 
                    checkIn, 
                    checkOut, 
                    partyJson, 
                    ct);

                if (response?.Data?.Rates != null)
                {
                    MergeRatesIntoRooms(rooms, response.Data.Rates, party);
                }
            }

            // Get alternatives if no availability
            var alternatives = new List<AlternativeDate>();
            if (rooms.Count == 0 || rooms.All(r => r.Rates.Count == 0))
            {
                alternatives = await GetAlternativesAsync(query, ct);
            }

            _logger.LogDebug("GetAvailabilityAsync completed - Rooms: {RoomCount}, Alternatives: {AltCount}", 
                rooms.Count, alternatives.Count);

            return new HotelAvailabilityResult
            {
                Source = AvailabilitySource.WebHotelier,
                HotelCode = query.ProviderHotelId,
                Rooms = rooms,
                Alternatives = alternatives
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetAvailabilityAsync failed - HTTP error for hotel {HotelId}", query.ProviderHotelId);
            return HotelAvailabilityResult.Failure("HTTP_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvailabilityAsync failed - Unexpected error for hotel {HotelId}", query.ProviderHotelId);
            return HotelAvailabilityResult.Failure("ERROR", "An unexpected error occurred");
        }
    }

    /// <inheritdoc />
    public async Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("GetHotelInfoAsync started - HotelId: {HotelId}", query.ProviderHotelId);

        try
        {
            var response = await _client.GetHotelInfoAsync(query.ProviderHotelId, ct);

            if (response?.Data == null)
            {
                return HotelInfoResult.Failure("NOT_FOUND", "Hotel not found");
            }

            var result = MapToHotelInfoResult(response.Data);
            
            _logger.LogDebug("GetHotelInfoAsync completed - HotelName: {HotelName}", result.Name);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetHotelInfoAsync failed - HTTP error for hotel {HotelId}", query.ProviderHotelId);
            return HotelInfoResult.Failure("HTTP_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHotelInfoAsync failed - Unexpected error for hotel {HotelId}", query.ProviderHotelId);
            return HotelInfoResult.Failure("ERROR", "An unexpected error occurred");
        }
    }

    /// <inheritdoc />
    public async Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default)
    {
        _logger.LogDebug("GetRoomInfoAsync started - HotelId: {HotelId}, RoomId: {RoomId}", 
            query.ProviderHotelId, query.RoomId);

        try
        {
            var response = await _client.GetRoomInfoAsync(query.ProviderHotelId, query.RoomId, ct);

            if (response?.Data == null)
            {
                return RoomInfoResult.Failure("NOT_FOUND", "Room not found");
            }

            var result = MapToRoomInfoResult(response.Data, query.RoomId);
            
            _logger.LogDebug("GetRoomInfoAsync completed - RoomName: {RoomName}", result.Name);

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetRoomInfoAsync failed - HTTP error for room {RoomId}", query.RoomId);
            return RoomInfoResult.Failure("HTTP_ERROR", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRoomInfoAsync failed - Unexpected error for room {RoomId}", query.RoomId);
            return RoomInfoResult.Failure("ERROR", "An unexpected error occurred");
        }
    }

    #endregion

    #region Mapping Methods

    private static WHAvailabilityRequest MapToWHAvailabilityRequest(HotelSearchQuery query, string partyJson)
    {
        return new WHAvailabilityRequest
        {
            CheckIn = query.CheckIn.ToString("yyyy-MM-dd"),
            CheckOut = query.CheckOut.ToString("yyyy-MM-dd"),
            Party = partyJson,
            Lat = query.CenterLatitude,
            Lon = query.CenterLongitude,
            BottomLeftLatitude = query.Location.BottomLeftLatitude,
            BottomLeftLongitude = query.Location.BottomLeftLongitude,
            TopRightLatitude = query.Location.TopRightLatitude,
            TopRightLongitude = query.Location.TopRightLongitude,
            SortBy = query.SortBy ?? "POPULARITY",
            SortOrder = query.SortOrder ?? "DESC"
        };
    }

    private static HotelSearchItem MapToHotelSearchItem(WHWebHotel hotel, int nights)
    {
        var compositeId = CompositeHotelId.ForWebHotelier(hotel.Code);

        return new HotelSearchItem
        {
            Id = compositeId.ToString(),
            Code = hotel.Code,
            Name = hotel.Name,
            Rating = hotel.Rating,
            Type = hotel.OriginalType,
            MinPrice = hotel.MinPrice ?? 0,
            MinPricePerNight = nights > 0 ? (hotel.MinPrice ?? 0) / nights : 0,
            SalePrice = hotel.SalePrice,
            Distance = hotel.Distance,
            PhotoMedium = hotel.PhotoM,
            PhotoLarge = hotel.PhotoL,
            Location = new HotelLocation
            {
                Latitude = (decimal)(hotel.Location?.Latitude ?? 0),
                Longitude = (decimal)(hotel.Location?.Longitude ?? 0)
            },
            Source = AvailabilitySource.WebHotelier
        };
    }

    private static HotelSummary MapToHotelSummary(WHHotel hotel)
    {
        var compositeId = CompositeHotelId.ForWebHotelier(hotel.code);

        return new HotelSummary
        {
            Id = compositeId.ToString(),
            Code = hotel.code,
            Name = hotel.name,
            Type = hotel.type,
            City = hotel.location?.name,
            Country = hotel.location?.country,
            Source = AvailabilitySource.WebHotelier
        };
    }

    private static HotelInfoResult MapToHotelInfoResult(WHHotelData data)
    {
        return new HotelInfoResult
        {
            Source = AvailabilitySource.WebHotelier,
            Code = data.Code,
            Name = data.Name,
            Description = data.Description,
            Rating = data.Rating,
            Type = data.Type,
            Address = data.Location?.Address,
            City = data.Location?.Name, // WHLocationInfo uses Name for city
            Country = data.Location?.Country,
            PostalCode = data.Location?.Zip,
            Location = new HotelLocation
            {
                Latitude = (decimal)(data.Location?.Latitude ?? 0),
                Longitude = (decimal)(data.Location?.Longitude ?? 0)
            },
            CheckInTime = data.Operation?.CheckinTime,
            CheckOutTime = data.Operation?.CheckoutTime,
            Photos = data.LargePhotos?.ToList() ?? [],
            Amenities = data.Facilities?.ToList() ?? []
        };
    }

    private static RoomInfoResult MapToRoomInfoResult(Models.Room.WHRoomInfo data, string roomCode)
    {
        var childrenAllowed = data.Capacity?.ChildrenAllowed ?? false;
        return new RoomInfoResult
        {
            Source = AvailabilitySource.WebHotelier,
            Code = roomCode,
            Name = data.Name,
            Description = data.Description,
            MaxAdults = data.Capacity?.MaxAdults ?? 2,
            MaxChildren = childrenAllowed ? (data.Capacity?.MaxPersons ?? 2) - (data.Capacity?.MaxAdults ?? 2) : 0,
            MaxOccupancy = data.Capacity?.MaxPersons ?? 2,
            Photos = data.LargePhotos?.ToList() ?? [],
            Amenities = data.Amenities ?? []
        };
    }

    private static void MergeRatesIntoRooms(List<RoomAvailability> rooms, List<WHHotelRate> rates, PartyConfiguration party)
    {
        foreach (var rate in rates)
        {
            // Group by room type (Type field contains room type code)
            var room = rooms.FirstOrDefault(r => r.Code == rate.Type);
            if (room == null)
            {
                room = new RoomAvailability
                {
                    Code = rate.Type,
                    Name = rate.RoomName ?? rate.Type
                };
                rooms.Add(room);
            }

            room.Rates.Add(new RoomRate
            {
                Id = rate.Id,
                Name = rate.RateName ?? "",
                BoardCode = rate.BoardType?.ToString() ?? "14",
                BoardName = GetBoardName(rate.BoardType),
                TotalPrice = rate.Pricing?.TotalPrice ?? 0,
                NetPrice = rate.Pricing?.StayPrice ?? 0,
                RetailPrice = rate.Retail?.TotalPrice,
                RemainingRooms = rate.RemainingRooms,
                IsRefundable = rate.CancellationPolicyId != 1, // Policy 1 is typically non-refundable
                CancellationPolicy = rate.CancellationPolicy,
                SearchPartyJson = $"[{party.ToJsonString()}]"
            });
        }
    }

    private static string GetBoardName(int? boardType)
    {
        return boardType switch
        {
            1 => "All Inclusive",
            2 => "Full Board",
            3 => "Half Board",
            4 => "Bed & Breakfast",
            14 => "Room Only",
            _ => "Room Only"
        };
    }

    private async Task<List<AlternativeDate>> GetAlternativesAsync(AvailabilityQuery query, CancellationToken ct)
    {
        try
        {
            var startDate = query.CheckIn.ToDateTime(TimeOnly.MinValue).AddDays(-14);
            var endDate = query.CheckOut.ToDateTime(TimeOnly.MinValue).AddDays(14);

            if (startDate < DateTime.Today.AddDays(1))
                startDate = DateTime.Today.AddDays(1);

            var party = query.Parties.FirstOrDefault() ?? PartyConfiguration.AdultsOnly(2);
            var partyJson = $"[{party.ToJsonString()}]";

            var response = await _client.GetFlexibleCalendarAsync(
                query.ProviderHotelId,
                partyJson,
                startDate,
                endDate,
                ct);

            if (response?.Data?.days == null)
                return [];

            // TODO: Implement full alternative date calculation logic
            // The full logic involves finding contiguous available date ranges
            // For now, return empty - the existing WebHotelierPropertiesService handles this
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get alternatives for hotel {HotelId}", query.ProviderHotelId);
            return [];
        }
    }

    #endregion
}
