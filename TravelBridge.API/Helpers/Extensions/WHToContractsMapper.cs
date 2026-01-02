using TravelBridge.Contracts.Common.Board;
using TravelBridge.Contracts.Common.Policies;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Contracts.Plugin.AutoComplete;
using TravelBridge.Providers.WebHotelier.Models.Common;
using TravelBridge.Providers.WebHotelier.Models.Hotel;
using TravelBridge.Providers.WebHotelier.Models.Rate;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using TravelBridge.Providers.WebHotelier.Models.Room;

namespace TravelBridge.API.Helpers.Extensions;

/// <summary>
/// Maps WebHotelier internal wire models to Contracts API DTOs.
/// This is the boundary layer between provider and API.
/// </summary>
public static class WHToContractsMapper
{
    #region Party Item Mapping

    public static PartyItem ToContracts(this WHPartyItem source)
    {
        return new PartyItem
        {
            adults = source.adults,
            children = source.children,
            RoomsCount = source.RoomsCount,
            party = source.party
        };
    }

    public static WHPartyItem ToWH(this PartyItem source)
    {
        return new WHPartyItem
        {
            adults = source.adults,
            children = source.children,
            RoomsCount = source.RoomsCount,
            party = source.party
        };
    }

    public static List<PartyItem> ToContracts(this IEnumerable<WHPartyItem> source)
        => source.Select(p => p.ToContracts()).ToList();

    public static List<WHPartyItem> ToWH(this IEnumerable<PartyItem> source)
        => source.Select(p => p.ToWH()).ToList();

    #endregion

    #region Payment Mapping

    public static PaymentWH ToContracts(this WHPayment source)
    {
        return new PaymentWH
        {
            DueDate = source.DueDate,
            Amount = source.Amount
        };
    }

    public static WHPayment ToWH(this PaymentWH source)
    {
        return new WHPayment
        {
            DueDate = source.DueDate,
            Amount = source.Amount
        };
    }

    public static List<PaymentWH> ToContracts(this IEnumerable<WHPayment>? source)
        => source?.Select(p => p.ToContracts()).ToList() ?? new List<PaymentWH>();

    public static List<WHPayment> ToWH(this IEnumerable<PaymentWH>? source)
        => source?.Select(p => p.ToWH()).ToList() ?? new List<WHPayment>();

    #endregion

    #region Pricing Info Mapping

    public static PricingInfo ToContracts(this WHPricingInfo source)
    {
        return new PricingInfo
        {
            Discount = source.Discount,
            ExcludedCharges = source.ExcludedCharges,
            Extras = source.Extras,
            Margin = source.Margin,
            StayPrice = source.StayPrice,
            Taxes = source.Taxes,
            TotalPrice = source.TotalPrice
        };
    }

    #endregion

    #region Cancellation Fee Mapping

    public static CancellationFee ToContracts(this WHCancellationFee source)
    {
        return new CancellationFee
        {
            After = source.After,
            Fee = source.Fee
        };
    }

    public static IEnumerable<CancellationFee> ToContracts(this IEnumerable<WHCancellationFee> source)
        => source.Select(c => c.ToContracts());

    #endregion

    #region Location Mapping

    public static Location ToContracts(this WHLocation source)
    {
        return new Location
        {
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Name = source.Name
        };
    }

    public static LocationInfo ToContractsInfo(this WHLocationInfo source)
    {
        return new LocationInfo
        {
            Latitude = source.Latitude,
            Longitude = source.Longitude,
            Name = source.Name,
            Address = source.Address,
            Zip = source.Zip,
            Country = source.Country
        };
    }

    #endregion

    #region Board Mapping

    public static Board ToContracts(this WHBoard source)
    {
        return new Board
        {
            Id = source.Id,
            Name = source.Name
        };
    }

    public static List<Board> ToContracts(this IEnumerable<WHBoard>? source)
        => source?.Select(b => b.ToContracts()).ToList() ?? new List<Board>();

    #endregion

    #region Alternative Mapping

    public static Alternative ToContracts(this WHAlternative source)
    {
        return new Alternative
        {
            CheckIn = source.CheckIn,
            Checkout = source.Checkout,
            MinPrice = source.MinPrice,
            NetPrice = source.NetPrice,
            Nights = source.Nights
        };
    }

    public static List<Alternative> ToContracts(this IEnumerable<WHAlternative>? source)
        => source?.Select(a => a.ToContracts()).ToList() ?? new List<Alternative>();

    #endregion

    #region Hotel Operation Mapping

    public static HotelOperation ToContracts(this WHHotelOperation source)
    {
        return new HotelOperation
        {
            CheckinTime = source.CheckinTime,
            CheckoutTime = source.CheckoutTime
        };
    }

    #endregion

    #region Children Policy Mapping

    public static ChildrenPolicy ToContracts(this WHChildrenPolicy source)
    {
        return new ChildrenPolicy
        {
            Allowed = source.Allowed,
            AgeFrom = source.AgeFrom,
            AgeTo = source.AgeTo,
            Policy = source.Policy
        };
    }

    #endregion

    #region Photo Info Mapping

    public static PhotoInfo ToContracts(this WHPhotoInfo source)
    {
        return new PhotoInfo
        {
            Medium = source.Medium,
            Large = source.Large
        };
    }

    public static IEnumerable<PhotoInfo> ToContracts(this IEnumerable<WHPhotoInfo>? source)
        => source?.Select(p => p.ToContracts()) ?? Enumerable.Empty<PhotoInfo>();

    #endregion

    #region Hotel Data Mapping

    public static HotelData ToContracts(this WHHotelData source)
    {
        var result = new HotelData
        {
            Code = source.Code,
            Provider = (Provider)(int)source.Provider,
            MinPrice = source.MinPrice,
            SalePrice = source.SalePrice,
            CustomInfo = source.CustomInfo,
            MappedTypes = source.MappedTypes,
            MinPricePerNight = source.MinPricePerNight,
            Name = source.Name,
            Type = source.Type,
            Rating = source.Rating,
            Description = source.Description,
            Location = source.Location.ToContractsInfo(),
            Children = source.Children.ToContracts(),
            Operation = source.Operation.ToContracts(),
            Facilities = source.Facilities,
            PhotosItems = source.PhotosItems.ToContracts(),
            LargePhotos = source.LargePhotos,
            Boards = source.Boards.ToContracts(),
            BoardsText = source.BoardsText,
            HasBoards = source.HasBoards
        };
        return result;
    }

    #endregion

    #region WebHotel Mapping (Multi-availability)

    public static WebHotel ToContracts(this WHWebHotel source)
    {
        return new WebHotel
        {
            Code = source.Code,
            Id = source.Id,
            Name = source.Name,
            Rating = source.Rating,
            MinPrice = source.MinPrice,
            MinPricePerDay = source.MinPricePerDay,
            SearchParty = source.SearchParty?.ToContracts(),
            SalePrice = source.SalePrice,
            PhotoM = source.PhotoM,
            PhotoL = source.PhotoL,
            Distance = source.Distance,
            Location = source.Location.ToContracts(),
            OriginalType = source.OriginalType,
            MappedTypes = source.MappedTypes,
            Rates = source.Rates.Select(r => r.ToContracts()).ToList(),
            Boards = source.Boards.ToContracts(),
            BoardsText = source.BoardsText,
            HasBoards = source.HasBoards
        };
    }

    public static IEnumerable<WebHotel> ToContracts(this IEnumerable<WHWebHotel>? source)
        => source?.Select(h => h.ToContracts()) ?? Enumerable.Empty<WebHotel>();

    #endregion

    #region MultiRate Mapping

    public static MultiRate ToContracts(this WHMultiRate source)
    {
        return new MultiRate
        {
            Type = source.Type,
            RoomName = source.RoomName,
            RateName = source.RateName,
            RateDescription = source.RateDescription,
            PaymentPolicy = source.PaymentPolicy,
            PaymentPolicyId = source.PaymentPolicyId,
            CancellationPolicy = source.CancellationPolicy,
            CancellationPolicyId = source.CancellationPolicyId,
            CancellationPenalty = source.CancellationPenalty,
            CancellationExpiry = source.CancellationExpiry,
            BoardType = source.BoardType,
            Price = source.Price,
            Retail = source.Retail,
            Discount = source.Discount,
            Margin = source.Margin,
            SearchParty = source.SearchParty?.ToContracts(),
            Remaining = source.Remaining
        };
    }

    #endregion

    #region HotelRate Mapping (Single availability)

    public static HotelRate ToContracts(this WHHotelRate source)
    {
        return new HotelRate
        {
            Type = source.Type,
            RoomName = source.RoomName,
            RateName = source.RateName,
            RateDescription = source.RateDescription,
            PaymentPolicy = source.PaymentPolicy,
            PaymentPolicyId = source.PaymentPolicyId,
            CancellationPolicy = source.CancellationPolicy,
            CancellationPolicyId = source.CancellationPolicyId,
            CancellationPenalty = source.CancellationPenalty,
            CancellationExpiry = source.CancellationExpiry,
            BoardType = source.BoardType,
            totalPrice = source.totalPrice,
            CancellationFees = source.CancellationFees.ToContracts(),
            Payments = source.Payments.ToContracts(),
            Id = source.Id,
            Pricing = source.Pricing.ToContracts(),
            RemainingRooms = source.RemainingRooms,
            Retail = source.Retail.ToContracts(),
            Status = source.Status,
            StatusDescription = source.StatusDescription,
            ProfitPerc = source.ProfitPerc,
            SearchParty = source.SearchParty?.ToContracts()
        };
    }

    public static List<HotelRate> ToContracts(this IEnumerable<WHHotelRate>? source)
        => source?.Select(r => r.ToContracts()).ToList() ?? new List<HotelRate>();

    #endregion

    #region Room Info Mapping

    public static RoomInfo ToContracts(this WHRoomInfo source)
    {
        return new RoomInfo
        {
            Name = source.Name,
            Description = source.Description,
            Capacity = source.Capacity.ToContracts(),
            Amenities = source.Amenities,
            PhotosItems = source.PhotosItems.ToContracts(),
            LargePhotos = source.LargePhotos,
            MediumPhotos = source.MediumPhotos
        };
    }

    public static RoomCapacity ToContracts(this WHRoomCapacity source)
    {
        return new RoomCapacity
        {
            MinPersons = source.MinPersons,
            MaxPersons = source.MaxPersons,
            MaxAdults = source.MaxAdults,
            MaxInfants = source.MaxInfants,
            ChildrenAllowed = source.ChildrenAllowed,
            CountInfant = source.CountInfant
        };
    }

    #endregion

    #region Hotel Mapping (Properties list)

    public static AutoCompleteHotel ToAutoComplete(this WHHotel source)
    {
        return new AutoCompleteHotel(
            source.code,
            Provider.WebHotelier,
            source.name,
            source.location?.name ?? "",
            source.location?.country ?? "",
            source.type);
    }

    public static IEnumerable<AutoCompleteHotel> ToAutoComplete(this IEnumerable<WHHotel>? source)
        => source?.Select(h => h.ToAutoComplete()) ?? Enumerable.Empty<AutoCompleteHotel>();

    #endregion
}
