using System.Globalization;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Providers;
using TravelBridge.API.Repositories;
using TravelBridge.API.Services;
using TravelBridge.Contracts.Contracts.Responses;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.API.Endpoints
{
    public class HotelEndpoint
    {
        private readonly IAvailabilityService _availabilityService;
        private readonly IHotelProviderResolver _providerResolver;
        private readonly ILogger<HotelEndpoint> _logger;

        public HotelEndpoint(
            IAvailabilityService availabilityService, 
            IHotelProviderResolver providerResolver,
            ILogger<HotelEndpoint> logger)
        {
            _availabilityService = availabilityService;
            _providerResolver = providerResolver;
            _logger = logger;
        }

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/hotel");

            apiGroup.MapGet("/hotelInfo",
           [EndpointSummary("Returns info of the selected hotel")]
            async (string HotelId) =>
            await GetHotelInfo(HotelId))
               .WithName("HotelInfo")
               .WithOpenApi(CustomizeGetHotelInfoOperation);

            apiGroup.MapGet("/roomInfo",
                      [EndpointSummary("Returns info of the selected room")]
            async (string HotelId, string RoomId) =>
            await GetRoomInfo(HotelId, RoomId))
                          .WithName("RoomInfo")
                          .WithOpenApi(CustomizeGetRoomInfoOperation);

            apiGroup.MapGet("/hotelRoomAvailability",
           [EndpointSummary("Returns availability of the selected hotel")]
            async (string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository repo) =>
            await GetHotelAvailability(checkin, checkOut, adults, children, rooms, party, hotelId, repo))
               .WithName("HotelRoomAvailability")
               .WithOpenApi(CustomizeGetHotelAvailabilityOperation);

            apiGroup.MapGet("/HotelFullInfo",
           [EndpointSummary("Returns full info for the selected hotel")]
            async (string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository reservationsRepository) =>
           await GetHotelFullInfo(checkin, checkOut, adults, children, rooms, party, hotelId, reservationsRepository))
               .WithName("HotelFullInfo")
               .WithOpenApi(CustomizeGetHotelAvailabilityOperation);
        }

        private async Task<IResult> GetHotelInfo(string hotelId)
        {
            _logger.LogInformation("GetHotelInfo started for HotelId: {HotelId}", hotelId);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    _logger.LogWarning("GetHotelInfo failed: Hotel ID is null or empty");
                    return Results.BadRequest(new { error = "Hotel ID cannot be null or empty." });
                }

                if (!CompositeId.TryParse(hotelId, out var compositeId))
                {
                    _logger.LogWarning("GetHotelInfo failed: Invalid hotelId format {HotelId}", hotelId);
                    return Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
                }

                // Resolve provider - return 400 if not supported
                if (!_providerResolver.TryGet(compositeId.ProviderId, out var provider))
                {
                    _logger.LogWarning("GetHotelInfo failed: Provider {ProviderId} not supported for HotelId: {HotelId}", 
                        compositeId.ProviderId, hotelId);
                    return Results.BadRequest(new { error = $"Hotel provider '{compositeId.ProviderId}' is not supported." });
                }

                _logger.LogDebug("Fetching hotel info via provider {ProviderId} for property: {PropertyId}", 
                    compositeId.ProviderId, compositeId.Value);

                // Call provider
                var query = new HotelInfoQuery { HotelId = compositeId.Value };
                var result = await provider.GetHotelInfoAsync(query);

                if (!result.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning("GetHotelInfo failed for HotelId: {HotelId}, ErrorCode: {ErrorCode}, ErrorMessage: {ErrorMessage}", 
                        hotelId, result.ErrorCode, result.ErrorMessage);
                    return Results.Ok(new HotelInfoResponse
                    {
                        ErrorCode = result.ErrorCode,
                        ErrorMsg = result.ErrorMessage,
                        Data = null
                    });
                }

                // Map provider result to Contracts
                var contractsData = ProviderToContractsMapper.ToHotelData(result.Data!, compositeId.ProviderId);

                stopwatch.Stop();
                _logger.LogInformation("GetHotelInfo completed for HotelId: {HotelId} in {ElapsedMs}ms, HasData: {HasData}", 
                    hotelId, stopwatch.ElapsedMilliseconds, contractsData != null);

                return Results.Ok(new HotelInfoResponse
                {
                    ErrorCode = null,
                    ErrorMsg = null,
                    Data = contractsData
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetHotelInfo failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    hotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<IResult> GetHotelFullInfo(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository reservationsRepository)
        {
            _logger.LogInformation("GetHotelFullInfo started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Adults: {Adults}, Children: {Children}, Rooms: {Rooms}", 
                hotelId, checkin, checkOut, adults, children, rooms);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Params Validation

                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Hotel ID is null or empty");
                    return Results.BadRequest(new { error = "Hotel ID cannot be null or empty." });
                }

                if (!CompositeId.TryParse(hotelId, out var compositeId))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Invalid hotelId format {HotelId}", hotelId);
                    return Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
                }

                // Resolve provider - return 400 if not supported
                if (!_providerResolver.TryGet(compositeId.ProviderId, out var provider))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Provider {ProviderId} not supported for HotelId: {HotelId}", 
                        compositeId.ProviderId, hotelId);
                    return Results.BadRequest(new { error = $"Hotel provider '{compositeId.ProviderId}' is not supported." });
                }

                if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Invalid checkin date format {CheckIn}", checkin);
                    return Results.BadRequest(new { error = "Invalid checkin date format. Use dd/MM/yyyy." });
                }

                if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Invalid checkout date format {CheckOut}", checkOut);
                    return Results.BadRequest(new { error = "Invalid checkout date format. Use dd/MM/yyyy." });
                }

                if (string.IsNullOrWhiteSpace(party))
                {
                    if (rooms != 1)
                    {
                        _logger.LogWarning("GetHotelFullInfo failed: Party required when rooms > 1, Rooms: {Rooms}", rooms);
                        return Results.BadRequest(new { error = "When rooms > 1, party must be used." });
                    }

                    if (adults == null || adults < 1)
                    {
                        _logger.LogWarning("GetHotelFullInfo failed: At least one adult required, Adults: {Adults}", adults);
                        return Results.BadRequest(new { error = "There must be at least one adult in the room." });
                    }

                    party = General.CreateParty(adults.Value, children);
                }
                else
                {
                    party = General.BuildMultiRoomJson(party);
                }

                #endregion Params Validation

                _logger.LogDebug("Fetching availability and hotel info via provider {ProviderId} for HotelId: {HotelId}, Party: {Party}", 
                    compositeId.ProviderId, hotelId, party);

                // Call provider for hotel info and availability service in parallel
                var hotelInfoQuery = new HotelInfoQuery { HotelId = compositeId.Value };
                var hotelInfoTask = provider.GetHotelInfoAsync(hotelInfoQuery);

                var availTask = _availabilityService.GetHotelAvailabilityAsync(
                    compositeId.Value,
                    compositeId.ProviderId,
                    parsedCheckin,
                    parsedCheckOut,
                    party,
                    reservationsRepository);
                
                await Task.WhenAll(hotelInfoTask, availTask);

                var hotelInfoResult = await hotelInfoTask;
                var availRes = await availTask;

                if (!hotelInfoResult.IsSuccess)
                {
                    stopwatch.Stop();
                    return Results.Ok(new HotelInfoFullResponse
                    {
                        ErrorCode = hotelInfoResult.ErrorCode,
                        ErrorMsg = hotelInfoResult.ErrorMessage,
                        HotelData = null!,
                        Rooms = [],
                        Alternatives = []
                    });
                }

                var hotelData = ProviderToContractsMapper.ToHotelData(hotelInfoResult.Data!, compositeId.ProviderId);

                int nights = (parsedCheckOut - parsedCheckin).Days;
                decimal salePrice = 0;

                var res = new HotelInfoFullResponse
                {
                    ErrorCode = null,
                    ErrorMsg = null,
                    HotelData = hotelData,
                    Rooms = availRes.Data?.Rooms ?? [],
                    Alternatives = availRes.Data?.Alternatives ?? []
                };
                res.HotelData.CustomInfo = GetHotelBasicInfo(availRes, hotelData);
                res.HotelData.MinPrice = Math.Floor(availRes.Data?.GetMinPrice(out salePrice) ?? 0);
                res.HotelData.SalePrice = salePrice;
                res.HotelData.MinPricePerNight = Math.Floor(res.HotelData.MinPrice / nights);
                res.HotelData.MappedTypes = res.HotelData.Type.MapToType();
                res.HotelData.Boards = res.Rooms.SelectMany(a => a.Rates).MapBoardTypes();
                res.HotelData.SetBoardText();

                stopwatch.Stop();
                _logger.LogInformation("GetHotelFullInfo completed for HotelId: {HotelId} in {ElapsedMs}ms, RoomsCount: {RoomsCount}, MinPrice: {MinPrice}", 
                    hotelId, stopwatch.ElapsedMilliseconds, res.Rooms.Count(), res.HotelData.MinPrice);

                return Results.Ok(res);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetHotelFullInfo failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    hotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private string GetHotelBasicInfo(SingleAvailabilityResponse availRes, HotelData hotelData)
        {
            string response = GenerateHtml(hotelData.Operation);
            return response;
        }

        public string GenerateHtml(HotelOperation operation)
        {
            return $@"
                  <ul style='list-style-type: none; padding: 0;'>
                    <li><strong>Check-in Time:</strong> {operation.CheckinTime}</li>
                    <li><strong>Check-out Time:</strong> {operation.CheckoutTime}</li>
                  </ul>";
        }

        private async Task<IResult> GetRoomInfo(string hotelId, string roomId)
        {
            _logger.LogInformation("GetRoomInfo started for HotelId: {HotelId}, RoomId: {RoomId}", hotelId, roomId);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (string.IsNullOrWhiteSpace(hotelId))
                {
                    _logger.LogWarning("GetRoomInfo failed: Hotel ID is null or empty");
                    return Results.BadRequest(new { error = "Hotel ID cannot be null or empty." });
                }

                if (string.IsNullOrWhiteSpace(roomId))
                {
                    _logger.LogWarning("GetRoomInfo failed: Room ID is null or empty");
                    return Results.BadRequest(new { error = "Room ID cannot be null or empty." });
                }

                if (!CompositeId.TryParse(hotelId, out var compositeId))
                {
                    _logger.LogWarning("GetRoomInfo failed: Invalid hotelId format {HotelId}", hotelId);
                    return Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
                }

                // Resolve provider - return 400 if not supported
                if (!_providerResolver.TryGet(compositeId.ProviderId, out var provider))
                {
                    _logger.LogWarning("GetRoomInfo failed: Provider {ProviderId} not supported for HotelId: {HotelId}", 
                        compositeId.ProviderId, hotelId);
                    return Results.BadRequest(new { error = $"Hotel provider '{compositeId.ProviderId}' is not supported." });
                }

                _logger.LogDebug("Fetching room info via provider {ProviderId} for PropertyId: {PropertyId}, RoomId: {RoomId}", 
                    compositeId.ProviderId, compositeId.Value, roomId);

                // Call provider
                var query = new RoomInfoQuery { HotelId = compositeId.Value, RoomId = roomId };
                var result = await provider.GetRoomInfoAsync(query);

                if (!result.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning("GetRoomInfo failed for HotelId: {HotelId}, RoomId: {RoomId}, ErrorCode: {ErrorCode}", 
                        hotelId, roomId, result.ErrorCode);
                    return Results.Ok(new RoomInfoResponse
                    {
                        HttpCode = 200,
                        ErrorCode = result.ErrorCode,
                        ErrorMessage = result.ErrorMessage,
                        Data = null
                    });
                }

                // Map provider result to Contracts
                var contractsData = ProviderToContractsMapper.ToRoomInfo(result.Data!);

                stopwatch.Stop();
                _logger.LogInformation("GetRoomInfo completed for HotelId: {HotelId}, RoomId: {RoomId} in {ElapsedMs}ms, HasData: {HasData}", 
                    hotelId, roomId, stopwatch.ElapsedMilliseconds, contractsData != null);

                return Results.Ok(new RoomInfoResponse
                {
                    HttpCode = 200,
                    ErrorCode = null,
                    ErrorMessage = null,
                    Data = contractsData
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetRoomInfo failed for HotelId: {HotelId}, RoomId: {RoomId} after {ElapsedMs}ms", 
                    hotelId, roomId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<IResult> GetHotelAvailability(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository reservationsRepository)
        {
            _logger.LogInformation("GetHotelAvailability started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Adults: {Adults}, Children: {Children}, Rooms: {Rooms}", 
                hotelId, checkin, checkOut, adults, children, rooms);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Params Validation

                if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("GetHotelAvailability failed: Invalid checkin date format {CheckIn}", checkin);
                    return Results.BadRequest(new { error = "Invalid checkin date format. Use dd/MM/yyyy." });
                }

                if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("GetHotelAvailability failed: Invalid checkout date format {CheckOut}", checkOut);
                    return Results.BadRequest(new { error = "Invalid checkout date format. Use dd/MM/yyyy." });
                }

                if (!CompositeId.TryParse(hotelId, out var compositeId))
                {
                    _logger.LogWarning("GetHotelAvailability failed: Invalid hotelId format {HotelId}", hotelId);
                    return Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
                }

                // Resolve provider - return 400 if not supported
                if (!_providerResolver.TryGet(compositeId.ProviderId, out var provider))
                {
                    _logger.LogWarning("GetHotelAvailability failed: Provider {ProviderId} not supported for HotelId: {HotelId}", 
                        compositeId.ProviderId, hotelId);
                    return Results.BadRequest(new { error = $"Hotel provider '{compositeId.ProviderId}' is not supported." });
                }

                if (string.IsNullOrWhiteSpace(party))
                {
                    if (rooms != 1)
                    {
                        _logger.LogWarning("GetHotelAvailability failed: Party required when rooms > 1, Rooms: {Rooms}", rooms);
                        return Results.BadRequest(new { error = "When rooms > 1, party must be used." });
                    }

                    if (adults == null || adults < 1)
                    {
                        _logger.LogWarning("GetHotelAvailability failed: At least one adult required, Adults: {Adults}", adults);
                        return Results.BadRequest(new { error = "There must be at least one adult in the room." });
                    }

                    party = General.CreateParty(adults.Value, children);
                }
                else
                {
                    party = General.BuildMultiRoomJson(party);
                }

                #endregion Params Validation

                _logger.LogDebug("Fetching availability via provider {ProviderId} for HotelId: {HotelId}, Party: {Party}", 
                    compositeId.ProviderId, hotelId, party);

                // Use provider-neutral availability service
                var res = await _availabilityService.GetHotelAvailabilityAsync(
                    compositeId.Value,
                    compositeId.ProviderId,
                    parsedCheckin,
                    parsedCheckOut,
                    party,
                    reservationsRepository);

                stopwatch.Stop();
                _logger.LogInformation("GetHotelAvailability completed for HotelId: {HotelId} in {ElapsedMs}ms, RoomsCount: {RoomsCount}", 
                    hotelId, stopwatch.ElapsedMilliseconds, res.Data?.Rooms?.Count() ?? 0);

                return Results.Ok(res);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetHotelAvailability failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    hotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private static OpenApiOperation CustomizeGetHotelInfoOperation(OpenApiOperation operation)
        {
            // Customize the query parameter in Swagger
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var param = operation.Parameters.FirstOrDefault(p => p.Name == "HotelId");
                if (param != null)
                {
                    param.Description = "The hotel id you need the info for";
                    param.Schema ??= new OpenApiSchema();

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("1-VAROSRESID");

                    param.Required = true;
                }
            }

            operation.Responses.Add("400", new OpenApiResponse
            {
                Description = "Bad request. The search query is invalid or missing."
            });

            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Internal server error. Something went wrong on the server."
            });
            return operation;
        }

        private static OpenApiOperation CustomizeGetRoomInfoOperation(OpenApiOperation operation)
        {
            // Customize the query parameter in Swagger
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var param = operation.Parameters.FirstOrDefault(p => p.Name == "HotelId");
                if (param != null)
                {
                    param.Description = "The hotel id you need the info for";
                    param.Schema ??= new OpenApiSchema();

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("1-VAROSRESID");

                    param.Required = true;
                }
                param = operation.Parameters.FirstOrDefault(p => p.Name == "RoomId");
                if (param != null)
                {
                    param.Description = "The room id you need the info for";
                    param.Schema ??= new OpenApiSchema();

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("LVLSTD");

                    param.Required = true;
                }
            }

            operation.Responses.Add("400", new OpenApiResponse
            {
                Description = "Bad request. The search query is invalid or missing."
            });

            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Internal server error. Something went wrong on the server."
            });
            return operation;
        }

        private static OpenApiOperation CustomizeGetHotelAvailabilityOperation(OpenApiOperation operation)
        {
            // Customize the query parameters in Swagger
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var parameterDetails = new Dictionary<string, (string Description, object Example, bool Required)>
                {
                    { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "15/06/2026", true) },
                    { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "20/06/2026", true) },
                    { "adults", ("The number of adults for the search. (only if 1 room)", 2, false) },
                    { "children", ("The ages of children, comma-separated (e.g., '5,10'). (only if 1 room)", "", false) },
                    { "rooms", ("The number of rooms required. (only if one room)", 1, false) },
                    { "hotelId", ("The Hotel Id","1-VAROSVILL", true) },
                    { "party", ("Additional information about the party (required if more than 1 room. always wins).", "[{\"adults\":2,\"children\":[2,6]},{\"adults\":3}]", false) }
                };

                foreach (var paramName in parameterDetails.Keys)
                {
                    var param = operation.Parameters.FirstOrDefault(p => p.Name == paramName);
                    if (param != null)
                    {
                        var details = parameterDetails[paramName];
                        param.Description = details.Description;
                        if (param.Schema == null)
                        {
                            param.Schema = new OpenApiSchema();
                        }

                        param.Example = new Microsoft.OpenApi.Any.OpenApiString(details.Example.ToString());
                        param.Required = details.Required;
                    }
                }
            }

            // Add response descriptions
            operation.Responses.Add("400", new OpenApiResponse
            {
                Description = "Bad request. One or more parameters are invalid or missing."
            });

            operation.Responses.Add("500", new OpenApiResponse
            {
                Description = "Internal server error. Something went wrong on the server."
            });

            return operation;
        }
    }
}