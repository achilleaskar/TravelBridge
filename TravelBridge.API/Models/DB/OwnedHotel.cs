using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TravelBridge.API.Models.DB;

/// <summary>
/// Owned hotel entity (providerId = 0).
/// Represents hotels managed directly in the TravelBridge system.
/// </summary>
[Table("OwnedHotels")]
public class OwnedHotel : BaseModel
{
    /// <summary>
    /// Unique code for the hotel (used in composite IDs like "0-OWNTEST01").
    /// Must not contain dashes.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public required string Code { get; set; }

    /// <summary>
    /// Hotel name.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// Hotel description.
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? Description { get; set; }

    /// <summary>
    /// Hotel type (e.g., "Hotel", "Resort", "Villa", "Apartment").
    /// </summary>
    [MaxLength(100)]
    public string? Type { get; set; }

    /// <summary>
    /// Star rating (0-5).
    /// </summary>
    [Range(0, 5)]
    public int? Rating { get; set; }

    /// <summary>
    /// Latitude for geolocation (-90 to 90).
    /// </summary>
    [Column(TypeName = "DECIMAL(10,7)")]
    [Range(-90, 90)]
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude for geolocation (-180 to 180).
    /// </summary>
    [Column(TypeName = "DECIMAL(10,7)")]
    [Range(-180, 180)]
    public decimal Longitude { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    [MaxLength(100)]
    public string? City { get; set; }

    /// <summary>
    /// Street address.
    /// </summary>
    [MaxLength(255)]
    public string? Address { get; set; }

    /// <summary>
    /// Country name.
    /// </summary>
    [MaxLength(100)]
    public string? Country { get; set; }

    /// <summary>
    /// Postal code.
    /// </summary>
    [MaxLength(20)]
    public string? PostalCode { get; set; }

    /// <summary>
    /// Check-in time (e.g., "15:00").
    /// </summary>
    [MaxLength(10)]
    public string? CheckInTime { get; set; }

    /// <summary>
    /// Check-out time (e.g., "11:00").
    /// </summary>
    [MaxLength(10)]
    public string? CheckOutTime { get; set; }

    /// <summary>
    /// Whether the hotel is active and bookable.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<OwnedRoomType> RoomTypes { get; set; } = new List<OwnedRoomType>();
}
