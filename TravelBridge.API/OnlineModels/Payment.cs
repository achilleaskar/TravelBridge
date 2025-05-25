using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class Payment
{
    public int Id { get; set; }

    public DateTime DateFinalized { get; set; }

    public int PaymentProvider { get; set; }

    public int PaymentStatus { get; set; }

    public decimal Amount { get; set; }

    public string? TransactionId { get; set; }

    public string? OrderCode { get; set; }

    public int? CustomerId { get; set; }

    public int? ReservationId { get; set; }

    public DateTime? DateCreated { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual Reservation? Reservation { get; set; }
}
