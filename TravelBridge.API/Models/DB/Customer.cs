using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB
{
    public class Customer : BaseModel
    {
        public Customer(string? firstName, string? lastName, string? tel, string? countryCode, string? email)
        {
            FirstName = firstName;
            LastName = lastName;
            Tel = tel;
            CountryCode = countryCode;
            Email = email;
        }

        [MaxLength(50)]
        public string? FirstName { get; set; }

        [MaxLength(50)]
        public string? LastName { get; set; }

        [MaxLength(80)]
        public string? Email { get; set; }

        [MaxLength(20)]
        [RegularExpression(@"^\+?[1-9]\d{1,14}$", ErrorMessage = "Invalid phone number format.")]
        public string? Tel { get; set; }

        [Column(TypeName = "CHAR(2)")]
        public string? CountryCode { get; set; }

        #region Relations

        public IEnumerable<Payment> Payments { get; set; }
        public IEnumerable<Reservation> Reservations { get; set; }

        #endregion Relations
    }
}