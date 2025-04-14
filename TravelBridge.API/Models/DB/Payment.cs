using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB
{
    public class Payment : BaseModel
    {
        public DateTime DateFinalized { get; set; }

        public PaymentProvider PaymentProvider { get; set; }
        public PaymentStatus PaymentStatus { get; set; }

        [Column(TypeName = "DECIMAL(10,2)")]
        public decimal Amount { get; set; }

        [MaxLength(50)]
        public string? TransactionId { get; set; }


        [MaxLength(50)]
        public string? OrderCode { get; set; }

        #region Relations
        public Customer? Customer { get; set; }
        public int? CustomerId { get; set; }
        public Reservation? Reservation { get; set; }
        public int? ReservationId { get; set; }
        #endregion

    }
}