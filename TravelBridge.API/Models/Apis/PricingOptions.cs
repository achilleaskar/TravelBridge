namespace TravelBridge.API.Models.Apis
{
    public class PricingOptions
    {
        /// <summary>
        /// Minimum margin percentage to add over net price (default: 10 = 10%)
        /// </summary>
        public int MinimumMarginPercent { get; set; } = 10;

        /// <summary>
        /// Discount percentage for special hotels (default: 5 = 5% discount, resulting in 0.95 multiplier)
        /// </summary>
        public int SpecialHotelDiscountPercent { get; set; } = 5;

        /// <summary>
        /// Calculated minimum margin as decimal (e.g., 0.10 for 10%)
        /// </summary>
        public decimal MinimumMarginDecimal => MinimumMarginPercent / 100m;

        /// <summary>
        /// Calculated price multiplier for special hotels (e.g., 0.95 for 5% discount)
        /// </summary>
        public decimal SpecialHotelPriceMultiplier => 1m - (SpecialHotelDiscountPercent / 100m);
    }

    /// <summary>
    /// Static holder for pricing configuration, initialized at startup
    /// </summary>
    public static class PricingConfig
    {
        private static PricingOptions _options = new();

        public static void Initialize(PricingOptions options)
        {
            _options = options ?? new PricingOptions();
        }

        /// <summary>
        /// Minimum margin as decimal (e.g., 0.10 for 10%)
        /// </summary>
        public static decimal MinimumMarginDecimal => _options.MinimumMarginDecimal;

        /// <summary>
        /// Minimum margin as integer percent (e.g., 10 for 10%)
        /// </summary>
        public static int MinimumMarginPercent => _options.MinimumMarginPercent;

        /// <summary>
        /// Price multiplier for special hotels (e.g., 0.95 for 5% discount)
        /// </summary>
        public static decimal SpecialHotelPriceMultiplier => _options.SpecialHotelPriceMultiplier;
    }
}
