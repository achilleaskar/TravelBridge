using System;
using System.Collections.Generic;

namespace TravelBridge.API.OnlineModels;

public partial class ReservationRate
{
    public int Id { get; set; }

    public string? HotelCode { get; set; }

    public string RateId { get; set; } = null!;

    public decimal Price { get; set; }

    public byte Quantity { get; set; }

    public int? Provider { get; set; }

    public int? ReservationId { get; set; }

    public DateTime? DateCreated { get; set; }

    public int? SearchPartyId { get; set; }

    public int BookingStatus { get; set; }

    public DateTime DateFinalized { get; set; }

    public decimal NetPrice { get; set; }

    public int ProviderResId { get; set; }

    public string? Name { get; set; }

    public string? BoardInfo { get; set; }

    public string? CancelationInfo { get; set; }

    public virtual Reservation? Reservation { get; set; }

    public virtual PartyItemDb? SearchParty { get; set; }
}
