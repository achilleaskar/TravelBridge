using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.API.Helpers.Extensions;
using TravelBridge.Geo.Mapbox;
using TravelBridge.Contracts.Plugin.AutoComplete;
using TravelBridge.Contracts.Plugin.Filters;
using TravelBridge.Providers.WebHotelier;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Endpoints
{
    public class SearchPluginEndpoints
    {
        private readonly WebHotelierPropertiesService webHotelierPropertiesService;
        private readonly MapBoxService mapBoxService;
        private readonly ILogger<SearchPluginEndpoints> logger;

        public SearchPluginEndpoints(WebHotelierPropertiesService webHotelierPropertiesService, MapBoxService mapBoxService, ILogger<SearchPluginEndpoints> logger)
        {
            this.webHotelierPropertiesService = webHotelierPropertiesService;
            this.mapBoxService = mapBoxService;
            this.logger = logger;
        }

        public record SubmitSearchParameters
        (
            [FromQuery] string checkin,
            [FromQuery] string checkOut,
            [FromQuery] string bbox,
            [FromQuery] int? adults,
            [FromQuery] string? children,
            [FromQuery] int? rooms,
            [FromQuery] string? party,
            [FromQuery] string searchTerm,
            [FromQuery] int? page,
            [FromQuery] string? sorting,
            [FromQuery] int? minPrice,
            [FromQuery] int? maxPrice,
            [FromQuery] string? hotelTypes,
            [FromQuery] string? boardTypes,
            [FromQuery] string? rating
        );

        public void MapEndpoints(IEndpointRouteBuilder app)
        {
            var apiGroup = app.MapGroup("/api/plugin");

            apiGroup.MapGet("/autocomplete",
            [EndpointSummary("Returns best matching locations that contain the provided search term")]
            async (string? searchQuery) => await GetAutocompleteResults(searchQuery))
                .WithName("GetLocations")
                .WithOpenApi(CustomizeAutoCompleteOperation);

            apiGroup.MapGet("/allproperties",
           [EndpointSummary("Returns best matching locations that contain the provided search term")]
            async (string? type) => await GetAllProperties(type));

            apiGroup.MapGet("/submitSearch",
            [EndpointSummary("Returns best matching locations that contain the provided search term")]
            async ([AsParameters] SubmitSearchParameters pars) =>
            await GetSearchResults(pars))
                .WithName("SubmitSearch")
                .WithOpenApi(CustomizeSearchOperation);
        }

        public static Dictionary<string, List<AutoCompleteHotel>> GroupHotelsByCategory(List<AutoCompleteHotel> hotels)
        {
            if (hotels == null)
            {
                return new Dictionary<string, List<AutoCompleteHotel>>();
            }
            return hotels
                .SelectMany(hotel => hotel.MappedTypes, (hotel, category) => new { category, hotel })
                .GroupBy(x => x.category)
                .ToDictionary(g => g.Key, g => g.Select(x => x.hotel).Distinct().ToList());
        }

        public static Dictionary<string, List<WebHotel>> GroupHotelsByCategory(List<WebHotel> hotels)
        {
            if (hotels == null)
            {
                return new Dictionary<string, List<WebHotel>>();
            }
            return hotels
                .SelectMany(hotel => hotel.MappedTypes, (hotel, category) => new { category, hotel })
                .GroupBy(x => x.category)
                .ToDictionary(g => g.Key, g => g.Select(x => x.hotel).Distinct().ToList());
        }

        private async Task<object> GetAllProperties(string? type)
        {
            var whHotels = await webHotelierPropertiesService.GetAllPropertiesFromWebHotelierAsync();
            var Hotels = whHotels.MapToAutoCompleteHotels().ToList();

            foreach (var h in Hotels)
            {
                h.MappedTypes = h.OriginalType.MapToType();
            }

            var groupedHotels = GroupHotelsByCategory(Hotels);

            var Filters = new List<Filter>
            {
                 new ("Τύποι Καταλυμμάτων","hotelTypes",groupedHotels.Select(h => new FilterValue
                        {
                            Id = h.Key,
                            Name = h.Key,
                            Count = h.Value.Count
                        }).ToList(),false)
            };

            if (string.IsNullOrEmpty(type))
            {
                return new
                {
                    Hotels = Hotels.Take(50),
                    Filters
                };
            }

            List<AutoCompleteHotel>? HotelsOfType = null;

            if (!string.IsNullOrEmpty(type))
            {
                groupedHotels.TryGetValue(type, out HotelsOfType);
            }

            return new
            {
                Hotels = HotelsOfType ?? new(),
                Filters
            };
        }

        private async Task<PluginSearchResponse> GetSearchResults(SubmitSearchParameters pars)
        {
            #region Param Validation

            if (!DateTime.TryParseExact(pars.checkin, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
            {
                throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
            }

            if (!DateTime.TryParseExact(pars.checkOut, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckOut))
            {
                throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
            }

            var location = pars.bbox.Split('-');
            if (location.Length != 3)
            {
                throw new ArgumentException("Invalid bbox format. Use bbox-lat-lon.");
            }

            string party;
            BBox bboxO = TryGetBBox(location[0]);
            if (string.IsNullOrWhiteSpace(pars.party))
            {
                if (pars.rooms != 1)
                {
                    throw new InvalidOperationException("when room greated than 1 party must be used");
                }

                if (pars.adults == null || pars.adults < 1)
                {
                    throw new ArgumentException("There must be at least one adult in the room.");
                }

                party = CreateParty(pars.adults.Value, pars.children);
            }
            else
            {
                party = BuildMultiRoomJson(pars.party);
            }

            #endregion Param Validation

            MultiAvailabilityRequest req = new()
            {
                CheckIn = parsedCheckin.ToString("yyyy-MM-dd"),
                CheckOut = parsedCheckOut.ToString("yyyy-MM-dd"),
                BottomLeftLatitude = bboxO.BottomLeftLatitude,
                TopRightLatitude = bboxO.TopRightLatitude,
                BottomLeftLongitude = bboxO.BottomLeftLongitude,
                TopRightLongitude = bboxO.TopRightLongitude,
                Lat = location[1],
                Lon = location[2],
                Party = party
            };
            //TODO check sorting after merge

            HandleSorting(pars.sorting, req);

            int skip = (pars.page ?? 0) * 20;

            WHAvailabilityRequest whReq = new()
            {
                CheckIn = req.CheckIn,
                CheckOut = req.CheckOut,
                Party = req.Party,
                Lat = req.Lat,
                Lon = req.Lon,
                BottomLeftLatitude = req.BottomLeftLatitude,
                TopRightLatitude = req.TopRightLatitude,
                BottomLeftLongitude = req.BottomLeftLongitude,
                TopRightLongitude = req.TopRightLongitude,
                SortBy = req.SortBy,
                SortOrder = req.SortOrder
            };

            var res = await webHotelierPropertiesService.GetAvailabilityAsync(whReq)
                ?? new PluginSearchResponse
                {
                    Results = new List<WebHotel>(),
                    Filters = new(),
                };

            res.SearchTerm = pars.searchTerm;
            int nights = (parsedCheckOut - parsedCheckin).Days;
            //ApplyPriceFilters(res, pars);
            FillFilters(res, nights);
            ApplyFilters(res, pars);
            SetBoardText(res);
            CalculateAppliedFilters(res);
            SetSelectedFilters(res, pars);

            if (string.IsNullOrWhiteSpace(pars.sorting) || StringToEnum.ParseEnumFromDescription<SortOption>(pars.sorting ?? "") == SortOption.PriceAsc)
                res.Results = res.Results.OrderBy(h => h.MinPrice).ToList();
            else if (StringToEnum.ParseEnumFromDescription<SortOption>(pars.sorting ?? "") == SortOption.PriceDesc)
                res.Results = res.Results.OrderByDescending(h => h.MinPrice).ToList();

            res.ResultsCount = res?.Results?.Count() ?? 0;
            if (res?.ResultsCount > 0)
                foreach (var hotel in res?.Results)
                {
                    hotel.Rates = new List<MultiRate>();
                }

            if (res.Results.IsNullOrEmpty())
            {
                res.Filters = new List<Filter>();
            }
            return res;
        }

        private static void SetBoardText(PluginSearchResponse res)
        {
            foreach (var hotel in res.Results)
            {
                hotel.SetBoardsText();
            }
        }

        private static void SetSelectedFilters(PluginSearchResponse res, SubmitSearchParameters pars)
        {
            foreach (var filter in res.Filters)
            {
                switch (filter.Id)
                {
                    case "hotelTypes":
                        filter.Values.ForEach(v => v.Selected = pars.hotelTypes?.Split(',').Contains(v.Id) == true);
                        break;

                    case "rating":
                        filter.Values.ForEach(v => v.Selected = pars.rating?.Split(',').Contains(v.Id) == true);
                        break;

                    case "boardTypes":
                        filter.Values.ForEach(v => v.Selected = pars.boardTypes?.Split(',').Contains(v.Id) == true);
                        break;
                }
            }
        }

        private static void ApplyPriceFilters(PluginSearchResponse res, SubmitSearchParameters pars)
        {
            var allHotesls = res.Results.AsEnumerable();

            if (pars.minPrice.HasValue)
                allHotesls = allHotesls.Where(h => h.MinPrice >= pars.minPrice.Value);

            if (pars.maxPrice.HasValue)
                allHotesls = allHotesls.Where(h => h.MinPrice <= pars.maxPrice.Value);

            res.Results = allHotesls.ToList();
        }

        private static void ApplyFilters(PluginSearchResponse res, SubmitSearchParameters pars)
        {
            var allHotesls = res.Results.AsEnumerable();

            if (pars.minPrice.HasValue)
                allHotesls = allHotesls.Where(h => h.MinPricePerDay >= pars.minPrice.Value);

            if (pars.maxPrice.HasValue)
                allHotesls = allHotesls.Where(h => h.MinPricePerDay <= pars.maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(pars.hotelTypes))
            {
                var selectedTypes = pars.hotelTypes.ToLower()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                allHotesls = allHotesls.Where(h => h.MappedTypes.Any(t => selectedTypes.Contains(t.ToLower())));
            }

            if (!string.IsNullOrWhiteSpace(pars.boardTypes))
            {
                var selectedTypes = pars.boardTypes.ToLower()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                allHotesls = allHotesls.Where(h => h.Boards.Any(t => selectedTypes.Contains(t.Id.ToString()) || (t.Id == 0 && selectedTypes.Contains("14"))));
            }

            if (!string.IsNullOrWhiteSpace(pars.rating))
            {
                var selectedRatings = pars.rating.ToLower()
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                allHotesls = allHotesls.Where(h => selectedRatings.Contains((h.Rating ?? 0).ToString()));
            }

            res.Results = allHotesls?.ToList() ?? new List<WebHotel>();
        }

        private static void CalculateAppliedFilters(PluginSearchResponse res)
        {
            foreach (var filter in res.Filters.Where(f => f.Type == FilterType.values))
            {
                if (filter.Id.Equals("hotelTypes", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var type in filter.Values)
                    {
                        type.FilteredCount = res.Results.Count(h => h.MappedTypes.Contains(type.Id));
                    }
                }
                else if (filter.Id.Equals("rating", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var type in filter.Values)
                    {
                        type.FilteredCount = res.Results.Count(h => (h.Rating ?? 0).ToString().Equals(type.Id));
                    }
                }
                else if (filter.Id.Equals("boardTypes", StringComparison.InvariantCultureIgnoreCase))
                {
                    foreach (var type in filter.Values)
                    {
                        type.FilteredCount = res.Results.Count(h => h.Boards.Any(b => b.Id.ToString().Equals(type.Id)));
                    }
                }
            }
        }

        private void FillFilters(PluginSearchResponse res, int nights)
        {
            FillPriceFilter(res, nights);
            res.Filters.Add(GetRatings(res));
            res.Filters.Add(GetTypes(res));
            res.Filters.Add(GetBoards(res));
        }

        private static void FillPriceFilter(PluginSearchResponse res, int nights)
        {
            res.Filters ??= new();
            if (nights > 0 && res.Results?.Count() > 0)
                res.Filters.Add(new Filter("Ευρος Τιμής", "price", Math.Floor((res.Results?.Min(h => h.MinPrice) ?? 0) / nights), Math.Floor(res.Results.Max(h => h.MinPrice) / nights ?? 0), true));
        }

        private static Filter GetBoards(PluginSearchResponse res)
        {
            foreach (var hotel in res.Results ?? new List<WebHotel>())
            {
                hotel.Boards = hotel.Rates.MapBoardTypes();
            }

            var boardCounts = (res.Results ?? new List<WebHotel>())
                .SelectMany(hotel => hotel.Boards)
                .GroupBy(board => new { board.Id, board.Name });

            var boards = new Filter("Τύποι Διατροφής", "boardTypes",
                boardCounts.Select(h => new FilterValue
                {
                    Id = h.Key.Id.ToString(),
                    Name = h.Key.Name,
                    Count = h.Count()
                }).ToList(),
                false);

            var fv0 = boards.Values.FirstOrDefault(v => v.Id == "0");
            if (fv0 != null)
            {
                var fv = boards.Values.FirstOrDefault(v => v.Id == "14");
                if (fv != null)
                {
                    fv.Count += fv0.Count;
                    fv.FilteredCount += fv0.FilteredCount;
                    boards.Values.Remove(fv0);
                }
                else
                {
                    fv0.Id = "14";
                    fv0.Name = "Μόνο Δωμάτιο";
                }
            }

            return boards;
        }

        private static Filter GetTypes(PluginSearchResponse res)
        {
            foreach (var item in res.Results ?? new List<WebHotel>())
            {
                item.MappedTypes = item.OriginalType.MapToType();
            }

            var groupedHotels = GroupHotelsByCategory(res.Results?.ToList() ?? new List<WebHotel>());

            return new Filter("Τύποι Καταλυμμάτων", "hotelTypes",
                groupedHotels.Select(h => new FilterValue
                {
                    Id = h.Key,
                    Name = h.Key,
                    Count = h.Value.Count
                }).OrderByDescending(c => c.Count).ToList(),
                false);
        }

        private static Filter GetRatings(PluginSearchResponse res)
        {
            var ratingFilter = new Filter("Αστέρια", "rating", new(), false);

            if (res.Results != null)
                foreach (var rating in res.Results.Where(r => r.Rating != null).GroupBy(r => r.Rating).OrderBy(r => r.Key))
                {
                    var ratingValue = GetRatingName(rating.Key, Language.el);
                    if (ratingValue == null)
                    {
                        continue;
                    }
                    ratingFilter.Values.Add(new FilterValue
                    {
                        Count = rating.Count(),
                        Id = rating.Key?.ToString() ?? "null",
                        Name = ratingValue
                    });
                }

            return ratingFilter;
        }

        private static string? GetRatingName(int? key, Language lang)
        {
            if (lang == Language.el)
            {
                return key switch
                {
                    1 => "1 αστέρι",
                    2 => "2 αστέρια",
                    3 => "3 αστέρια",
                    4 => "4 αστέρια",
                    5 => "5 αστέρια",
                    _ => "Χωρίς αστέρια",
                };
            }
            return null;
        }

        private static void HandleSorting(string? sorting, MultiAvailabilityRequest req)
        {
            var sortOption = StringToEnum.ParseEnumFromDescription<SortOption>(sorting);

            switch (sortOption)
            {
                case SortOption.Popularity:
                    req.SortBy = "POPULARITY";
                    req.SortOrder = "DESC";
                    break;

                case SortOption.Distance:
                    req.SortBy = "DISTANCE";
                    req.SortOrder = "ASC";
                    break;

                case SortOption.PriceAsc:
                    req.SortBy = "PRICE";
                    req.SortOrder = "ASC";
                    break;

                case SortOption.PriceDesc:
                    req.SortBy = "PRICE";
                    req.SortOrder = "DESC";
                    break;

                default:
                    req.SortBy = "POPULARITY";
                    req.SortOrder = "DESC";
                    break;
            }
        }

        private static string CreateParty(int adults, string? children)
        {
            // Check if children is null or empty and build JSON accordingly
            if (string.IsNullOrWhiteSpace(children))
            {
                return $"[{{\"adults\":{adults}}}]";
            }
            else
            {
                return $"[{{\"adults\":{adults},\"children\":[{children}]}}]";
            }
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

        private static BBox TryGetBBox(string locationId)
        {
            // Validate the input format
            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new ArgumentException("Location ID cannot be null or empty.", nameof(locationId));
            }

            // Remove brackets and split by comma
            var parts = locationId.Trim('[', ']').Split(',');

            // Ensure there are exactly 4 parts
            if (parts.Length != 4)
            {
                throw new ArgumentException("Invalid location ID format. Expected format: [lon1,lat1,lon2,lat2]", nameof(locationId));
            }

            // Parse values
            var lon1 = double.Parse(parts[0]);
            var lat1 = double.Parse(parts[1]);
            var lon2 = double.Parse(parts[2]);
            var lat2 = double.Parse(parts[3]);

            // Calculate min/max for bottom-left and top-right
            var bottomLeftLatitude = Math.Min(lat1, lat2).ToString();
            var topRightLatitude = Math.Max(lat1, lat2).ToString();
            var bottomLeftLongitude = Math.Min(lon1, lon2).ToString();
            var topRightLongitude = Math.Max(lon1, lon2).ToString();

            // Return the BBox object
            return new BBox
            {
                BottomLeftLatitude = bottomLeftLatitude,
                TopRightLatitude = topRightLatitude,
                BottomLeftLongitude = bottomLeftLongitude,
                TopRightLongitude = topRightLongitude
            };
        }

        private async Task<AutoCompleteResponse> GetAutocompleteResults(string? searchQuery)
        {
            logger.LogInformation("Autocomplete search query: {searchQuery}", searchQuery);

            if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
            {
                return new AutoCompleteResponse
                {
                    Hotels = [],
                    Locations = []
                };
            }

            var hotelsTask = webHotelierPropertiesService.SearchPropertyFromWebHotelierAsync(searchQuery);
            var locationsTask = mapBoxService.GetLocationsAsync(searchQuery, "el");
            await Task.WhenAll(hotelsTask, locationsTask);

            return new AutoCompleteResponse
            {
                Hotels = hotelsTask.Result.MapToAutoCompleteHotels(),
                Locations = locationsTask.Result
            };
        }

        private static OpenApiOperation CustomizeAutoCompleteOperation(OpenApiOperation operation)
        {
            // Customize the query parameter in Swagger
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var param = operation.Parameters.FirstOrDefault(p => p.Name == "searchQuery");
                if (param != null)
                {
                    param.Description = "The term to search for locations.";
                    if (param.Schema == null)
                    {
                        param.Schema = new OpenApiSchema();
                    }

                    // Set an example value to prefill in Swagger UI
                    param.Example = new Microsoft.OpenApi.Any.OpenApiString("Trikala");

                    param.Required = false; // Optional parameter
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

        private static OpenApiOperation CustomizeSearchOperation(OpenApiOperation operation)
        {
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                // Dynamically extract properties from the record
                var recordProperties = typeof(SubmitSearchParameters).GetProperties();

                foreach (var property in recordProperties)
                {
                    var param = operation.Parameters.FirstOrDefault(p => p.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                    if (param != null)
                    {
                        var queryAttr = property.GetCustomAttribute<FromQueryAttribute>();
                        var paramName = queryAttr?.Name ?? property.Name; // Use query parameter name if provided

                        // Provide descriptions and examples dynamically
                        var details = GetParameterDetails(paramName);

                        param.Description = details.Description;
                        param.Example = new Microsoft.OpenApi.Any.OpenApiString(details.Example?.ToString() ?? "");
                        param.Required = details.Required;
                    }
                }
            }

            // Add response descriptions
            operation.Responses.TryAdd("400", new OpenApiResponse
            {
                Description = "Bad request. One or more parameters are invalid or missing."
            });

            operation.Responses.TryAdd("500", new OpenApiResponse
            {
                Description = "Internal server error. Something went wrong on the server."
            });

            return operation;
        }

        // Helper function for parameter details
        private static (string Description, object Example, bool Required) GetParameterDetails(string paramName)
        {
            var details = new Dictionary<string, (string Description, object Example, bool Required)>
            {
                { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "15/06/2025", true) },
                { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "20/06/2025", true) },
                { "bbox", ("The bbox to search in.", "[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348", true) },
                { "adults", ("The number of adults for the search. (only if 1 room)", 2, false) },
                { "children", ("The ages of children, comma-separated (e.g., '5,10'). (only if 1 room)", "5,10", false) },
                { "rooms", ("The number of rooms required. (only if one room)", 1, false) },
                { "searchTerm", ("Search term (e.g., location name)", "Crete", true) },
                { "page", ("The page", "0", false) },
                { "sorting", ("Sorting Types", "", false) },
                { "party", ("Additional information about the party (required if more than 1 room. always wins).", "[{\"adults\":2,\"children\":[2,6]},{\"adults\":3}]", false) },
                { "minPrice", ("min Price", "", false) },
                { "maxPrice", ("max Price", "", false) },
                { "hotelTypes", ("hotel Types", "", false) },
                { "boardTypes", ("board Types", "", false) },
                { "rating", ("rating", "", false) },
            };

            return details.ContainsKey(paramName) ? details[paramName] : ("No description available.", "N/A", false);
        }

        private static OpenApiOperation CustomizeSearchOperationOld(OpenApiOperation operation)
        {
            // Customize the query parameters in Swagger
            if (operation.Parameters != null && operation.Parameters.Count > 0)
            {
                var parameterDetails = new Dictionary<string, (string Description, object Example, bool Required)>
                {
                    { "checkin", ("The check-in date for the search (format: dd/MM/yyyy).", "15/06/2025", true) },
                    { "checkOut", ("The check-out date for the search (format: dd/MM/yyyy).", "20/06/2025", true) },
                    { "bbox", ("The bbox to search in.", "[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348", true) },
                    { "adults", ("The number of adults for the search. (only if 1 room)", 2, false) },
                    { "children", ("The ages of children, comma-separated (e.g., '5,10'). (only if 1 room)", "5,10", false) },
                    { "rooms", ("The number of rooms required. (only if one room)", 1, false) },
                    { "searchTerm", ("passthrough","Crete", true) },
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