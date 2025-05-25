using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class PartyItemDb
{
    public int Id { get; set; }

    public int Adults { get; set; }

    public string Children { get; set; } = null!;

    public string Party { get; set; } = null!;

    public DateTime? DateCreated { get; set; }

    public virtual ICollection<ReservationRate> ReservationRates { get; set; } = new List<ReservationRate>();
}
