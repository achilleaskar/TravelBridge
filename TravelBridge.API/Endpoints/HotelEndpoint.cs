using System.Globalization;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Repositories;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.Providers.WebHotelier.Models.Hotel;
using TravelBridge.Providers.WebHotelier.Models.Rate;
using TravelBridge.API.Models.WebHotelier;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using TravelBridge.Contracts.Contracts.Responses;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Providers.Abstractions;

namespace TravelBridge.API.Endpoints
{
    public class HotelEndpoint
    {
        private readonly WebHotelierPropertiesService webHotelierPropertiesService;
        private readonly HotelProviderResolver _providerResolver;
        private readonly ILogger<HotelEndpoint> _logger;

        public HotelEndpoint(
            WebHotelierPropertiesService webHotelierPropertiesService, 
            HotelProviderResolver providerResolver,
            ILogger<HotelEndpoint> logger)
        {
            this.webHotelierPropertiesService = webHotelierPropertiesService;
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

        /// <summary>
        /// Parses hotel ID supporting both new format (wh:HOTELID) and legacy format (1-HOTELID).
        /// </summary>
        private CompositeHotelId ParseHotelId(string hotelId)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(hotelId));
            }

            if (!CompositeHotelId.TryParse(hotelId, out var compositeId))
            {
                _logger.LogWarning("Invalid hotelId format: {HotelId}", hotelId);
                throw new ArgumentException($"Invalid hotel ID format: '{hotelId}'. Expected 'wh:HOTELID' or '1-HOTELID'.", nameof(hotelId));
            }

            return compositeId;
        }

        private async Task<HotelInfoResponse> GetHotelInfo(string hotelId)
        {
            _logger.LogInformation("GetHotelInfo started for HotelId: {HotelId}", hotelId);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var compositeId = ParseHotelId(hotelId);
                
                // Currently only WebHotelier is supported
                // Future: Use _providerResolver.GetProvider(compositeId).GetHotelInfoAsync(...)
                if (compositeId.Source != AvailabilitySource.WebHotelier)
                {
                    throw new NotSupportedException($"Provider '{compositeId.Source}' is not yet supported.");
                }

                _logger.LogDebug("Fetching hotel info from WebHotelier for property: {PropertyId}", compositeId.ProviderHotelId);
                var res = await webHotelierPropertiesService.GetHotelInfo(compositeId.ProviderHotelId);
            
                var contractsData = res.Data?.ToContracts();
                if (contractsData != null)
                {
                    contractsData.Provider = Provider.WebHotelier;
                }

                stopwatch.Stop();
                _logger.LogInformation("GetHotelInfo completed for HotelId: {HotelId} in {ElapsedMs}ms, HasData: {HasData}", 
                    hotelId, stopwatch.ElapsedMilliseconds, contractsData != null);

                return new HotelInfoResponse
                {
                    ErrorCode = res.ErrorCode,
                    ErrorMsg = res.ErrorMessage,
                    Data = contractsData
                };
            }
            catch (Exception ex) when (ex is not ArgumentException and not NotSupportedException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetHotelInfo failed for HotelId: {HotelId} after {ElapsedMs}ms", 
                    hotelId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        public async Task<HotelInfoFullResponse> GetHotelFullInfo(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository reservationsRepository)
        {
            _logger.LogInformation("GetHotelFullInfo started for HotelId: {HotelId}, CheckIn: {CheckIn}, CheckOut: {CheckOut}, Adults: {Adults}, Children: {Children}, Rooms: {Rooms}", 
                hotelId, checkin, checkOut, adults, children, rooms);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                #region Params Validation

                var compositeId = ParseHotelId(hotelId);

                // Currently only WebHotelier is supported
                if (compositeId.Source != AvailabilitySource.WebHotelier)
                {
                    throw new NotSupportedException($"Provider '{compositeId.Source}' is not yet supported.");
                }

                if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Invalid checkin date format {CheckIn}", checkin);
                    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
                }

                if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("GetHotelFullInfo failed: Invalid checkout date format {CheckOut}", checkOut);
                    throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
                }

                if (string.IsNullOrWhiteSpace(party))
                {
                    if (rooms != 1)
                    {
                        _logger.LogWarning("GetHotelFullInfo failed: Party required when rooms > 1, Rooms: {Rooms}", rooms);
                        throw new InvalidOperationException("when room greated than 1 party must be used");
                    }

                    if (adults == null || adults < 1)
                    {
                        _logger.LogWarning("GetHotelFullInfo failed: At least one adult required, Adults: {Adults}", adults);
                        throw new ArgumentException("There must be at least one adult in the room.");
                    }

                    party = General.CreateParty(adults.Value, children);
                }
                else
                {
                    party = General.BuildMultiRoomJson(party);
                }

                #endregion Params Validation

                _logger.LogDebug("Fetching availability and hotel info from WebHotelier for HotelId: {HotelId}, Party: {Party}", hotelId, party);

                WHSingleAvailabilityRequest whReq = new()
                {
                    PropertyId = compositeId.ProviderHotelId,
                    CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                    CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                    Party = party
                };

                var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(whReq, parsedCheckin, reservationsRepository);
                var hotelTask = webHotelierPropertiesService.GetHotelInfo(compositeId.ProviderHotelId);
                await Task.WhenAll(availTask, hotelTask);

                SingleAvailabilityResponse? availRes = await availTask;
                WHHotelInfoResponse? hotelRes = await hotelTask;

                if (availRes.Data != null)
                {
                    availRes.Data.Provider = Provider.WebHotelier;
                }

                var hotelData = hotelRes.Data!.ToContracts();
                hotelData.Provider = Provider.WebHotelier;

                int nights = (parsedCheckOut - parsedCheckin).Days;
                decimal salePrice = 0;

                var res = new HotelInfoFullResponse
                {
                    ErrorCode = hotelRes.ErrorCode,
                    ErrorMsg = hotelRes.ErrorMessage,
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

                return res;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidCastException && ex is not InvalidOperationException && ex is not NotSupportedException)
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

        private async Task<RoomInfoResponse> GetRoomInfo(string hotelId, string roomId)
        {
            _logger.LogInformation("GetRoomInfo started for HotelId: {HotelId}, RoomId: {RoomId}", hotelId, roomId);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var compositeId = ParseHotelId(hotelId);

                if (string.IsNullOrWhiteSpace(roomId))
                {
                    _logger.LogWarning("GetRoomInfo failed: Room ID is null or empty");
                    throw new ArgumentException("Room ID cannot be null or empty.", nameof(roomId));
                }

                // Currently only WebHotelier is supported
                if (compositeId.Source != AvailabilitySource.WebHotelier)
                {
                    throw new NotSupportedException($"Provider '{compositeId.Source}' is not yet supported.");
                }

                _logger.LogDebug("Fetching room info from WebHotelier for PropertyId: {PropertyId}, RoomId: {RoomId}", compositeId.ProviderHotelId, roomId);
                var res = await webHotelierPropertiesService.GetRoomInfo(compositeId.ProviderHotelId, roomId);

                stopwatch.Stop();
                _logger.LogInformation("GetRoomInfo completed for HotelId: {HotelId}, RoomId: {RoomId} in {ElapsedMs}ms, HasData: {HasData}", 
                    hotelId, roomId, stopwatch.ElapsedMilliseconds, res.Data != null);

                return new RoomInfoResponse
                {
                    HttpCode = res.HttpCode,
                    ErrorCode = res.ErrorCode,
                    ErrorMessage = res.ErrorMessage,
                    Data = res.Data?.ToContracts()
                };
            }
            catch (Exception ex) when (ex is not ArgumentException and not NotSupportedException)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "GetRoomInfo failed for HotelId: {HotelId}, RoomId: {RoomId} after {ElapsedMs}ms", 
                    hotelId, roomId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }

        private async Task<SingleAvailabilityResponse> GetHotelAvailability(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId, ReservationsRepository reservationsRepository)
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
                    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
                }

                if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
                {
                    _logger.LogWarning("GetHotelAvailability failed: Invalid checkout date format {CheckOut}", checkOut);
                    throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
                }

                var compositeId = ParseHotelId(hotelId);

                // Currently only WebHotelier is supported
                if (compositeId.Source != AvailabilitySource.WebHotelier)
                {
                    throw new NotSupportedException($"Provider '{compositeId.Source}' is not yet supported.");
                }

                if (string.IsNullOrWhiteSpace(party))
                {
                    if (rooms != 1)
                    {
                        _logger.LogWarning("GetHotelAvailability failed: Party required when rooms > 1, Rooms: {Rooms}", rooms);
                        throw new InvalidOperationException("when room greated than 1 party must be used");
                    }

                    if (adults == null || adults < 1)
                    {
                        _logger.LogWarning("GetHotelAvailability failed: At least one adult required, Adults: {Adults}", adults);
                        throw new ArgumentException("There must be at least one adult in the room.");
                    }

                    party = General.CreateParty(adults.Value, children);
                }
                else
                {
                    party = General.BuildMultiRoomJson(party);
                }

                #endregion Params Validation

                _logger.LogDebug("Fetching availability from WebHotelier for HotelId: {HotelId}, Party: {Party}", hotelId, party);

                WHSingleAvailabilityRequest whReq = new()
                {
                    PropertyId = compositeId.ProviderHotelId,
                    CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                    CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                    Party = party
                };

                var res = await webHotelierPropertiesService.GetHotelAvailabilityAsync(whReq, parsedCheckin, reservationsRepository);

                if (res.Data != null)
                {
                    res.Data.Provider = Provider.WebHotelier;
                }

                stopwatch.Stop();
                _logger.LogInformation("GetHotelAvailability completed for HotelId: {HotelId} in {ElapsedMs}ms, RoomsCount: {RoomsCount}", 
                    hotelId, stopwatch.ElapsedMilliseconds, res.Data?.Rooms?.Count() ?? 0);

                return res;
            }
            catch (Exception ex) when (ex is not ArgumentException && ex is not InvalidCastException && ex is not InvalidOperationException && ex is not NotSupportedException)
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
                    param.Description = "The hotel id. Supports both formats: 'wh:VAROSRESID' (new) or '1-VAROSRESID' (legacy)";
                    param.Schema ??= new OpenApiSchema();

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("wh:VAROSRESID");

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
                    param.Description = "The hotel id. Supports both formats: 'wh:VAROSRESID' (new) or '1-VAROSRESID' (legacy)";
                    param.Schema ??= new OpenApiSchema();

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("wh:VAROSRESID");

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
                    { "hotelId", ("The Hotel Id. Supports both formats: 'wh:VAROSVILL' (new) or '1-VAROSVILL' (legacy)","wh:VAROSVILL", true) },
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