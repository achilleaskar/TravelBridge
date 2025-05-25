using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class PartialPaymentDb
{
    public int Id { get; set; }

    public decimal PrepayAmount { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual ICollection<NextPaymentDb> NextPaymentDbs { get; set; } = new List<NextPaymentDb>();

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
