using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB;

/// <summary>
/// Owned room type entity.
/// Represents a type of room available at an owned hotel.
/// </summary>
[Table("OwnedRoomTypes")]
public class OwnedRoomType : BaseModel
{
    /// <summary>
    /// Foreign key to the parent hotel.
    /// </summary>
    public int HotelId { get; set; }

    /// <summary>
    /// Stable code for this room type (used in rate IDs like "rt_5-2").
    /// Must be unique within the hotel.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    /// <summary>
    /// Display name for the room type.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// Detailed description of the room.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Maximum number of adults allowed in this room type.
    /// </summary>
    [Range(1, 50)]
    public int MaxAdults { get; set; }

    /// <summary>
    /// Maximum number of children allowed in this room type.
    /// </summary>
    [Range(0, 50)]
    public int MaxChildren { get; set; }

    /// <summary>
    /// Maximum total occupancy (adults + children).
    /// </summary>
    [Range(1, 50)]
    public int MaxTotalOccupancy { get; set; }

    /// <summary>
    /// Base price per night (fallback when OwnedInventoryDaily.PricePerNight is null).
    /// </summary>
    [Column(TypeName = "DECIMAL(10,2)")]
    [Range(0.01, 999999.99)]
    public decimal BasePricePerNight { get; set; }

    /// <summary>
    /// Default total units to use when seeding inventory rows.
    /// Represents the standard capacity for this room type.
    /// </summary>
    [Range(0, 9999)]
    public int DefaultTotalUnits { get; set; } = 10;

    /// <summary>
    /// Whether this room type is active and bookable.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public OwnedHotel Hotel { get; set; } = null!;
    public ICollection<OwnedInventoryDaily> InventoryDays { get; set; } = new List<OwnedInventoryDaily>();
}
