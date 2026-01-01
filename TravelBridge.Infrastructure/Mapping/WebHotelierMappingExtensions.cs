using TravelBridge.Contracts.Responses;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Contracts;
using TravelBridge.Infrastructure.Integrations.WebHotelier.Models;
using ContractPartyInfo = TravelBridge.Contracts.Responses.PartyInfo;

namespace TravelBridge.Infrastructure.Mapping
{
    /// <summary>
    /// Extension methods to map WebHotelier models to Contracts DTOs.
    /// </summary>
    public static class WebHotelierMappingExtensions
    {
        /// <summary>
        /// Maps WHHotelAvailability to SearchHotelResult.
        /// </summary>
        public static SearchHotelResult ToSearchResult(this WHHotelAvailability hotel)
        {
            return new SearchHotelResult
            {
                Id = hotel.Id,
                Code = hotel.Code,
                Name = hotel.Name,
                Type = hotel.Type,
                Rating = hotel.Rating,
                PhotoUrl = hotel.PhotoL,
                Location = hotel.Location?.ToSearchLocation(),
                MinPrice = hotel.MinPrice,
                MinPricePerDay = hotel.MinPricePerDay,
                SalePrice = hotel.SalePrice,
                MappedTypes = MapHotelType(hotel.Type),
                Boards = hotel.Rates.SelectMany(r => r.Board != null ? new[] { r.Board.ToBoardInfo() } : [])
                    .DistinctBy(b => b.Id)
                    .ToList()
            };
        }

        /// <summary>
        /// Maps WHLocation to SearchLocation.
        /// </summary>
        public static SearchLocation ToSearchLocation(this WHLocation location)
        {
            return new SearchLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Name = location.Name,
                Country = location.Country
            };
        }

        /// <summary>
        /// Maps WHBoard to BoardInfo.
        /// </summary>
        public static BoardInfo ToBoardInfo(this WHBoard board)
        {
            return new BoardInfo
            {
                Id = board.Id,
                Name = board.Name
            };
        }

        /// <summary>
        /// Maps WHHotelData to HotelInfo.
        /// </summary>
        public static HotelInfo ToHotelInfo(this WHHotelData hotel)
        {
            return new HotelInfo
            {
                Id = hotel.Id,
                Code = hotel.Code,
                Name = hotel.Name,
                Description = hotel.Description,
                Type = hotel.Type,
                Rating = hotel.Rating,
                Location = hotel.Location?.ToHotelLocation(),
                Operation = hotel.Operation?.ToHotelOperation(),
                Photos = hotel.LargePhotos?.ToList() ?? [],
                Facilities = hotel.Facilities?.ToList() ?? [],
                MappedTypes = MapHotelType(hotel.Type)
            };
        }

        /// <summary>
        /// Maps WHLocation to HotelLocation.
        /// </summary>
        public static HotelLocation ToHotelLocation(this WHLocation location)
        {
            return new HotelLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Name = location.Name,
                Address = location.Address,
                ZipCode = location.Zip,
                Country = location.Country
            };
        }

        /// <summary>
        /// Maps WHOperation to HotelOperation.
        /// </summary>
        public static HotelOperation ToHotelOperation(this WHOperation operation)
        {
            return new HotelOperation
            {
                CheckInTime = operation.CheckinTime,
                CheckOutTime = operation.CheckoutTime
            };
        }

        /// <summary>
        /// Maps WHAvailabilityRate to RoomRate.
        /// </summary>
        public static RoomRate ToRoomRate(this WHAvailabilityRate rate)
        {
            return new RoomRate
            {
                Id = rate.Id,
                TotalPrice = rate.Pricing?.TotalPrice ?? 0,
                NetPrice = rate.Retail?.TotalPrice ?? rate.Pricing?.TotalPrice ?? 0,
                RemainingRooms = rate.RemainingRooms,
                BoardType = rate.Board?.Id,
                Properties = new RateProperties
                {
                    Board = rate.Board?.Name,
                    BoardId = rate.Board?.Id,
                    HasBoard = rate.Board?.Id != null && rate.Board.Id != 0 && rate.Board.Id != 14,
                    HasCancellation = rate.Cancellation != null && rate.Cancellation.Type != "NRF",
                    CancellationName = rate.Cancellation?.Name,
                    CancellationExpiry = rate.Cancellation?.Expiry,
                    CancellationFees = rate.Cancellation?.Fees?.Select(f => new CancellationFee
                    {
                        After = f.After,
                        Fee = f.Fee
                    }).ToList(),
                    Payments = rate.Payments?.Select(p => new ScheduledPayment
                    {
                        DueDate = p.DueDate,
                        Amount = p.Amount
                    }).ToList()
                },
                SearchParty = rate.SearchParty?.ToContractPartyInfo()
            };
        }

        /// <summary>
        /// Maps WHPartyItem to Contracts PartyInfo.
        /// </summary>
        public static ContractPartyInfo ToContractPartyInfo(this WHPartyItem party)
        {
            return new ContractPartyInfo
            {
                Adults = party.adults,
                Children = party.children?.ToList(),
                RoomsCount = party.RoomsCount,
                PartyJson = party.party
            };
        }

        /// <summary>
        /// Maps WHAlternative to AlternativeDate.
        /// </summary>
        public static AlternativeDate ToAlternativeDate(this WHAlternative alt)
        {
            return new AlternativeDate
            {
                CheckIn = alt.CheckIn,
                CheckOut = alt.Checkout,
                Nights = alt.Nights,
                MinPrice = alt.MinPrice,
                NetPrice = alt.NetPrice
            };
        }

        /// <summary>
        /// Maps WHRoomData to RoomInfo.
        /// </summary>
        public static RoomInfo ToRoomInfo(this WHRoomData room, string roomCode)
        {
            return new RoomInfo
            {
                Code = roomCode,
                Name = room.Name,
                Description = room.Description,
                Capacity = room.Capacity?.ToRoomCapacity(),
                Photos = room.LargePhotos?.ToList() ?? [],
                Amenities = room.Amenities?.ToList() ?? []
            };
        }

        /// <summary>
        /// Maps WHRoomCapacity to RoomCapacity.
        /// </summary>
        public static RoomCapacity ToRoomCapacity(this WHRoomCapacity capacity)
        {
            return new RoomCapacity
            {
                MinPersons = capacity.MinPersons,
                MaxPersons = capacity.MaxPersons,
                MaxAdults = capacity.MaxAdults,
                MaxInfants = capacity.MaxInfants,
                ChildrenAllowed = capacity.ChildrenAllowed
            };
        }

        /// <summary>
        /// Maps WHHotelBasic to AutoCompleteHotel.
        /// </summary>
        public static AutoCompleteHotel ToAutoCompleteHotel(this WHHotelBasic hotel)
        {
            return new AutoCompleteHotel
            {
                Id = $"1-{hotel.Code}",  // 1 = WebHotelier provider
                Code = hotel.Code,
                Name = hotel.Name,
                Location = hotel.Location?.Name,
                CountryCode = hotel.Location?.Country,
                Type = hotel.Type,
                MappedTypes = MapHotelType(hotel.Type)
            };
        }

        /// <summary>
        /// Maps hotel type string to standardized categories (Greek).
        /// </summary>
        private static IReadOnlyList<string> MapHotelType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return [];

            var lowerType = type.ToLowerInvariant();

            return lowerType switch
            {
                var t when t.Contains("hotel") || t.Contains("ξενοδοχείο") => ["Ξενοδοχεία"],
                var t when t.Contains("villa") || t.Contains("βίλα") => ["Βίλες"],
                var t when t.Contains("apartment") || t.Contains("διαμέρισμα") => ["Διαμερίσματα"],
                var t when t.Contains("studio") || t.Contains("στούντιο") => ["Στούντιο"],
                var t when t.Contains("hostel") => ["Hostels"],
                var t when t.Contains("resort") => ["Resorts"],
                var t when t.Contains("guesthouse") || t.Contains("pension") || t.Contains("πανσιόν") => ["Πανσιόν"],
                var t when t.Contains("boutique") => ["Boutique Hotels"],
                _ => ["Άλλο"]
            };
        }
    }
}
