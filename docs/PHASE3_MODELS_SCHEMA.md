# Phase 3: Owned Inventory - Models & Schema Design

## Overview

This document defines all data models, entities, and interfaces for the Owned Inventory provider (Phase 3).

---

## Database Schema

### 1. OwnedHotel Entity

**Table:** `OwnedHotels`

```csharp
public class OwnedHotel : BaseModel
{
    public int Id { get; set; }                          // PK, auto-increment
    public required string Code { get; set; }             // VARCHAR(50), unique - used in composite IDs
    public required string Name { get; set; }             // VARCHAR(255)
    public string? Description { get; set; }              // TEXT
    public string? Type { get; set; }                     // VARCHAR(100) - "Hotel", "Resort", "Villa"
    public int? Rating { get; set; }                      // INT (1-5)
    public decimal Latitude { get; set; }                 // DECIMAL(10,7)
    public decimal Longitude { get; set; }                // DECIMAL(10,7)
    public string? City { get; set; }                     // VARCHAR(100)
    public string? Address { get; set; }                  // VARCHAR(255)
    public string? Country { get; set; }                  // VARCHAR(100)
    public string? PostalCode { get; set; }               // VARCHAR(20)
    public string? CheckInTime { get; set; }              // VARCHAR(10) - "15:00"
    public string? CheckOutTime { get; set; }             // VARCHAR(10) - "11:00"
    public bool IsActive { get; set; }                    // TINYINT (1 = active, 0 = inactive)
    
    // Navigation
    public ICollection<OwnedRoomType> RoomTypes { get; set; }
}
```

**Indexes:**
- `IX_OwnedHotel_Code` (unique)
- `IX_OwnedHotel_IsActive`
- `IX_OwnedHotel_Location` (Latitude, Longitude) - for bounding box queries

---

### 2. OwnedRoomType Entity

**Table:** `OwnedRoomTypes`

```csharp
public class OwnedRoomType : BaseModel
{
    public int Id { get; set; }                          // PK, auto-increment
    public int HotelId { get; set; }                     // FK to OwnedHotel
    public required string Code { get; set; }             // VARCHAR(50) - stable code for room type
    public required string Name { get; set; }             // VARCHAR(255) - "Deluxe Double Room"
    public string? Description { get; set; }              // TEXT
    public int MaxAdults { get; set; }                    // INT - max occupancy adults
    public int MaxChildren { get; set; }                  // INT - max occupancy children
    public int MaxTotalOccupancy { get; set; }            // INT - max total guests
    public decimal BasePricePerNight { get; set; }        // DECIMAL(10,2) - fallback price
    public bool IsActive { get; set; }                    // TINYINT (1 = active, 0 = inactive)
    
    // Navigation
    public OwnedHotel Hotel { get; set; }
    public ICollection<OwnedInventoryDaily> InventoryDays { get; set; }
}
```

**Indexes:**
- `IX_OwnedRoomType_HotelId`
- `IX_OwnedRoomType_HotelId_Code` (unique composite)
- `IX_OwnedRoomType_IsActive`

---

### 3. OwnedInventoryDaily Entity

**Table:** `OwnedInventoryDaily`

**Primary Key:** Composite `(RoomTypeId, Date)`

```csharp
public class OwnedInventoryDaily
{
    public int RoomTypeId { get; set; }                  // FK to OwnedRoomType (part of PK)
    public DateOnly Date { get; set; }                   // DATE (part of PK) - the night being sold
    
    // Inventory Counters
    public int TotalUnits { get; set; }                  // Total physical rooms available
    public int ClosedUnits { get; set; }                 // Admin stop-sell (maintenance, blocks)
    public int HeldUnits { get; set; }                   // Temporarily reserved (Phase 4)
    public int ConfirmedUnits { get; set; }              // Confirmed bookings (Phase 4)
    
    // Pricing (Phase 3)
    public decimal? PricePerNight { get; set; }          // DECIMAL(10,2), nullable - override price
    
    // Audit
    public DateTime? LastModifiedUtc { get; set; }       // DATETIME(6) - when last changed
    
    // Navigation
    public OwnedRoomType RoomType { get; set; }
    
    // Computed property (not mapped to DB)
    public int AvailableUnits => TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits;
}
```

**Indexes:**
- `PK_OwnedInventoryDaily` (RoomTypeId, Date)
- `IX_OwnedInventoryDaily_Date` - for date range queries

**Constraints:**
- `0 <= ClosedUnits <= TotalUnits`
- `0 <= HeldUnits`
- `0 <= ConfirmedUnits`
- `ClosedUnits + HeldUnits + ConfirmedUnits <= TotalUnits`

---

## Provider Store Interface

### IOwnedInventoryStore

**Location:** `TravelBridge.Providers.Abstractions/Store/IOwnedInventoryStore.cs`

```csharp
namespace TravelBridge.Providers.Abstractions.Store;

/// <summary>
/// Data access interface for owned hotel inventory.
/// Implemented in API layer with EF Core.
/// </summary>
public interface IOwnedInventoryStore
{
    // Hotel queries
    Task<OwnedHotelStoreModel?> GetHotelByIdAsync(int hotelId, CancellationToken ct = default);
    Task<OwnedHotelStoreModel?> GetHotelByCodeAsync(string hotelCode, CancellationToken ct = default);
    Task<List<OwnedHotelStoreModel>> SearchHotelsInBoundingBoxAsync(
        decimal minLat, decimal maxLat, 
        decimal minLon, decimal maxLon, 
        bool activeOnly = true,
        CancellationToken ct = default);
    
    // Room type queries
    Task<OwnedRoomTypeStoreModel?> GetRoomTypeByIdAsync(int roomTypeId, CancellationToken ct = default);
    Task<OwnedRoomTypeStoreModel?> GetRoomTypeByCodeAsync(int hotelId, string roomCode, CancellationToken ct = default);
    Task<List<OwnedRoomTypeStoreModel>> GetRoomTypesByHotelIdAsync(int hotelId, bool activeOnly = true, CancellationToken ct = default);
    
    // Inventory queries
    Task<List<OwnedInventoryDailyStoreModel>> GetInventoryAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken ct = default);
    
    Task<Dictionary<int, List<OwnedInventoryDailyStoreModel>>> GetInventoryForMultipleRoomTypesAsync(
        List<int> roomTypeIds, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken ct = default);
    
    // Admin operations
    Task UpdateInventoryCapacityAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDate, 
        int totalUnits, 
        CancellationToken ct = default);
    
    Task UpdateInventoryClosedUnitsAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDate, 
        int closedUnits, 
        CancellationToken ct = default);
    
    Task EnsureInventoryExistsAsync(
        int roomTypeId, 
        DateOnly startDate, 
        int days, 
        CancellationToken ct = default);
}
```

---

## Store Models (DTOs)

### OwnedHotelStoreModel

```csharp
public sealed record OwnedHotelStoreModel
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Type { get; init; }
    public int? Rating { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? City { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? CheckInTime { get; init; }
    public string? CheckOutTime { get; init; }
    public List<OwnedRoomTypeStoreModel> RoomTypes { get; init; } = [];
}
```

### OwnedRoomTypeStoreModel

```csharp
public sealed record OwnedRoomTypeStoreModel
{
    public int Id { get; init; }
    public int HotelId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public int MaxAdults { get; init; }
    public int MaxChildren { get; init; }
    public int MaxTotalOccupancy { get; init; }
    public decimal BasePricePerNight { get; init; }
}
```

### OwnedInventoryDailyStoreModel

```csharp
public sealed record OwnedInventoryDailyStoreModel
{
    public int RoomTypeId { get; init; }
    public DateOnly Date { get; init; }
    public int TotalUnits { get; init; }
    public int ClosedUnits { get; init; }
    public int HeldUnits { get; init; }
    public int ConfirmedUnits { get; init; }
    public decimal? PricePerNight { get; init; }
    
    public int AvailableUnits => TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits;
}
```

---

## Rate ID Format

For owned inventory, rate IDs follow this format:

```
rt_{roomTypeId}-{adults}[_{childAge1}_{childAge2}...]
```

**Examples:**
- `rt_5-2` → Room type 5, 2 adults
- `rt_5-2_5_10` → Room type 5, 2 adults, children ages 5 and 10
- `rt_12-10_3_7_9` → Room type 12, 10 adults, children ages 3, 7, 9

**Parsing Logic (reuses existing `FillPartyFromId`):**
```csharp
// Input: "rt_5-2_5_10"
// After split on first '-': baseId = "rt_5", suffix = "2_5_10"
// Parse suffix: adults = 2, childrenAges = [5, 10]
```

---

## Pricing Logic (Phase 3)

### Price Resolution

```csharp
public decimal GetPriceForNight(OwnedInventoryDailyStoreModel inventory, OwnedRoomTypeStoreModel roomType)
{
    // Use per-night override if set, otherwise fallback to room type base price
    return inventory.PricePerNight ?? roomType.BasePricePerNight;
}
```

### Total Price Calculation

```csharp
public decimal CalculateTotalPrice(
    List<OwnedInventoryDailyStoreModel> inventoryDays,
    OwnedRoomTypeStoreModel roomType,
    int requestedRooms)
{
    decimal totalPrice = 0m;
    
    foreach (var inv in inventoryDays)
    {
        var pricePerNight = inv.PricePerNight ?? roomType.BasePricePerNight;
        totalPrice += pricePerNight * requestedRooms;
    }
    
    return totalPrice;
}
```

---

## Availability Logic

### Requested Rooms Calculation

```csharp
public int CalculateRequestedRooms(PartyConfiguration party)
{
    // Sum of all rooms in the party
    return party.RoomCount;
}
```

### Availability Check

```csharp
public bool IsAvailable(
    List<OwnedInventoryDailyStoreModel> inventoryDays, 
    int requestedRooms)
{
    // Every night in the range must have enough available units
    return inventoryDays.All(inv => inv.AvailableUnits >= requestedRooms);
}
```

### Min Available Units

```csharp
public int GetMinAvailableUnits(List<OwnedInventoryDailyStoreModel> inventoryDays)
{
    return inventoryDays.Min(inv => inv.AvailableUnits);
}
```

---

## Admin Endpoint Models

### SetCapacityRequest

```csharp
public sealed record SetCapacityRequest
{
    public required int RoomTypeId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int TotalUnits { get; init; }
}
```

### SetClosedUnitsRequest

```csharp
public sealed record SetClosedUnitsRequest
{
    public required int RoomTypeId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
    public required int ClosedUnits { get; init; }
}
```

### CloseHotelRequest

```csharp
public sealed record CloseHotelRequest
{
    public required int HotelId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}
```

### GetInventoryRequest

```csharp
public sealed record GetInventoryRequest
{
    public required int RoomTypeId { get; init; }
    public required DateOnly StartDate { get; init; }
    public required DateOnly EndDate { get; init; }
}
```

---

## Seed Data (Development)

### Sample Hotel

```json
{
  "code": "OWNTEST01",
  "name": "Test Owned Hotel",
  "description": "A sample owned hotel for development testing",
  "type": "Hotel",
  "rating": 4,
  "latitude": 38.2466,
  "longitude": 21.7346,
  "city": "Patras",
  "address": "123 Test Street",
  "country": "Greece",
  "postalCode": "26221",
  "checkInTime": "15:00",
  "checkOutTime": "11:00",
  "isActive": true
}
```

### Sample Room Types

```json
[
  {
    "code": "STDDBL",
    "name": "Standard Double Room",
    "description": "Comfortable double room with city view",
    "maxAdults": 2,
    "maxChildren": 1,
    "maxTotalOccupancy": 3,
    "basePricePerNight": 80.00
  },
  {
    "code": "DLXFAM",
    "name": "Deluxe Family Room",
    "description": "Spacious family room with balcony",
    "maxAdults": 2,
    "maxChildren": 2,
    "maxTotalOccupancy": 4,
    "basePricePerNight": 120.00
  }
]
```

### Sample Inventory (next 400 days)

- **TotalUnits**: 10 (Standard Double), 5 (Deluxe Family)
- **ClosedUnits**: 0
- **HeldUnits**: 0
- **ConfirmedUnits**: 0
- **PricePerNight**: null (uses base price)

---

## Migration Summary

**Migration Name:** `AddOwnedInventoryTables`

**Tables Created:**
1. `OwnedHotels`
2. `OwnedRoomTypes`
3. `OwnedInventoryDaily`

**Foreign Keys:**
- `OwnedRoomTypes.HotelId` → `OwnedHotels.Id` (CASCADE)
- `OwnedInventoryDaily.RoomTypeId` → `OwnedRoomTypes.Id` (CASCADE)

**Indexes:**
- See individual entity sections above

---

## Validation Rules

### OwnedHotel
- `Code` must be unique
- `Code` cannot contain `-` (reserved for composite ID separator)
- `Latitude` range: -90 to 90
- `Longitude` range: -180 to 180
- `Rating` range: 0 to 5

### OwnedRoomType
- `(HotelId, Code)` must be unique
- `MaxAdults` >= 1
- `MaxChildren` >= 0
- `MaxTotalOccupancy` >= MaxAdults
- `BasePricePerNight` > 0

### OwnedInventoryDaily
- `TotalUnits` >= 0
- `ClosedUnits` >= 0 and <= TotalUnits
- `HeldUnits` >= 0
- `ConfirmedUnits` >= 0
- `ClosedUnits + HeldUnits + ConfirmedUnits <= TotalUnits`

---

## Next Steps

After reviewing this schema, we'll proceed with:

1. ✅ Creating EF entities in `TravelBridge.API/Models/DB/`
2. ✅ Updating `AppDbContext`
3. ✅ Creating EF migration
4. ✅ Implementing store interface and repository
5. ✅ Building the provider
6. ✅ Adding admin endpoints
7. ✅ Creating seed service
8. ✅ Testing

---

**Document Version:** 1.0  
**Last Updated:** Phase 3 Planning  
**Status:** Ready for Implementation
