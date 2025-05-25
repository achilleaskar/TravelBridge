using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class Reservation
{
    public int Id { get; set; }

    public DateOnly CheckIn { get; set; }

    public DateOnly CheckOut { get; set; }

    public string? HotelCode { get; set; }

    public decimal TotalAmount { get; set; }

    public byte TotalRooms { get; set; }

    public string? Party { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? DateCreated { get; set; }

    public string? HotelName { get; set; }

    public int PartialPaymentId { get; set; }

    public decimal RemainingAmount { get; set; }

    public int BookingStatus { get; set; }

    public string CheckInTime { get; set; } = null!;

    public string CheckOutTime { get; set; } = null!;

    public DateTime DateFinalized { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual PartialPaymentDb PartialPayment { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<ReservationRate> ReservationRates { get; set; } = new List<ReservationRate>();
}
