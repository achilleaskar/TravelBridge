using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Data.Models
{
    /// <summary>
    /// Customer entity - stores customer information.
    /// </summary>
    public class CustomerEntity : BaseEntity
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Tel { get; set; } = "";
        public string? CountryCode { get; set; }
        public string Notes { get; set; } = "";

        // Navigation properties
        public ICollection<PaymentEntity> Payments { get; set; } = [];
        public ICollection<ReservationEntity> Reservations { get; set; } = [];
    }
}
