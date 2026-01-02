using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using TravelBridge.API.Helpers;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.API.Models.DB
{
    public class ReservationRate : BaseModel
    {
        [MaxLength(50)]
        public string? HotelCode { get; set; }

        [MaxLength(20)]
        public string RateId { get; set; }
        public BookingStatus BookingStatus { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal NetPrice { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "TINYINT UNSIGNED")]
        public int Quantity { get; set; }
        public Provider? Provider { get; set; }

        #region Relations

        public Reservation? Reservation { get; set; }
        public int? ReservationId { get; set; }
        public PartyItemDB? SearchParty { get; set; }
        public DateTime DateFinalized { get; set; }
        public int ProviderResId { get; set; }
        public string? Name { get; set; }
        public string? CancelationInfo { get; set; }
        public string? BoardInfo { get; set; }

        internal string GetPartyInfo()
        {
            int adults = SearchParty?.Adults ?? 0;
            int children;
            var childrenStr = SearchParty?.Children;
            if (string.IsNullOrWhiteSpace(childrenStr))
                children = 0;
            else
                children = childrenStr.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;


            var sb = new StringBuilder();

            if (adults > 0)
            {
                sb.Append(adults == 1 ? "1 ενήλικας" : $"{adults} ενήλικες");
            }

            if (children > 0)
            {
                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(children == 1 ? "1 παιδί" : $"{children} παιδιά");
            }

            return sb.ToString();
        }

        #endregion Relations
    }
}