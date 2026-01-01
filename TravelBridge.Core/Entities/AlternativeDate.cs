namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Represents an alternative date option with pricing.
    /// Provider-agnostic model for showing alternative availability.
    /// </summary>
    public class AlternativeDate
    {
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
        public int Nights { get; set; }
        public decimal MinPrice { get; set; }
        public decimal NetPrice { get; set; }

        /// <summary>
        /// Price per night.
        /// </summary>
        public decimal PricePerNight => Nights > 0 ? MinPrice / Nights : 0;
    }
}
