using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using TravelBridge.API.Contracts;

namespace TravelBridge.API.Models.DB
{
    public class Reservation : BaseModel
    {
        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }

        [MaxLength(50)]
        public string? HotelCode { get; set; }

        [MaxLength(70)]
        public string? HotelName { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal TotalAmount { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "TINYINT UNSIGNED")]
        public int TotalRooms { get; set; }
        public BookingStatus BookingStatus { get; set; }

        [MaxLength(150)]
        public string? Party { get; set; }

        #region Relations
        public Customer? Customer { get; set; }
        public int? CustomerId { get; set; }
        public List<ReservationRate> Rates { get; set; }
        public List<Payment> Payments { get; set; }
        public PartialPaymentDB PartialPayment { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal RemainingAmount { get; set; }
        public DateTime DateFinalized { get; set; }
        public string CheckInTime { get; set; }
        public string CheckOutTime { get; set; }

        internal string GetFullPartyDescription()
        {
            int adults = Rates.Sum(r => r.SearchParty?.Adults ?? 0);
            int children = Rates.Sum(r =>
            {
                var childrenStr = r.SearchParty?.Children;
                if (string.IsNullOrWhiteSpace(childrenStr))
                    return 0;

                return childrenStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
            });

            var sb = new StringBuilder();

            if (adults > 0)
            {
                sb.Append(adults == 1 ? "1 ενήλικα" : $"{adults} ενήλικες");
            }

            if (children > 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(children == 1 ? "1 παιδί" : $"{children} παιδιά");
            }

            if (Rates.Count > 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(Rates.Count == 1 ? "1 δωμάτιο" : $"{Rates.Count} δωμάτια");
            }

            return sb.ToString();
        }

        #endregion
    }
}
