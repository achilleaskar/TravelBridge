using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Common.Payments;
using TravelBridge.Contracts.Common.Policies;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.API.Providers;

/// <summary>
/// Maps provider-neutral Abstractions models to API Contracts models.
/// This mapping lives in API layer, not in providers.
/// </summary>
public static class ProviderToContractsMapper
{
    /// <summary>
    /// Maps SearchAvailabilityResult to PluginSearchResponse.
    /// </summary>
    /// <param name="result">The provider result.</param>
    /// <param name="providerId">The provider ID to use for composite hotel IDs.</param>
    /// <param name="nights">Number of nights (currently unused, kept for future per-night calculations).</param>
    public static PluginSearchResponse ToPluginSearchResponse(SearchAvailabilityResult result, int providerId, int nights)
    {
        if (!result.IsSuccess || result.Hotels.Count == 0)
        {
            return new PluginSearchResponse
            {
                Results = [],
                Filters = []
            };
        }

        var hotels = result.Hotels.Select(h => new WebHotel
        {
            Id = $"{providerId}-{h.Code}",
            Code = h.Code,
            Name = h.Name,
            Rating = h.Rating,
            MinPrice = h.MinPrice,
            MinPricePerDay = h.MinPricePerNight,
            SalePrice = h.SalePrice,
            PhotoM = h.PhotoMedium,
            PhotoL = h.PhotoLarge,
            Distance = h.Distance,
            Location = h.Location != null ? new Location
            {
                Latitude = (decimal?)h.Location.Latitude,
                Longitude = (decimal?)h.Location.Longitude
            } : null,
            OriginalType = h.OriginalType,
            SearchParty = h.SearchParty != null ? new PartyItem
            {
                adults = h.SearchParty.Adults,
                children = h.SearchParty.ChildrenAges
            } : null,
            Rates = h.Rates.Select(r => new MultiRate
            {
                Type = r.RateId,
                Price = r.NetPrice,
                Retail = r.TotalPrice,
                BoardType = r.BoardTypeId,
                SearchParty = r.SearchParty != null ? new PartyItem
                {
                    adults = r.SearchParty.Adults,
                    children = r.SearchParty.ChildrenAges
                } : null
            }).ToList()
        }).ToList();

        return new PluginSearchResponse
        {
            Results = hotels,
            Filters = []
        };
    }

    /// <summary>
    /// Maps HotelInfoData to Contracts HotelData.
    /// </summary>
    public static HotelData ToHotelData(HotelInfoData data, int providerId)
    {
        return new HotelData
        {
            Code = data.Code,
            Name = data.Name,
            Type = data.Type ?? "",
            Rating = data.Rating,
            Description = data.Description,
            Provider = providerId == ProviderIds.WebHotelier ? Provider.WebHotelier : Provider.WebHotelier, // Only WebHotelier supported for now
            Location = data.Location != null ? new LocationInfo
            {
                Latitude = data.Location.Latitude,
                Longitude = data.Location.Longitude,
                Address = data.Location.Address,
                Zip = data.Location.PostalCode,
                Name = data.Location.City,
                Country = data.Location.Country
            } : null,
            Operation = data.Operation != null ? new HotelOperation
            {
                CheckinTime = data.Operation.CheckinTime,
                CheckoutTime = data.Operation.CheckoutTime
            } : null,
            Children = data.ChildrenPolicy != null ? new ChildrenPolicy
            {
                AgeTo = data.ChildrenPolicy.MaxChildAge,
                AgeFrom = data.ChildrenPolicy.InfantAge
            } : null,
            Facilities = data.Facilities,
            LargePhotos = data.LargePhotos
        };
    }

    /// <summary>
    /// Maps RoomInfoData to Contracts RoomInfo.
    /// </summary>
    public static RoomInfo ToRoomInfo(RoomInfoData data)
    {
        return new RoomInfo
        {
            Name = data.Name,
            Description = data.Description,
            Capacity = data.Capacity != null ? new RoomCapacity
            {
                MinPersons = data.Capacity.MinPersons,
                MaxPersons = data.Capacity.MaxPersons,
                MaxAdults = data.Capacity.MaxAdults,
                MaxInfants = data.Capacity.MaxInfants,
                ChildrenAllowed = data.Capacity.ChildrenAllowed
            } : null,
            Amenities = data.Amenities?.ToList(),
            LargePhotos = data.LargePhotos,
            MediumPhotos = data.MediumPhotos
        };
    }

    /// <summary>
    /// Parses party JSON string to PartyConfiguration.
    /// </summary>
    public static PartyConfiguration ParsePartyConfiguration(string partyJson)
    {
        var items = System.Text.Json.JsonSerializer.Deserialize<List<PartyJsonItem>>(partyJson) ?? [];
        
        var rooms = items.Select(item => new PartyRoom
        {
            Adults = item.adults,
            ChildrenAges = item.children ?? []
        }).ToArray();

        return new PartyConfiguration { Rooms = rooms };
    }

    /// <summary>
    /// Maps HotelAvailabilityResult to a list of contract HotelRate objects.
    /// These can then be processed by existing pricing logic.
    /// </summary>
    public static List<HotelRate> ToHotelRates(HotelAvailabilityResult result)
    {
        if (!result.IsSuccess || result.Data?.Rooms == null)
        {
            return [];
        }

        var rates = new List<HotelRate>();
        
        foreach (var room in result.Data.Rooms)
        {
            foreach (var providerRate in room.Rates)
            {
                var contractRate = new HotelRate
                {
                    Type = room.RoomCode,
                    RoomName = room.RoomName,
                    RateName = providerRate.RateName,
                    RateDescription = providerRate.RateDescription ?? "",
                    
                    Id = providerRate.RateId,
                    RemainingRooms = providerRate.RemainingRooms,
                    BoardType = providerRate.BoardTypeId,
                    
                    // Pricing breakdown
                    Pricing = providerRate.Pricing != null ? new PricingInfo
                    {
                        Discount = providerRate.Pricing.Discount,
                        ExcludedCharges = providerRate.Pricing.ExcludedCharges,
                        Extras = providerRate.Pricing.Extras,
                        Margin = providerRate.Pricing.Margin,
                        StayPrice = providerRate.Pricing.StayPrice,
                        Taxes = providerRate.Pricing.Taxes,
                        TotalPrice = providerRate.Pricing.TotalPrice
                    } : new PricingInfo { TotalPrice = providerRate.NetPrice },
                    
                    Retail = providerRate.Retail != null ? new PricingInfo
                    {
                        Discount = providerRate.Retail.Discount,
                        ExcludedCharges = providerRate.Retail.ExcludedCharges,
                        Extras = providerRate.Retail.Extras,
                        Margin = providerRate.Retail.Margin,
                        StayPrice = providerRate.Retail.StayPrice,
                        Taxes = providerRate.Retail.Taxes,
                        TotalPrice = providerRate.Retail.TotalPrice
                    } : new PricingInfo { TotalPrice = providerRate.TotalPrice },
                    
                    // Cancellation
                    CancellationPolicy = providerRate.CancellationPolicy ?? "",
                    CancellationPolicyId = providerRate.CancellationPolicyId,
                    CancellationPenalty = providerRate.CancellationPenalty ?? "",
                    CancellationExpiry = providerRate.CancellationDeadline,
                    CancellationFees = providerRate.CancellationFees.Select(cf => new CancellationFee
                    {
                        After = cf.After,
                        Fee = cf.Fee
                    }),
                    
                    // Payments
                    PaymentPolicy = providerRate.PaymentPolicy ?? "",
                    PaymentPolicyId = providerRate.PaymentPolicyId,
                    Payments = providerRate.Payments.Select(p => new PaymentWH
                    {
                        DueDate = p.DueDate,
                        Amount = p.Amount
                    }).ToList(),
                    
                    // Status
                    Status = providerRate.Status,
                    StatusDescription = providerRate.StatusDescription,
                    
                    // Party - include RoomsCount for internal logic
                    SearchParty = providerRate.SearchParty != null ? new PartyItem
                    {
                        adults = providerRate.SearchParty.Adults,
                        children = providerRate.SearchParty.ChildrenAges,
                        RoomsCount = providerRate.SearchParty.RoomsCount,
                        party = providerRate.SearchParty.PartyJson
                    } : null
                };
                
                rates.Add(contractRate);
            }
        }
        
        return rates;
    }

    /// <summary>
    /// Maps HotelAvailabilityResult to SingleHotelAvailabilityInfo (hotel + location only, without rooms).
    /// Rooms are added separately after pricing logic is applied.
    /// </summary>
    public static SingleHotelAvailabilityInfo ToSingleHotelAvailabilityInfo(HotelAvailabilityResult result, int providerId)
    {
        return new SingleHotelAvailabilityInfo
        {
            Code = result.Data?.HotelCode ?? "",
            Name = result.Data?.HotelName ?? "",
            Provider = providerId == ProviderIds.WebHotelier ? Provider.WebHotelier : Provider.WebHotelier,
            Location = result.Data?.Location != null ? new Location
            {
                Latitude = result.Data.Location.Latitude,
                Longitude = result.Data.Location.Longitude,
                Name = result.Data.Location.Name
            } : null,
            Rooms = [],
            Alternatives = result.Data?.Alternatives.Select(a => new Alternative
            {
                CheckIn = a.CheckIn.ToDateTime(TimeOnly.MinValue),
                Checkout = a.CheckOut.ToDateTime(TimeOnly.MinValue),
                Nights = a.Nights,
                MinPrice = a.MinPrice,
                NetPrice = a.NetPrice
            }).ToList() ?? []
        };
    }
    
    private record PartyJsonItem(int adults, int[]? children);
}
