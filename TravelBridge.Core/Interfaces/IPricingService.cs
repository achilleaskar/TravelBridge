namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Interface for pricing calculations.
    /// Provides access to pricing configuration without static state.
    /// </summary>
    public interface IPricingService
    {
        /// <summary>
        /// Minimum margin as decimal (e.g., 0.10 for 10%).
        /// </summary>
        decimal MinimumMarginDecimal { get; }

        /// <summary>
        /// Minimum margin as integer percent (e.g., 10 for 10%).
        /// </summary>
        int MinimumMarginPercent { get; }

        /// <summary>
        /// Price multiplier for special hotels (e.g., 0.95 for 5% discount).
        /// </summary>
        decimal SpecialHotelPriceMultiplier { get; }

        /// <summary>
        /// Checks if a hotel code is in the special hotels list.
        /// </summary>
        bool IsSpecialHotel(string hotelCode);
    }
}
