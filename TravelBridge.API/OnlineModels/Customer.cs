using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class Customer
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Tel { get; set; } = null!;

    public string? CountryCode { get; set; }

    public DateTime? DateCreated { get; set; }

    public string Email { get; set; } = null!;

    public string Notes { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
