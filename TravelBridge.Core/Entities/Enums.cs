using System.ComponentModel;

namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Status of a booking/reservation.
    /// </summary>
    public enum BookingStatus
    {
        [Description("New")]
        New = 0,
        [Description("Pending")]
        Pending = 1,
        [Description("Running")]
        Running = 2,
        [Description("Confirmed")]
        Confirmed = 3,
        [Description("Cancelled")]
        Cancelled = 4,
        [Description("Error")]
        Error = 5
    }

    /// <summary>
    /// Payment provider types.
    /// </summary>
    public enum PaymentProvider
    {
        [Description("Viva")]
        Viva = 1
    }

    /// <summary>
    /// Status of a payment transaction.
    /// </summary>
    public enum PaymentStatus
    {
        [Description("Pending")]
        Pending = 1,
        [Description("Success")]
        Success = 2,
        [Description("Failed")]
        Failed = 3
    }

    /// <summary>
    /// Hotel provider types.
    /// </summary>
    public enum HotelProvider
    {
        [Description("Web Hotelier")]
        WebHotelier = 1
    }

    /// <summary>
    /// Coupon discount types.
    /// </summary>
    public enum CouponType
    {
        [Description("none")]
        None = 0,
        [Description("Flat")]
        Flat = 1,
        [Description("Percentage")]
        Percentage = 2
    }

    /// <summary>
    /// Autocomplete result types.
    /// </summary>
    public enum AutoCompleteType
    {
        [Description("Hotel")]
        Hotel = 1,
        [Description("Location")]
        Location = 2
    }

    /// <summary>
    /// Supported languages.
    /// </summary>
    public enum Language
    {
        [Description("Greek")]
        Greek = 1,
        [Description("English")]
        English = 2
    }

    /// <summary>
    /// Filter types for search.
    /// </summary>
    public enum FilterType
    {
        [Description("Range")]
        Range = 1,
        [Description("Values")]
        Values = 2
    }

    /// <summary>
    /// Sort options for search results.
    /// </summary>
    public enum SortOption
    {
        [Description("popularity")]
        Popularity,
        [Description("distance")]
        Distance,
        [Description("price_asc")]
        PriceAsc,
        [Description("price_desc")]
        PriceDesc
    }
}
