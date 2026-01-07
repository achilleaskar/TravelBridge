using Microsoft.Extensions.Logging;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;
using TravelBridge.Providers.WebHotelier.Models.Common;

namespace TravelBridge.Providers.WebHotelier;

/// <summary>
/// WebHotelier implementation of IHotelProvider.
/// Provides hotel information, room details, and availability from WebHotelier API.
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
    public int ProviderId => ProviderIds.WebHotelier;

    /// <inheritdoc />
    public async Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetHotelInfoAsync started for HotelId: {HotelId}", query.HotelId);

        try
        {
            var response = await _client.GetHotelInfoAsync(query.HotelId, cancellationToken);
            
            if (response?.Data != null)
            {
                // Process photos
                response.Data.LargePhotos = response.Data.PhotosItems?.Select(p => p.Large) ?? [];
            }

            var result = WHMappingHelpers.ToHotelInfoResult(response);
            
            _logger.LogDebug("GetHotelInfoAsync completed for HotelId: {HotelId}, Success: {Success}", 
                query.HotelId, result.IsSuccess);
            
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("GetHotelInfoAsync canceled for HotelId: {HotelId}", query.HotelId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetHotelInfoAsync failed for HotelId: {HotelId}", query.HotelId);
            return HotelInfoResult.Failure("HTTP_ERROR", $"Error calling WebHotelier API: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetRoomInfoAsync started for HotelId: {HotelId}, RoomId: {RoomId}", query.HotelId, query.RoomId);

        try
        {
            var response = await _client.GetRoomInfoAsync(query.HotelId, query.RoomId, cancellationToken);
            
            if (response?.Data != null)
            {
                // Process photos
                response.Data.LargePhotos = response.Data.PhotosItems?.Select(p => p.Large) ?? [];
                response.Data.MediumPhotos = response.Data.PhotosItems?.Select(p => p.Medium) ?? [];
            }

            var result = WHMappingHelpers.ToRoomInfoResult(response);
            
            _logger.LogDebug("GetRoomInfoAsync completed for HotelId: {HotelId}, RoomId: {RoomId}, Success: {Success}", 
                query.HotelId, query.RoomId, result.IsSuccess);
            
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("GetRoomInfoAsync canceled for HotelId: {HotelId}, RoomId: {RoomId}", query.HotelId, query.RoomId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetRoomInfoAsync failed for HotelId: {HotelId}, RoomId: {RoomId}", query.HotelId, query.RoomId);
            return RoomInfoResult.Failure("HTTP_ERROR", $"Error calling WebHotelier API: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetHotelAvailabilityAsync started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}", 
            query.HotelId, query.CheckIn, query.CheckOut);

        try
        {
            var partyJson = WHMappingHelpers.ToPartyJson(query.Party);
            var partyItems = WHMappingHelpers.ParsePartyJson(partyJson);
            var groupedParties = WHMappingHelpers.GroupPartyItems(partyItems);

            // Call WebHotelier for each unique party configuration
            var tasks = new Dictionary<WHPartyItem, Task<Models.Responses.WHSingleAvailabilityData?>>();
            foreach (var partyItem in groupedParties)
            {
                tasks.Add(partyItem, _client.GetSingleAvailabilityAsync(
                    query.HotelId,
                    WHMappingHelpers.ToDateString(query.CheckIn),
                    WHMappingHelpers.ToDateString(query.CheckOut),
                    partyItem.party!,
                    cancellationToken));
            }

            await Task.WhenAll(tasks.Values);

            // Collect all rates with their room codes
            var allRates = new List<RoomRateData>();
            var allRooms = new Dictionary<string, AvailableRoomData>();
            
            // Capture hotel metadata from first response
            string? hotelName = null;
            AvailabilityLocationData? hotelLocation = null;

            foreach (var task in tasks)
            {
                var response = await task.Value;
                if (response?.Data?.Rates == null) continue;

                var partyInfo = task.Key;
                
                // Capture hotel metadata once
                if (hotelName == null && response.Data != null)
                {
                    hotelName = response.Data.Name;
                    if (response.Data.Location != null)
                    {
                        hotelLocation = new AvailabilityLocationData
                        {
                            Latitude = response.Data.Location.Latitude,
                            Longitude = response.Data.Location.Longitude,
                            Name = response.Data.Location.Name
                        };
                    }
                }
                
                foreach (var rate in response.Data.Rates)
                {
                    var roomCode = rate.Type;
                    
                    // Build rate ID compatible with FillPartyFromId: {baseId}-{adults}[_{childAge1}_{childAge2}...]
                    // RoomsCount is stored in SearchParty.RoomsCount, not in the ID
                    var partySuffix = $"{partyInfo.adults}" +
                        (partyInfo.children?.Length > 0 ? "_" + string.Join("_", partyInfo.children) : "");
                    var rateId = $"{rate.Id}-{partySuffix}";

                    // Register room if not seen
                    if (!allRooms.ContainsKey(roomCode))
                    {
                        allRooms[roomCode] = new AvailableRoomData
                        {
                            RoomCode = roomCode,
                            RoomName = rate.RoomName,
                            RoomType = rate.Type,
                            Rates = []
                        };
                    }

                    // Add rate with full details for SingleAvailabilityResponse mapping
                    allRates.Add(new RoomRateData
                    {
                        RoomCode = roomCode,
                        RateId = rateId,
                        RateName = rate.RateName,
                        RateDescription = rate.RateDescription,
                        
                        // Legacy totals
                        TotalPrice = rate.Retail?.TotalPrice ?? 0,
                        NetPrice = rate.Pricing?.TotalPrice ?? 0,
                        
                        // Full pricing breakdown
                        Pricing = rate.Pricing != null ? new PricingInfoData
                        {
                            Discount = rate.Pricing.Discount,
                            ExcludedCharges = rate.Pricing.ExcludedCharges,
                            Extras = rate.Pricing.Extras,
                            Margin = rate.Pricing.Margin,
                            StayPrice = rate.Pricing.StayPrice,
                            Taxes = rate.Pricing.Taxes,
                            TotalPrice = rate.Pricing.TotalPrice
                        } : null,
                        Retail = rate.Retail != null ? new PricingInfoData
                        {
                            Discount = rate.Retail.Discount,
                            ExcludedCharges = rate.Retail.ExcludedCharges,
                            Extras = rate.Retail.Extras,
                            Margin = rate.Retail.Margin,
                            StayPrice = rate.Retail.StayPrice,
                            Taxes = rate.Retail.Taxes,
                            TotalPrice = rate.Retail.TotalPrice
                        } : null,
                        
                        RemainingRooms = rate.RemainingRooms ?? 0,
                        BoardTypeId = rate.BoardType,
                        
                        // Cancellation info
                        HasCancellation = rate.CancellationExpiry.HasValue,
                        CancellationDeadline = rate.CancellationExpiry,
                        CancellationPolicy = rate.CancellationPolicy,
                        CancellationPolicyId = rate.CancellationPolicyId,
                        CancellationPenalty = rate.CancellationPenalty,
                        CancellationFees = rate.CancellationFees?.Select(cf => new CancellationFeeData
                        {
                            After = cf.After,
                            Fee = cf.Fee
                        }).ToList() ?? [],
                        
                        // Payment info
                        PaymentPolicy = rate.PaymentPolicy,
                        PaymentPolicyId = rate.PaymentPolicyId,
                        Payments = rate.Payments?.Select(p => new PaymentData
                        {
                            DueDate = p.DueDate,
                            Amount = p.Amount
                        }).ToList() ?? [],
                        
                        // Status
                        Status = rate.Status,
                        StatusDescription = rate.StatusDescription,
                        
                        SearchParty = new RatePartyInfo
                        {
                            Adults = partyInfo.adults,
                            ChildrenAges = partyInfo.children ?? [],
                            RoomsCount = partyInfo.RoomsCount,
                            PartyJson = partyInfo.party
                        }
                    });
                }
            }

            // Group rates by room using dictionary (O(n) instead of O(n²))
            var ratesByRoom = allRates
                .GroupBy(r => r.RoomCode)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rooms = allRooms.Values
                .Select(room => room with
                {
                    Rates = ratesByRoom.TryGetValue(room.RoomCode, out var rr) ? rr : []
                })
                .ToList();

            var result = new HotelAvailabilityResult
            {
                IsSuccess = true,
                Data = new HotelAvailabilityData
                {
                    HotelCode = query.HotelId,
                    HotelName = hotelName,
                    Location = hotelLocation,
                    Rooms = rooms,
                    Alternatives = []
                }
            };

            _logger.LogDebug("GetHotelAvailabilityAsync completed for HotelId: {HotelId}, RoomsCount: {RoomsCount}", 
                query.HotelId, rooms.Count);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("GetHotelAvailabilityAsync canceled for HotelId: {HotelId}", query.HotelId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetHotelAvailabilityAsync failed for HotelId: {HotelId}", query.HotelId);
            return HotelAvailabilityResult.Failure("HTTP_ERROR", $"Error calling WebHotelier API: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<SearchAvailabilityResult> SearchAvailabilityAsync(SearchAvailabilityQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("SearchAvailabilityAsync started - CheckIn: {CheckIn}, CheckOut: {CheckOut}, Center: ({Lat}, {Lon})", 
            query.CheckIn, query.CheckOut, query.CenterLatitude, query.CenterLongitude);

        try
        {
            var partyJson = WHMappingHelpers.ToPartyJson(query.Party);
            var partyItems = WHMappingHelpers.ParsePartyJson(partyJson);
            var groupedParties = WHMappingHelpers.GroupPartyItems(partyItems);

            // Call WebHotelier for each unique party configuration
            var tasks = new Dictionary<WHPartyItem, Task<Models.Responses.WHMultiAvailabilityResponse?>>();
            foreach (var partyItem in groupedParties)
            {
                var whRequest = WHMappingHelpers.ToWHAvailabilityRequest(
                    query, 
                    partyItem.party!, 
                    query.SortBy ?? "POPULARITY", 
                    query.SortOrder ?? "DESC");

                tasks.Add(partyItem, _client.GetAvailabilityAsync(whRequest, partyItem.party!, cancellationToken));
            }

            await Task.WhenAll(tasks.Values);

            // Use accumulator pattern for deterministic totals
            var hotelAccumulator = new Dictionary<string, HotelAccumulator>();

            foreach (var task in tasks)
            {
                var response = await task.Value;
                if (response?.Data?.Hotels == null) continue;

                var partyItem = task.Key;
                
                foreach (var hotel in response.Data.Hotels)
                {
                    // Calculate contribution for this party configuration
                    var contributionMin = (hotel.MinPrice ?? 0m) * partyItem.RoomsCount;
                    var contributionPerNight = (hotel.MinPricePerDay ?? 0m) * partyItem.RoomsCount;
                    var contributionSale = (hotel.SalePrice ?? 0m) * partyItem.RoomsCount;

                    // Map rates for this party - RateId format compatible with FillPartyFromId
                    var partyRates = hotel.Rates?.Select(rate => new HotelRateSummary
                    {
                        RateId = $"{rate.Type}-{partyItem.adults}" +
                            (partyItem.children?.Length > 0 ? "_" + string.Join("_", partyItem.children) : ""),
                        TotalPrice = rate.Retail ?? 0,
                        NetPrice = rate.Price ?? 0,
                        BoardTypeId = rate.BoardType,
                        SearchParty = new RatePartyInfo
                        {
                            Adults = partyItem.adults,
                            ChildrenAges = partyItem.children ?? [],
                            RoomsCount = partyItem.RoomsCount,
                            PartyJson = partyItem.party
                        }
                    }).ToList() ?? [];

                    if (!hotelAccumulator.TryGetValue(hotel.Code, out var acc))
                    {
                        // First time seeing this hotel - create base data
                        acc = new HotelAccumulator
                        {
                            Code = hotel.Code,
                            Name = hotel.Name,
                            Rating = hotel.Rating,
                            PhotoMedium = hotel.PhotoM,
                            PhotoLarge = hotel.PhotoL,
                            Distance = hotel.Distance,
                            Location = hotel.Location,
                            OriginalType = hotel.OriginalType,
                            MinPrice = 0m,
                            MinPricePerNight = 0m,
                            SalePrice = 0m,
                            Rates = []
                        };
                    }

                    // Accumulate totals
                    acc.MinPrice += contributionMin;
                    acc.MinPricePerNight += contributionPerNight;
                    acc.SalePrice += contributionSale;
                    acc.Rates.AddRange(partyRates);

                    hotelAccumulator[hotel.Code] = acc;
                }
            }

            // Convert accumulators to HotelSummaryData
            var hotels = hotelAccumulator.Values.Select(acc => new HotelSummaryData
            {
                Code = acc.Code,
                Name = acc.Name,
                Rating = acc.Rating,
                MinPrice = acc.MinPrice,
                MinPricePerNight = acc.MinPricePerNight,
                SalePrice = acc.SalePrice > acc.MinPrice ? acc.SalePrice : null,
                PhotoMedium = acc.PhotoMedium,
                PhotoLarge = acc.PhotoLarge,
                Distance = acc.Distance,
                Location = acc.Location != null ? new HotelLocationData
                {
                    Latitude = (double)(acc.Location.Latitude ?? 0),
                    Longitude = (double)(acc.Location.Longitude ?? 0)
                } : null,
                OriginalType = acc.OriginalType,
                Rates = acc.Rates
            }).ToList();

            var result = SearchAvailabilityResult.Success(hotels);

            _logger.LogDebug("SearchAvailabilityAsync completed - HotelsCount: {HotelsCount}", result.Hotels.Count);

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("SearchAvailabilityAsync canceled");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "SearchAvailabilityAsync failed");
            return SearchAvailabilityResult.Failure("HTTP_ERROR", $"Error calling WebHotelier API: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<AlternativesResult> GetAlternativesAsync(AlternativesQuery query, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GetAlternativesAsync started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}",
            query.HotelId, query.CheckIn, query.CheckOut);

        try
        {
            var partyJson = WHMappingHelpers.ToPartyJson(query.Party);
            var partyItems = WHMappingHelpers.ParsePartyJson(partyJson);
            var groupedParties = WHMappingHelpers.GroupPartyItems(partyItems);

            // Calculate date range for alternatives search
            var from = query.CheckIn.ToDateTime(TimeOnly.MinValue).AddDays(-query.SearchRangeDays);
            var to = query.CheckOut.ToDateTime(TimeOnly.MinValue).AddDays(query.SearchRangeDays);

            if (from < DateTime.Today.AddDays(1))
            {
                from = DateTime.Today.AddDays(1);
            }

            // Call WebHotelier for each unique party configuration
            var tasks = new Dictionary<WHPartyItem, Task<Models.Responses.WHAlternativeDaysData?>>();
            foreach (var partyItem in groupedParties)
            {
                tasks.Add(partyItem, _client.GetFlexibleCalendarAsync(
                    query.HotelId,
                    partyItem.party!,
                    from,
                    to,
                    cancellationToken));
            }

            await Task.WhenAll(tasks.Values);

            // Process alternatives for each party configuration
            var alterDatesDict = new Dictionary<WHPartyItem, List<AlternativeDateData>>();
            var requestedNights = query.CheckOut.DayNumber - query.CheckIn.DayNumber;

            foreach (var task in tasks.OrderBy(p => p.Key.adults))
            {
                var response = await task.Value;
                if (response?.Data?.days == null || response.Data.days.Count == 0)
                {
                    alterDatesDict.Add(task.Key, []);
                    continue;
                }

                var availableDays = response.Data.days.Where(d => d.status == "AVL" || d.status == "MIN").ToList();
                var alternatives = GetAlterDates(availableDays, requestedNights);
                
                // Apply RoomsCount weighting (important for duplicate rooms in party)
                // e.g., 2 identical rooms at 100€ each = 200€ total
                if (task.Key.RoomsCount > 1)
                {
                    alternatives = alternatives
                        .Select(a => a with
                        {
                            MinPrice = a.MinPrice * task.Key.RoomsCount,
                            NetPrice = a.NetPrice * task.Key.RoomsCount
                        })
                        .ToList();
                }
                
                alterDatesDict.Add(task.Key, alternatives);
            }

            // Keep only common alternatives across all party configurations
            var commonAlternatives = KeepCommon(alterDatesDict);

            _logger.LogDebug("GetAlternativesAsync completed for HotelId: {HotelId}, found {Count} alternatives",
                query.HotelId, commonAlternatives.Count);

            return AlternativesResult.Success(commonAlternatives);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("GetAlternativesAsync canceled for HotelId: {HotelId}", query.HotelId);
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "GetAlternativesAsync failed for HotelId: {HotelId}", query.HotelId);
            return AlternativesResult.Failure("HTTP_ERROR", $"Error calling WebHotelier API: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets alternative dates from available days.
    /// </summary>
    private static List<AlternativeDateData> GetAlterDates(List<Models.Responses.WHAlternativeDayInfo> alterDates, int requestedNights)
    {
        if (alterDates.Count == 0)
        {
            return [];
        }

        var results = new List<AlternativeDateData>();

        // Parse dates
        foreach (var dat in alterDates)
        {
            dat.dateOnly = DateTime.ParseExact(dat.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        }

        var maxDay = alterDates.Max(a => a.dateOnly).AddDays(1);

        for (int i = 0; i < alterDates.Count; i++)
        {
            var curDate = alterDates[i];
            var duration = requestedNights;

            if (curDate.min_stay > requestedNights)
            {
                duration = curDate.min_stay;
            }

            if (curDate.dateOnly.AddDays(duration) > maxDay)
            {
                continue; // Skip if duration exceeds available dates
            }

            decimal minPrice = 0;
            decimal netPrice = 0;
            var tempDate = curDate.dateOnly;
            var tempItem = curDate;
            bool isOk = true;

            for (int j = 0; j < duration; j++)
            {
                if (tempItem == null || tempItem.min_stay > duration || tempItem.dateOnly != tempDate)
                {
                    isOk = false;
                    break;
                }
                minPrice += tempItem.retail;
                netPrice += tempItem.price;
                tempItem = (i + j + 1) < alterDates.Count ? alterDates[i + j + 1] : null;
                tempDate = tempDate.AddDays(1);
            }

            if (isOk)
            {
                results.Add(new AlternativeDateData
                {
                    CheckIn = DateOnly.FromDateTime(curDate.dateOnly),
                    CheckOut = DateOnly.FromDateTime(curDate.dateOnly.AddDays(duration)),
                    Nights = duration,
                    MinPrice = minPrice,
                    NetPrice = netPrice
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Keeps only alternatives that are common across all party configurations.
    /// </summary>
    private static List<AlternativeDateData> KeepCommon(Dictionary<WHPartyItem, List<AlternativeDateData>> alterDatesDict)
    {
        if (alterDatesDict.Count == 0 || alterDatesDict.Values.Any(v => v.Count == 0))
        {
            return [];
        }

        var allCheckInOutPairs = alterDatesDict.Values
            .Select(list => list
                .Select(a => (a.CheckIn, a.CheckOut))
                .Distinct()
                .ToHashSet())
            .ToList();

        // Find common (CheckIn, CheckOut) pairs in all lists
        var commonPairs = allCheckInOutPairs
            .Skip(1)
            .Aggregate(new HashSet<(DateOnly, DateOnly)>(allCheckInOutPairs.First()),
                       (h, s) => { h.IntersectWith(s); return h; });

        // Filter and flatten all matching alternatives
        var matchingAlternatives = alterDatesDict.Values
            .SelectMany(list => list)
            .Where(a => commonPairs.Contains((a.CheckIn, a.CheckOut)));

        // Group by date and sum prices
        return matchingAlternatives
            .GroupBy(a => new { a.CheckIn, a.CheckOut })
            .Select(g => new AlternativeDateData
            {
                CheckIn = g.Key.CheckIn,
                CheckOut = g.Key.CheckOut,
                MinPrice = g.Sum(x => x.MinPrice),
                NetPrice = g.Sum(x => x.NetPrice),
                Nights = g.Key.CheckOut.DayNumber - g.Key.CheckIn.DayNumber
            })
            .OrderBy(a => a.CheckIn)
            .ToList();
    }

    /// <summary>
    /// Internal accumulator for merging hotel data from multiple party configurations.
    /// </summary>
    private class HotelAccumulator
    {
        public required string Code { get; init; }
        public required string Name { get; init; }
        public int? Rating { get; init; }
        public string? PhotoMedium { get; init; }
        public string? PhotoLarge { get; init; }
        public decimal? Distance { get; init; }
        public Models.Common.WHLocation? Location { get; init; }
        public string? OriginalType { get; init; }
        public decimal MinPrice { get; set; }
        public decimal MinPricePerNight { get; set; }
        public decimal SalePrice { get; set; }
        public List<HotelRateSummary> Rates { get; init; } = [];
    }
}
