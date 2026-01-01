using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Reservation entity - stores booking information.
    /// </summary>
    public class ReservationEntity : BaseEntity
    {
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }
        public string? HotelCode { get; set; }
        public string? HotelName { get; set; }
        public decimal TotalAmount { get; set; }
        public int TotalRooms { get; set; }
        public BookingStatus BookingStatus { get; set; }
        public string? Party { get; set; }
        public decimal RemainingAmount { get; set; }
        public DateTime DateFinalized { get; set; }
        public string? CheckInTime { get; set; }
        public string? CheckOutTime { get; set; }
        public string? Coupon { get; set; }

        // Foreign keys
        public int? CustomerId { get; set; }

        // Navigation properties
        public CustomerEntity? Customer { get; set; }
        public ICollection<ReservationRateEntity> Rates { get; set; } = [];
        public ICollection<PaymentEntity> Payments { get; set; } = [];
        public PartialPaymentEntity? PartialPayment { get; set; }

        /// <summary>
        /// Number of nights for this reservation.
        /// </summary>
        public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

        /// <summary>
        /// Gets full party description (Greek).
        /// </summary>
        public string GetFullPartyDescription()
        {
            int adults = Rates.Sum(r => r.SearchParty?.Adults ?? 0);
            int children = Rates.Sum(r =>
            {
                var childrenStr = r.SearchParty?.Children;
                if (string.IsNullOrWhiteSpace(childrenStr))
                    return 0;
                return childrenStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            });

            var parts = new List<string>();
            if (adults > 0)
                parts.Add(adults == 1 ? "1 ενήλικα" : $"{adults} ενήλικες");
            if (children > 0)
                parts.Add(children == 1 ? "1 παιδί" : $"{children} παιδιά");
            if (Rates.Count > 0)
                parts.Add(Rates.Count == 1 ? "1 δωμάτιο" : $"{Rates.Count} δωμάτια");

            return string.Join(", ", parts);
        }
    }
}
