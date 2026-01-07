using System.Text.Json;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;
using TravelBridge.Providers.WebHotelier.Models.Common;
using TravelBridge.Providers.WebHotelier.Models.Hotel;
using TravelBridge.Providers.WebHotelier.Models.Rate;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using TravelBridge.Providers.WebHotelier.Models.Room;

namespace TravelBridge.Providers.WebHotelier;

/// <summary>
/// Internal mapping helpers for converting between Abstractions models and WebHotelier wire models.
/// These mappings are WebHotelier-specific and should not be in Abstractions.
/// </summary>
internal static class WHMappingHelpers
{
    /// <summary>
    /// Converts a PartyConfiguration to WebHotelier party JSON string.
    /// </summary>
    public static string ToPartyJson(PartyConfiguration party)
    {
        var partyItems = party.Rooms.Select(room => new WHPartyItem
        {
            adults = room.Adults,
            children = room.ChildrenAges.Length > 0 ? room.ChildrenAges : null
        }).ToList();

        return JsonSerializer.Serialize(partyItems);
    }

    /// <summary>
    /// Converts a DateOnly to WebHotelier date string (yyyy-MM-dd).
    /// </summary>
    public static string ToDateString(DateOnly date) => date.ToString("yyyy-MM-dd");

    /// <summary>
    /// Creates a WHAvailabilityRequest from a SearchAvailabilityQuery.
    /// </summary>
    public static WHAvailabilityRequest ToWHAvailabilityRequest(SearchAvailabilityQuery query, string partyJson, string sortBy, string sortOrder)
    {
        return new WHAvailabilityRequest
        {
            CheckIn = ToDateString(query.CheckIn),
            CheckOut = ToDateString(query.CheckOut),
            Party = partyJson,
            Lat = query.CenterLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Lon = query.CenterLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BottomLeftLatitude = query.BoundingBox.BottomLeftLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TopRightLatitude = query.BoundingBox.TopRightLatitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BottomLeftLongitude = query.BoundingBox.BottomLeftLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            TopRightLongitude = query.BoundingBox.TopRightLongitude.ToString(System.Globalization.CultureInfo.InvariantCulture),
            SortBy = sortBy,
            SortOrder = sortOrder
        };
    }

    /// <summary>
    /// Converts WebHotelier hotel info response to Abstractions HotelInfoResult.
    /// </summary>
    public static HotelInfoResult ToHotelInfoResult(WHHotelInfoResponse? response)
    {
        if (response?.Data == null)
        {
            return HotelInfoResult.Failure(
                response?.ErrorCode ?? "NOT_FOUND",
                response?.ErrorMessage ?? "Hotel not found");
        }

        var data = response.Data;
        return HotelInfoResult.Success(new HotelInfoData
        {
            Code = data.Code,
            Name = data.Name,
            Type = data.Type,
            Rating = data.Rating,
            Description = data.Description,
            Location = data.Location != null ? new HotelLocationData
            {
                Latitude = data.Location.Latitude,
                Longitude = data.Location.Longitude,
                Address = data.Location.Address,
                PostalCode = data.Location.Zip,
                City = data.Location.Name,
                Country = data.Location.Country
            } : null,
            Operation = data.Operation != null ? new HotelOperationData
            {
                CheckinTime = data.Operation.CheckinTime,
                CheckoutTime = data.Operation.CheckoutTime
            } : null,
            ChildrenPolicy = data.Children != null ? new ChildrenPolicyData
            {
                MaxChildAge = data.Children.AgeTo,
                InfantAge = data.Children.AgeFrom
            } : null,
            Facilities = data.Facilities?.ToList() ?? [],
            LargePhotos = data.LargePhotos?.ToList() ?? data.PhotosItems?.Select(p => p.Large).ToList() ?? []
        });
    }

    /// <summary>
    /// Converts WebHotelier room info response to Abstractions RoomInfoResult.
    /// </summary>
    public static RoomInfoResult ToRoomInfoResult(WHRoomInfoResponse? response)
    {
        if (response?.Data == null)
        {
            return RoomInfoResult.Failure(
                response?.ErrorCode ?? "NOT_FOUND",
                response?.ErrorMessage ?? "Room not found");
        }

        var data = response.Data;
        return RoomInfoResult.Success(new RoomInfoData
        {
            Name = data.Name ?? "",
            Description = data.Description,
            Capacity = data.Capacity != null ? new RoomCapacityData
            {
                MinPersons = data.Capacity.MinPersons,
                MaxPersons = data.Capacity.MaxPersons,
                MaxAdults = data.Capacity.MaxAdults,
                MaxInfants = data.Capacity.MaxInfants,
                ChildrenAllowed = data.Capacity.ChildrenAllowed
            } : null,
            Amenities = data.Amenities?.ToList() ?? [],
            LargePhotos = data.LargePhotos?.ToList() ?? data.PhotosItems?.Select(p => p.Large).ToList() ?? [],
            MediumPhotos = data.MediumPhotos?.ToList() ?? data.PhotosItems?.Select(p => p.Medium).ToList() ?? []
        });
    }

    /// <summary>
    /// Converts WebHotelier multi-availability response to Abstractions SearchAvailabilityResult.
    /// </summary>
    public static SearchAvailabilityResult ToSearchAvailabilityResult(
        WHMultiAvailabilityResponse? response,
        WHPartyItem searchParty)
    {
        if (response?.Data?.Hotels == null || !response.Data.Hotels.Any())
        {
            return SearchAvailabilityResult.Success([]);
        }

        var hotels = response.Data.Hotels.Select(hotel => new HotelSummaryData
        {
            Code = hotel.Code,
            Name = hotel.Name,
            Rating = hotel.Rating,
            MinPrice = hotel.MinPrice,
            MinPricePerNight = hotel.MinPricePerDay,
            SalePrice = hotel.SalePrice,
            PhotoMedium = hotel.PhotoM,
            PhotoLarge = hotel.PhotoL,
            Distance = hotel.Distance,
            Location = hotel.Location != null ? new HotelLocationData
            {
                Latitude = (double)(hotel.Location.Latitude ?? 0),
                Longitude = (double)(hotel.Location.Longitude ?? 0)
            } : null,
            OriginalType = hotel.OriginalType,
            SearchParty = new RatePartyInfo
            {
                Adults = searchParty.adults,
                ChildrenAges = searchParty.children ?? [],
                PartyJson = searchParty.party
            },
            Rates = hotel.Rates?.Select(rate => new HotelRateSummary
            {
                RateId = rate.Type,
                TotalPrice = rate.Retail ?? 0,
                NetPrice = rate.Price ?? 0,
                BoardTypeId = rate.BoardType,
                BoardName = null, // Board name not available in multi-rate response, only board type ID
                SearchParty = new RatePartyInfo
                {
                    Adults = searchParty.adults,
                    ChildrenAges = searchParty.children ?? [],
                    PartyJson = searchParty.party
                }
            }).ToList() ?? []
        }).ToList();

        return SearchAvailabilityResult.Success(hotels);
    }

    /// <summary>
    /// Parses party JSON string into a list of WHPartyItem.
    /// </summary>
    public static List<WHPartyItem> ParsePartyJson(string partyJson)
    {
        return JsonSerializer.Deserialize<List<WHPartyItem>>(partyJson) ?? [];
    }

    /// <summary>
    /// Groups party items by unique adult/children combination.
    /// </summary>
    public static List<WHPartyItem> GroupPartyItems(List<WHPartyItem> partyItems)
    {
        return partyItems
            .GroupBy(g => new { g.adults, Children = g.children != null ? string.Join(",", g.children) : "" })
            .Select(g => new WHPartyItem
            {
                adults = g.Key.adults,
                children = g.First().children,
                RoomsCount = g.Count(),
                party = JsonSerializer.Serialize(new List<WHPartyItem> { new() { adults = g.Key.adults, children = g.First().children } })
            }).ToList();
    }
}
