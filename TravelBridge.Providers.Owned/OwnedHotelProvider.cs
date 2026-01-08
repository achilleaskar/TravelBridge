using Microsoft.Extensions.Logging;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;
using TravelBridge.Providers.Abstractions.Store;

namespace TravelBridge.Providers.Owned;

/// <summary>
/// Hotel provider for owned inventory (ProviderId = 0).
/// Provides hotel availability, information, and search functionality
/// backed by local MySQL database inventory.
/// </summary>
public sealed class OwnedHotelProvider : IHotelProvider
{
    private readonly IOwnedInventoryStore _store;
    private readonly ILogger<OwnedHotelProvider> _logger;

    public OwnedHotelProvider(IOwnedInventoryStore store, ILogger<OwnedHotelProvider> logger)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Provider ID for owned inventory.
    /// </summary>
    public int ProviderId => ProviderIds.Owned; // 0

    /// <summary>
    /// Get hotel information by hotel code.
    /// Composite ID format: "0-{hotelCode}" (e.g., "0-OWNTEST01")
    /// </summary>
    public async Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetHotelInfoAsync started for HotelId: {HotelId}", query.HotelId);
        
        try
        {
            // Hotel ID is the Code (not numeric ID)
            var hotel = await _store.GetHotelByCodeAsync(query.HotelId, ct);
            
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found: {HotelId}", query.HotelId);
                return HotelInfoResult.Failure("NOT_FOUND", "Hotel not found.");
            }

            var hotelInfo = new HotelInfoData
            {
                Code = hotel.Code,              // ✅ Correct property name
                Name = hotel.Name,              // ✅ Correct property name
                Type = hotel.Type,
                Rating = hotel.Rating ?? 0,
                Description = hotel.Description,
                Location = new HotelLocationData
                {
                    Latitude = (double)hotel.Latitude,
                    Longitude = (double)hotel.Longitude,
                    Address = hotel.Address,
                    PostalCode = hotel.PostalCode,
                    City = hotel.City,
                    Country = hotel.Country
                },
                Operation = new HotelOperationData
                {
                    CheckinTime = hotel.CheckInTime,
                    CheckoutTime = hotel.CheckOutTime
                }
            };

            _logger.LogInformation("GetHotelInfoAsync completed for HotelId: {HotelId}", query.HotelId);

            return HotelInfoResult.Success(hotelInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHotelInfoAsync failed for HotelId: {HotelId}", query.HotelId);
            return HotelInfoResult.Failure("ERROR", "An error occurred while retrieving hotel information.");
        }
    }

    /// <summary>
    /// Get room information by hotel code and room code.
    /// </summary>
    public async Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetRoomInfoAsync started for HotelId: {HotelId}, RoomId: {RoomId}", 
            query.HotelId, query.RoomId);
        
        try
        {
            // First get hotel to get its database ID
            var hotel = await _store.GetHotelByCodeAsync(query.HotelId, ct);
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found: {HotelId}", query.HotelId);
                return RoomInfoResult.Failure("NOT_FOUND", "Hotel not found.");
            }

            var roomType = await _store.GetRoomTypeByCodeAsync(hotel.Id, query.RoomId, ct);
            if (roomType == null)
            {
                _logger.LogWarning("Room type not found: {RoomId} in hotel {HotelId}", 
                    query.RoomId, query.HotelId);
                return RoomInfoResult.Failure("NOT_FOUND", "Room not found.");
            }

            var roomInfo = new RoomInfoData
            {
                Name = roomType.Name,               // ✅ Correct property name
                Description = roomType.Description,
                Capacity = new RoomCapacityData
                {
                    MaxAdults = roomType.MaxAdults,
                    MaxPersons = roomType.MaxTotalOccupancy,
                    MinPersons = 1,
                    MaxInfants = roomType.MaxChildren,
                    ChildrenAllowed = roomType.MaxChildren > 0
                }
            };

            _logger.LogInformation("GetRoomInfoAsync completed for HotelId: {HotelId}, RoomId: {RoomId}", 
                query.HotelId, query.RoomId);

            return RoomInfoResult.Success(roomInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetRoomInfoAsync failed for HotelId: {HotelId}, RoomId: {RoomId}", 
                query.HotelId, query.RoomId);
            return RoomInfoResult.Failure("ERROR", "An error occurred while retrieving room information.");
        }
    }

    /// <summary>
    /// Get hotel availability for specified dates and party.
    /// This is the core method for availability checking.
    /// </summary>
    public async Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetHotelAvailabilityAsync started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Rooms: {Rooms}",
            query.HotelId, query.CheckIn, query.CheckOut, query.Party.RoomCount);

        try
        {
            // Fetch hotel by code
            var hotel = await _store.GetHotelByCodeAsync(query.HotelId, ct);
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found: {HotelId}", query.HotelId);
                return HotelAvailabilityResult.Failure("NOT_FOUND", "Hotel not found.");
            }

            // Get all active room types for this hotel
            var roomTypes = await _store.GetRoomTypesByHotelIdAsync(hotel.Id, activeOnly: true, ct);
            if (roomTypes.Count == 0)
            {
                _logger.LogWarning("No active room types found for hotel: {HotelId}", query.HotelId);
                return HotelAvailabilityResult.Success(new HotelAvailabilityData
                {
                    HotelCode = hotel.Code,
                    HotelName = hotel.Name,
                    Location = new AvailabilityLocationData  // ✅ Use AvailabilityLocationData
                    {
                        Latitude = hotel.Latitude,
                        Longitude = hotel.Longitude,
                        Name = hotel.City
                    },
                    Rooms = Array.Empty<AvailableRoomData>(),
                    Alternatives = Array.Empty<AlternativeDateData>()
                });
            }

            // Calculate requested rooms from party
            var requestedRooms = PartyHelpers.GetRequestedRooms(query.Party);
            var nights = query.Nights;

            if (nights <= 0)
            {
                _logger.LogWarning("Invalid dates: CheckOut must be after CheckIn");
                return HotelAvailabilityResult.Failure("INVALID_DATES", "CheckOut must be after CheckIn.");
            }

            // Bulk inventory fetch for all room types (performance optimization)
            var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
            var inventoryByRoomType = await _store.GetInventoryForMultipleRoomTypesAsync(
                roomTypeIds,
                query.CheckIn,
                query.CheckOut,
                ct);

            _logger.LogDebug("Fetched inventory for {Count} room types across {Nights} nights",
                roomTypeIds.Count, nights);

            // Check availability for each room type and build rates
            var availableRooms = new List<AvailableRoomData>();

            foreach (var roomType in roomTypes)
            {
                if (!inventoryByRoomType.TryGetValue(roomType.Id, out var inventoryRows))
                {
                    _logger.LogDebug("No inventory found for room type {RoomTypeId}", roomType.Id);
                    continue;
                }

                // Require complete coverage (one row per night)
                if (inventoryRows.Count != nights)
                {
                    _logger.LogDebug("Incomplete inventory for room type {RoomTypeId}: {RowCount}/{ExpectedNights}",
                        roomType.Id, inventoryRows.Count, nights);
                    continue;
                }

                // Calculate minimum available units across all nights
                var minAvailable = inventoryRows.Min(inv => inv.AvailableUnits);

                if (minAvailable < requestedRooms)
                {
                    _logger.LogDebug("Insufficient availability for room type {RoomTypeId}: {Available}/{Requested}",
                        roomType.Id, minAvailable, requestedRooms);
                    continue;
                }

                // Calculate total price (sum of nightly prices * requested rooms)
                decimal totalPrice = 0;
                foreach (var inv in inventoryRows)
                {
                    var pricePerNight = inv.PricePerNight ?? roomType.BasePricePerNight;
                    totalPrice += pricePerNight;
                }
                totalPrice *= requestedRooms;

                // Build rate ID: rt_{roomTypeId}-{adults}[_{childAges}]
                var rateId = PartyHelpers.BuildRateId(roomType.Id, query.Party);

                // Create rate data
                var rate = new RoomRateData
                {
                    RoomCode = roomType.Code,
                    RateId = rateId,
                    RateName = "Standard",
                    TotalPrice = totalPrice,
                    NetPrice = totalPrice, // Phase 3: no markup logic
                    RemainingRooms = minAvailable,
                    HasCancellation = false, // Phase 3: no cancellation policies
                    SearchParty = new RatePartyInfo
                    {
                        Adults = PartyHelpers.GetAdults(query.Party),
                        ChildrenAges = PartyHelpers.GetChildrenAges(query.Party),
                        RoomsCount = requestedRooms,
                        PartyJson = PartyHelpers.ToPartyJson(query.Party)
                    }
                };

                // Create available room data
                availableRooms.Add(new AvailableRoomData
                {
                    RoomCode = roomType.Code,
                    RoomName = roomType.Name,
                    RoomType = roomType.Code,
                    Rates = new[] { rate }
                });

                _logger.LogDebug("Room type {RoomTypeId} available: {MinAvailable} units, price {TotalPrice}",
                    roomType.Id, minAvailable, totalPrice);
            }

            var result = new HotelAvailabilityData
            {
                HotelCode = hotel.Code,
                HotelName = hotel.Name,
                Location = new AvailabilityLocationData  // ✅ Use AvailabilityLocationData
                {
                    Latitude = hotel.Latitude,
                    Longitude = hotel.Longitude,
                    Name = hotel.City
                },
                Rooms = availableRooms,
                Alternatives = Array.Empty<AlternativeDateData>() // Populated by service layer if no rooms
            };

            _logger.LogInformation("GetHotelAvailabilityAsync completed for HotelId: {HotelId}, AvailableRooms: {Count}",
                query.HotelId, availableRooms.Count);

            return HotelAvailabilityResult.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHotelAvailabilityAsync failed for HotelId: {HotelId}", query.HotelId);
            return HotelAvailabilityResult.Failure("ERROR", "An error occurred while checking availability.");
        }
    }

    /// <summary>
    /// Get alternative available dates when requested dates have no availability.
    /// Scans a window of dates (default 14 days) before and after the requested dates.
    /// </summary>
    public async Task<AlternativesResult> GetAlternativesAsync(AlternativesQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("GetAlternativesAsync started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}",
            query.HotelId, query.CheckIn, query.CheckOut);

        try
        {
            // Fetch hotel by code
            var hotel = await _store.GetHotelByCodeAsync(query.HotelId, ct);
            if (hotel == null)
            {
                _logger.LogWarning("Hotel not found: {HotelId}", query.HotelId);
                return AlternativesResult.Failure("NOT_FOUND", "Hotel not found.");
            }

            // Get all active room types
            var roomTypes = await _store.GetRoomTypesByHotelIdAsync(hotel.Id, activeOnly: true, ct);
            if (roomTypes.Count == 0)
            {
                _logger.LogWarning("No active room types found for hotel: {HotelId}", query.HotelId);
                return AlternativesResult.Success(Array.Empty<AlternativeDateData>());
            }

            var requestedRooms = PartyHelpers.GetRequestedRooms(query.Party);
            var stayLength = query.CheckOut.DayNumber - query.CheckIn.DayNumber;
            var searchRangeDays = 14; // Default 14-day window (Phase 3 fixed value)

            // Build scan window: [checkIn - searchRangeDays, checkOut + searchRangeDays]
            var scanStart = query.CheckIn.AddDays(-searchRangeDays);
            var scanEnd = query.CheckOut.AddDays(searchRangeDays);

            _logger.LogDebug("Scanning alternatives from {ScanStart} to {ScanEnd} for {StayLength}-night stays",
                scanStart, scanEnd, stayLength);

            // Fetch inventory for entire scan window for all room types
            var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
            var inventoryByRoomType = await _store.GetInventoryForMultipleRoomTypesAsync(
                roomTypeIds,
                scanStart,
                scanEnd,
                ct);

            // Find alternative date ranges where ANY room type has availability
            var alternatives = new List<AlternativeDateData>();
            var currentDate = scanStart;

            while (currentDate.AddDays(stayLength) <= scanEnd)
            {
                var potentialCheckOut = currentDate.AddDays(stayLength);

                // Skip the originally requested dates
                if (currentDate == query.CheckIn)
                {
                    currentDate = currentDate.AddDays(1);
                    continue;
                }

                // Check if ANY room type has availability for this date range
                foreach (var roomType in roomTypes)
                {
                    if (!inventoryByRoomType.TryGetValue(roomType.Id, out var allInventory))
                        continue;

                    // Get inventory rows for this specific date range
                    var rangeInventory = allInventory
                        .Where(inv => inv.Date >= currentDate && inv.Date < potentialCheckOut)
                        // .Where(inv => inv.Date >= currentDate && inv.Date < potentialCheckOut && inv.AvailableUnits > 0)
                        .ToList();

                    if (rangeInventory.Count != stayLength)
                        continue; // Incomplete coverage

                    var minAvailable = rangeInventory.Min(inv => inv.AvailableUnits);

                    if (minAvailable >= requestedRooms)
                    {
                        // Calculate price for this alternative
                        decimal totalPrice = 0;
                        foreach (var inv in rangeInventory)
                        {
                            var pricePerNight = inv.PricePerNight ?? roomType.BasePricePerNight;
                            totalPrice += pricePerNight;
                        }
                        totalPrice *= requestedRooms;

                        alternatives.Add(new AlternativeDateData
                        {
                            CheckIn = currentDate,
                            CheckOut = potentialCheckOut,
                            MinPrice = totalPrice,
                            NetPrice = totalPrice
                        });

                        _logger.LogDebug("Alternative found: {CheckIn} to {CheckOut}, price {Price}",
                            currentDate, potentialCheckOut, totalPrice);

                        break; // Found availability for this date range, move to next
                    }
                }

                currentDate = currentDate.AddDays(1);
            }

            // Sort by check-in date
            alternatives = alternatives
                .OrderBy(a => a.CheckIn)
                .ToList();

            _logger.LogInformation("GetAlternativesAsync completed for HotelId: {HotelId}, AlternativesCount: {Count}",
                query.HotelId, alternatives.Count);

            return AlternativesResult.Success(alternatives);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAlternativesAsync failed for HotelId: {HotelId}", query.HotelId);
            return AlternativesResult.Failure("ERROR", "An error occurred while searching for alternatives.");
        }
    }

    /// <summary>
    /// Search for available hotels within a geographic bounding box.
    /// Returns hotels that have availability for the specified dates and party.
    /// </summary>
    public async Task<SearchAvailabilityResult> SearchAvailabilityAsync(SearchAvailabilityQuery query, CancellationToken ct = default)
    {
        _logger.LogInformation("SearchAvailabilityAsync started for BBox: [{MinLat},{MinLon}]-[{MaxLat},{MaxLon}], CheckIn: {CheckIn}, CheckOut: {CheckOut}, Rooms: {Rooms}",
            query.BoundingBox.BottomLeftLatitude, query.BoundingBox.BottomLeftLongitude,
            query.BoundingBox.TopRightLatitude, query.BoundingBox.TopRightLongitude,
            query.CheckIn, query.CheckOut, query.Party.RoomCount);

        try
        {
            // Search hotels within bounding box
            var hotels = await _store.SearchHotelsInBoundingBoxAsync(
                (decimal)query.BoundingBox.BottomLeftLatitude,
                (decimal)query.BoundingBox.TopRightLatitude,
                (decimal)query.BoundingBox.BottomLeftLongitude,
                (decimal)query.BoundingBox.TopRightLongitude,
                activeOnly: true,
                ct);

            if (hotels.Count == 0)
            {
                _logger.LogInformation("No hotels found in bounding box");
                return SearchAvailabilityResult.Success(Array.Empty<HotelSummaryData>());
            }

            _logger.LogDebug("Found {Count} hotels in bounding box", hotels.Count);

            var requestedRooms = PartyHelpers.GetRequestedRooms(query.Party);
            var nights = query.CheckOut.DayNumber - query.CheckIn.DayNumber;

            if (nights <= 0)
            {
                _logger.LogWarning("Invalid dates: CheckOut must be after CheckIn");
                return SearchAvailabilityResult.Failure("INVALID_DATES", "CheckOut must be after CheckIn.");
            }

            // Check availability for each hotel
            var results = new List<HotelSummaryData>();

            foreach (var hotel in hotels)
            {
                try
                {
                    // Get room types for this hotel
                    var roomTypes = hotel.RoomTypes; // Already included from store
                    if (roomTypes.Count == 0)
                        continue;

                    // Fetch inventory for all room types in this hotel
                    var roomTypeIds = roomTypes.Select(rt => rt.Id).ToList();
                    var inventoryByRoomType = await _store.GetInventoryForMultipleRoomTypesAsync(
                        roomTypeIds,
                        query.CheckIn,
                        query.CheckOut,
                        ct);

                    decimal? minPrice = null;
                    var hasAvailability = false;

                    // Find minimum price across all available room types
                    foreach (var roomType in roomTypes)
                    {
                        if (!inventoryByRoomType.TryGetValue(roomType.Id, out var inventoryRows))
                            continue;

                        if (inventoryRows.Count != nights)
                            continue; // Incomplete coverage

                        var minAvailable = inventoryRows.Min(inv => inv.AvailableUnits);
                        if (minAvailable < requestedRooms)
                            continue; // Not enough rooms

                        // Calculate price for this room type
                        decimal totalPrice = 0;
                        foreach (var inv in inventoryRows)
                        {
                            var pricePerNight = inv.PricePerNight ?? roomType.BasePricePerNight;
                            totalPrice += pricePerNight;
                        }
                        totalPrice *= requestedRooms;

                        if (!minPrice.HasValue || totalPrice < minPrice.Value)
                        {
                            minPrice = totalPrice;
                        }

                        hasAvailability = true;
                    }

                    // Only include hotels with availability
                    if (hasAvailability && minPrice.HasValue)
                    {
                        // Calculate distance from center point (haversine formula)
                        var distance = CalculateDistance(
                            query.CenterLatitude,
                            query.CenterLongitude,
                            (double)hotel.Latitude,
                            (double)hotel.Longitude);

                        results.Add(new HotelSummaryData
                        {
                            Code = hotel.Code,  // ✅ Correct
                            Name = hotel.Name,  // ✅ Correct
                            Location = new HotelLocationData
                            {
                                Latitude = (double)hotel.Latitude,
                                Longitude = (double)hotel.Longitude,
                                Address = hotel.Address,
                                City = hotel.City,
                                Country = hotel.Country
                            },
                            MinPrice = minPrice.Value,
                            Distance = (decimal)distance,  // ✅ Cast to decimal
                            Rating = hotel.Rating
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking availability for hotel {HotelCode}", hotel.Code);
                    // Continue with other hotels
                }
            }

            // Sort results based on query sort preferences
            results = SortResults(results, query.SortBy, query.SortOrder);

            _logger.LogInformation("SearchAvailabilityAsync completed, found {Count} hotels with availability",
                results.Count);

            return SearchAvailabilityResult.Success(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchAvailabilityAsync failed");
            return SearchAvailabilityResult.Failure("ERROR", "An error occurred while searching for hotels.");
        }
    }

    /// <summary>
    /// Calculate distance between two points using the Haversine formula.
    /// Returns distance in kilometers.
    /// </summary>
    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    /// <summary>
    /// Sort search results based on specified criteria.
    /// </summary>
    private static List<HotelSummaryData> SortResults(
        List<HotelSummaryData> results,
        string? sortBy,
        string? sortOrder)
    {
        var descending = string.Equals(sortOrder, "DESC", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToUpperInvariant() switch
        {
            "PRICE" => descending
                ? results.OrderByDescending(h => h.MinPrice).ToList()
                : results.OrderBy(h => h.MinPrice).ToList(),

            "DISTANCE" => descending
                ? results.OrderByDescending(h => h.Distance).ToList()
                : results.OrderBy(h => h.Distance).ToList(),

            "RATING" => descending
                ? results.OrderByDescending(h => h.Rating ?? 0).ToList()
                : results.OrderBy(h => h.Rating ?? 0).ToList(),

            _ => // Default: POPULARITY (price + distance weighted)
                results.OrderBy(h => h.Distance).ThenBy(h => h.MinPrice).ToList()
        };
    }
}
