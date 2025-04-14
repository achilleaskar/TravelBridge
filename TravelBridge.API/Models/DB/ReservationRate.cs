using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB
{
    public class ReservationRate : BaseModel
    {
        [MaxLength(50)]
        public string? HotelCode { get; set; }

        public int RateId { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal Price { get; set; }

        [Range(0, 100)]
        [Column(TypeName = "TINYINT UNSIGNED")]
        public int Quantity { get; set; }
        public Provider? Provider { get; set; }

        #region Relations

        public Reservation? Reservation { get; set; }
        public int? ReservationId { get; set; }

        #endregion Relations
    }
}