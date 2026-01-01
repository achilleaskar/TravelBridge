using Microsoft.Extensions.Options;
using TravelBridge.Core.Interfaces;
using TravelBridge.Core.Services;

namespace TravelBridge.API.Services
{
    /// <summary>
    /// Injectable pricing service that wraps PricingConfig.
    /// Allows DI-based access to pricing configuration.
    /// </summary>
    public class PricingService : IPricingService
    {
        private readonly PricingOptions _options;

        // List of special hotel codes that don't get the standard discount
        private static readonly HashSet<string> SpecialHotelCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "GRECASTIR",
            "LEONIKIRES",
            "LAKOPETRA",
            "ROYALPARK",
            "CRETAPAL",
            "GREGNATIA",
            "EVAPALACE",
            "GREFILOXE",
            "GELINVRSPA",
            "GRECRHOROY",
            "DAPHNILBAY",
            "KOSIMPERIA",
            "OLYMPIAVIL",
            "ILIAPALMS",
            "GRECELGREC",
            "OLTHALASSO",
            "LARIMPER",
            "CLUBMARINE",
            "MELIPALACE",
            "PALLASATH",
            "PLAZAGRECO",
            "OLIVA",
            "AMIRANDES",
            "CAPESOUNIO",
            "CARAMEL",
            "CORFUIMPER",
            "MANDOLAROS",
            "MYKONOSBLU",
            "STARMYK",
            "VOULSUITES"
        };

        public PricingService(IOptions<PricingOptions> options)
        {
            _options = options.Value;
        }

        public decimal MinimumMarginDecimal => _options.MinimumMarginDecimal;

        public int MinimumMarginPercent => _options.MinimumMarginPercent;

        public decimal SpecialHotelPriceMultiplier => _options.SpecialHotelPriceMultiplier;

        public bool IsSpecialHotel(string hotelCode)
        {
            return SpecialHotelCodes.Contains(hotelCode);
        }
    }
}
