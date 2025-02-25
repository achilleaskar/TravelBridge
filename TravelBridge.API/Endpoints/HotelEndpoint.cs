using System.Globalization;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.API.Services.WebHotelier;

namespace TravelBridge.API.Endpoints
{
    public class HotelEndpoint
    {
        private readonly WebHotelierPropertiesService webHotelierPropertiesService;

        public HotelEndpoint(WebHotelierPropertiesService webHotelierPropertiesService)
        {
            this.webHotelierPropertiesService = webHotelierPropertiesService;
        }

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/hotel");

            apiGroup.MapGet("/hotelInfo",
           [EndpointSummary("Returns info of the selected hotel")]
            (string HotelId) =>
           GetHotelInfo(HotelId))
               .WithName("HotelInfo")
               .WithOpenApi(CustomizeGetHotelInfoOperation);

            apiGroup.MapGet("/roomInfo",
                      [EndpointSummary("Returns info of the selected hotel")]
            (string HotelId, string RoomId) =>
                      GetRoomInfo(HotelId, RoomId))
                          .WithName("RoomInfo")
                          .WithOpenApi(CustomizeGetRoomInfoOperation);

            apiGroup.MapGet("/hotelRoomAvailability",
           [EndpointSummary("Returns availability of the selected hotel")]
            (string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId) =>
           GetHotelAvailability(checkin, checkOut, adults, children, rooms, party, hotelId))
               .WithName("HotelRoomAvailability")
               .WithOpenApi(CustomizeGetHotelAvailabilityOperation);

            apiGroup.MapGet("/HotelFullInfo",
           [EndpointSummary("Returns full info for the selected hotel")]
            (string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId) =>
           GetHotelFullInfo(checkin, checkOut, adults, children, rooms, party, hotelId))
               .WithName("HotelFullInfo")
               .WithOpenApi(CustomizeGetHotelAvailabilityOperation);
        }

        private async Task<HotelInfoResponse> GetHotelInfo(string hotelId)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(hotelId));
            }

            var hotelInfo = hotelId.Split('-');
            if (hotelInfo.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            var res = await webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
            res.Data.Provider = Models.Provider.WebHotelier;
            return res;
        }

        public async Task<HotelInfoFullResponse> GetHotelFullInfo(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId)
        {
            #region Params Validation


            if (string.IsNullOrWhiteSpace(hotelId))
            {
                throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(hotelId));
            }

            var hotelInfo = hotelId.Split('-');
            if (hotelInfo.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            if (string.IsNullOrWhiteSpace(party))
            {
                if (rooms != 1)
                {
                    throw new InvalidOperationException("when room greated than 1 party must be used");
                }

                if (adults == null || adults < 1)
                {
                    throw new ArgumentException("There must be at least one adult in the room.");
                }

                party = General.CreateParty(adults.Value, children);
            }
            else
            {
                party = BuildMultiRoomJson(party);
            }
            #endregion

            SingleAvailabilityRequest availReq = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                Party = party,
                PropertyId = hotelInfo[1]
            };

            var availTask = webHotelierPropertiesService.GetHotelAvailabilityAsync(availReq, parsedCheckOut);
            var hotelTask = webHotelierPropertiesService.GetHotelInfo(hotelInfo[1]);
            Task.WaitAll(availTask, hotelTask);

            SingleAvailabilityResponse? availRes = await availTask;
            HotelInfoResponse? hotelRes = await hotelTask;

            if (availRes.Data != null)
            {
                availRes.Data.Provider = Models.Provider.WebHotelier;
            }

            hotelRes.Data.Provider = Models.Provider.WebHotelier;

            int nights = (parsedCheckOut - parsedCheckin).Days;

            decimal salePrice = 0;

            var res = new HotelInfoFullResponse
            {
                ErrorCode = hotelRes.ErrorCode,
                ErrorMsg = hotelRes.ErrorMsg,
                HotelData = hotelRes.Data,
                Rooms = availRes.Data?.Rooms ?? [],
            };
            res.HotelData.CustomInfo = GetHotelBasicInfo(availRes, hotelRes);
            res.HotelData.MinPrice = Math.Floor(availRes.Data?.GetMinPrice(out salePrice) ?? 0);
            res.HotelData.SalePrice = salePrice;
            res.HotelData.MinPricePerNight = Math.Floor(res.HotelData.MinPrice / nights);
            res.HotelData.MappedTypes = res.HotelData.Type.MapToType();
            res.HotelData.Boards = res.Rooms.SelectMany(a => a.Rates).MapBoardTypes();
            res.HotelData.SetBoardText();

            return res;
        }

        private string GetHotelBasicInfo(SingleAvailabilityResponse availRes, HotelInfoResponse hotelRes)
        {
            string response = GenerateHtml(hotelRes.Data.Operation);
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


        private async Task<RoomInfoRespone> GetRoomInfo(string hotelId, string roomId)
        {
            if (string.IsNullOrWhiteSpace(hotelId))
            {
                throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(hotelId));
            }

            if (string.IsNullOrWhiteSpace(roomId))
            {
                throw new ArgumentException("Room ID cannot be null or empty.", nameof(roomId));
            }

            var hotelInfo = hotelId.Split('-');
            if (hotelInfo.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            var res = await webHotelierPropertiesService.GetRoomInfo(hotelInfo[1], roomId);
            return res;
        }

        private async Task<SingleAvailabilityResponse> GetHotelAvailability(string checkin, string checkOut, int? adults, string? children, int? rooms, string? party, string hotelId)
        {
            #region Params Validation

            if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            var hotelInfo = hotelId.Split('-');
            if (hotelInfo.Length != 2)
            {
                throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
            }

            if (string.IsNullOrWhiteSpace(party))
            {
                if (rooms != 1)
                {
                    throw new InvalidOperationException("when room greated than 1 party must be used");
                }

                if (adults == null || adults < 1)
                {
                    throw new ArgumentException("There must be at least one adult in the room.");
                }

                party = General.CreateParty(adults.Value, children);
            }
            else
            {
                party = BuildMultiRoomJson(party);
            }
            #endregion

            SingleAvailabilityRequest req = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                Party = party,
                PropertyId = hotelInfo[1]
            };

            var res = await webHotelierPropertiesService.GetHotelAvailabilityAsync(req, parsedCheckOut);
            if (res.Data != null)
            {
                res.Data.Provider = Models.Provider.WebHotelier;
            }

            return res;
        }

        // Method for multiple rooms
        public static string BuildMultiRoomJson(string party)
        {
            // Validate and return the party JSON
            try
            {
                // Attempt to parse to ensure the input is valid JSON
                JsonSerializer.Deserialize<List<Dictionary<string, object>>>(party);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid party data format. Ensure it's valid JSON.", ex);
            }

            return party;
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
                    { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "08/06/2025", true) },
                    { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "10/06/2025", true) },
                    { "adults", ("The number of adults for the search. (only if 1 room)", 2, false) },
                    { "children", ("The ages of children, comma-separated (e.g., '5,10'). (only if 1 room)", "", false) },
                    { "rooms", ("The number of rooms required. (only if one room)", 1, false) },
                    { "hotelId", ("The Hotel Id","1-VAROSVILL", true) },
                    { "party", ("Additional information about the party (required if more than 1 room. always wins).", "[{\"adults\":2,\"childrens\":[2,6]},{\"adults\":3}]", false) }
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