using System.ComponentModel;

namespace TravelBridge.Contracts.Common
{
    /// <summary>
    /// USAGE: Common - Used across API endpoints, Database models, and Provider mappings
    /// SHOULD SPLIT: No - These are shared enums used throughout all layers
    /// </summary>
    public enum AutoCompleteType
    {
        [Description("Hotel")]
        hotel = 1,

        [Description("Location")]
        location = 2
    }

    /// <summary>
    /// USAGE: Common - Used in Database (Coupon model) and API responses
    /// SHOULD SPLIT: No - Shared enum for coupon functionality
    /// </summary>
    public enum CouponType
    {
        [Description("none")]
        none = 0,

        [Description("Flat")]
        flat = 1,

        [Description("Percentage")]
        percentage = 2
    }

    /// <summary>
    /// USAGE: Common - Used for localization across API and external services
    /// SHOULD SPLIT: No - Shared enum for language selection
    /// </summary>
    public enum Language
    {
        [Description("Greek")]
        el = 1,

        [Description("English")]
        en = 2
    }

    /// <summary>
    /// USAGE: Common - Used in Database (Reservation, ReservationRate) and API responses
    /// SHOULD SPLIT: No - Shared enum for booking status tracking
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
    /// USAGE: Common - Used to identify external hotel providers (WebHotelier, future providers)
    /// SHOULD SPLIT: No - Shared enum for provider identification
    /// </summary>
    public enum Provider
    {
        [Description("Web Hotelier")]
        WebHotelier = 1
    }

    /// <summary>
    /// USAGE: Common - Used to identify payment providers (Viva, future payment gateways)
    /// SHOULD SPLIT: No - Shared enum for payment provider identification
    /// </summary>
    public enum PaymentProvider
    {
        [Description("Viva")]
        Viva = 1
    }

    /// <summary>
    /// USAGE: Common - Used in Database (Payment model) and API responses
    /// SHOULD SPLIT: No - Shared enum for payment status tracking
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
    /// USAGE: API responses - Used in Filter model for API responses
    /// SHOULD SPLIT: Maybe - Consider moving to API-specific location if filters are only for API
    /// </summary>
    public enum FilterType
    {
        [Description("Range")]
        range = 1,
        [Description("values")]
        values = 2
    }

    /// <summary>
    /// USAGE: API requests - Used in search/sorting functionality
    /// SHOULD SPLIT: Maybe - Consider moving to API-specific location if only for API requests
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
