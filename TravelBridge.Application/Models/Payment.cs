namespace TravelBridge.Application.Models;

/// <summary>
/// Represents a payment schedule item.
/// Application-layer domain model.
/// </summary>
public class Payment
{
    public DateTime? DueDate { get; set; }
    public decimal? Amount { get; set; }
}
