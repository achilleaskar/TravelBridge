using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Reservation rate entity - stores individual room/rate selections.
    /// </summary>
    public class ReservationRateEntity : BaseEntity
    {
        public string? HotelCode { get; set; }
        public string RateId { get; set; } = "";
        public BookingStatus BookingStatus { get; set; }
        public decimal Price { get; set; }
        public decimal NetPrice { get; set; }
        public int Quantity { get; set; }
        public HotelProvider? Provider { get; set; }
        public DateTime DateFinalized { get; set; }
        public int ProviderResId { get; set; }
        public string? Name { get; set; }
        public string? CancelationInfo { get; set; }
        public string? BoardInfo { get; set; }

        // Foreign keys
        public int? ReservationId { get; set; }

        // Navigation properties
        public ReservationEntity? Reservation { get; set; }
        public PartyItemEntity? SearchParty { get; set; }

        /// <summary>
        /// Gets party description (Greek).
        /// </summary>
        public string GetPartyInfo()
        {
            int adults = SearchParty?.Adults ?? 0;
            int children = 0;
            var childrenStr = SearchParty?.Children;
            if (!string.IsNullOrWhiteSpace(childrenStr))
                children = childrenStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;

            var parts = new List<string>();
            if (adults > 0)
                parts.Add(adults == 1 ? "1 ενήλικας" : $"{adults} ενήλικες");
            if (children > 0)
                parts.Add(children == 1 ? "1 παιδί" : $"{children} παιδιά");

            return string.Join(", ", parts);
        }
    }
}
