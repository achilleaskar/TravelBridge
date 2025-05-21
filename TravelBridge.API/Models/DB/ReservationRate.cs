using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        #endregion Relations
    }
}