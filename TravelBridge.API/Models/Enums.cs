using System.ComponentModel;

namespace TravelBridge.API.Models
{
    public enum AutoCompleteType
    {
        [Description("Hotel")]
        hotel = 1,

        [Description("Location")]
        location = 2
    }

    public enum Language
    {
        [Description("Greek")]
        el = 1,

        [Description("English")]
        en = 2
    }

    public enum Provider
    {
        [Description("Web Hotelier")]
        WebHotelier = 1
    }

    public enum FilterType
    {
        [Description("Range")]
        range = 1,
        [Description("values")]
        values = 2
    }

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