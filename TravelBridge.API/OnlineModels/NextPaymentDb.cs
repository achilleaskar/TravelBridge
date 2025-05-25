using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class NextPaymentDb
{
    public int Id { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal? Amount { get; set; }

    public int? PartialPaymentDbid { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual PartialPaymentDb? PartialPaymentDb { get; set; }
}
