using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TravelBridge.API.Models.DB;

/// <summary>
/// Daily inventory record for a room type.
/// One row per room type per night.
/// Primary key: (RoomTypeId, Date)
/// 
/// Date range semantics: [start, end) - endDate is EXCLUSIVE.
/// For a stay check-in 2026-07-15 to check-out 2026-07-18,
/// inventory is consumed for dates 2026-07-15, 2026-07-16, 2026-07-17.
/// The checkout date (2026-07-18) is NOT consumed.
/// </summary>
[Table("OwnedInventoryDaily")]
[PrimaryKey(nameof(RoomTypeId), nameof(Date))]
public class OwnedInventoryDaily
{
    /// <summary>
    /// Foreign key to room type (part of composite PK).
    /// </summary>
    public int RoomTypeId { get; set; }

    /// <summary>
    /// The night being sold (part of composite PK).
    /// This is the DATE of the night, not the checkout date.
    /// </summary>
    [Column(TypeName = "DATE")]
    public DateOnly Date { get; set; }

    // Inventory Counters

    /// <summary>
    /// Total physical units available for this room type on this night.
    /// </summary>
    [Range(0, 9999)]
    public int TotalUnits { get; set; }

    /// <summary>
    /// Units intentionally removed from sale (maintenance, renovation, stop-sell).
    /// Admin-controlled.
    /// Constraint: 0 <= ClosedUnits <= TotalUnits
    /// </summary>
    [Range(0, 9999)]
    public int ClosedUnits { get; set; }

    /// <summary>
    /// Units temporarily reserved (holds that haven't been confirmed yet).
    /// Phase 4 feature - keep at 0 for Phase 3.
    /// </summary>
    [Range(0, 9999)]
    public int HeldUnits { get; set; }

    /// <summary>
    /// Units with confirmed bookings.
    /// Phase 4 feature - keep at 0 for Phase 3.
    /// </summary>
    [Range(0, 9999)]
    public int ConfirmedUnits { get; set; }

    // Pricing

    /// <summary>
    /// Optional price override for this specific night.
    /// If null, falls back to OwnedRoomType.BasePricePerNight.
    /// Allows seasonal/weekend pricing without rate plans.
    /// </summary>
    [Column(TypeName = "DECIMAL(10,2)")]
    [Range(0.01, 999999.99)]
    public decimal? PricePerNight { get; set; }

    // Audit

    /// <summary>
    /// When this inventory record was last modified (UTC).
    /// </summary>
    [Column(TypeName = "DATETIME(6)")]
    public DateTime? LastModifiedUtc { get; set; }

    // Navigation properties
    public OwnedRoomType RoomType { get; set; } = null!;

    // Computed properties (not mapped to database)

    /// <summary>
    /// Derived availability: TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits.
    /// This property is NOT stored in the database.
    /// </summary>
    [NotMapped]
    public int AvailableUnits => TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits;

    /// <summary>
    /// Validates that inventory counters don't exceed capacity.
    /// Call this before saving changes.
    /// </summary>
    public bool IsValid(out string? error)
    {
        if (ClosedUnits < 0)
        {
            error = "ClosedUnits cannot be negative";
            return false;
        }
        if (HeldUnits < 0)
        {
            error = "HeldUnits cannot be negative";
            return false;
        }
        if (ConfirmedUnits < 0)
        {
            error = "ConfirmedUnits cannot be negative";
            return false;
        }
        if (ClosedUnits > TotalUnits)
        {
            error = "ClosedUnits cannot exceed TotalUnits";
            return false;
        }
        if (ClosedUnits + HeldUnits + ConfirmedUnits > TotalUnits)
        {
            error = "Sum of ClosedUnits, HeldUnits, and ConfirmedUnits cannot exceed TotalUnits";
            return false;
        }
        error = null;
        return true;
    }
}
