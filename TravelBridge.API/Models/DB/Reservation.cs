using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [MaxLength(150)]
        public string? Party { get; set; }

        #region Relations
        public Customer? Customer { get; set; }
        public int? CustomerId { get; set; }
        public List<ReservationRate> Rates { get; set; }
        public List<Payment> Payments { get; set; }
        #endregion
    }
}
