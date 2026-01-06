# Owned Rooms Feature - Implementation Plan

> **Document Purpose**: Master plan for implementing owned hotel inventory management in TravelBridge.
> 
> **Created**: January 2025
> 
> **Status**: Phase 1 In Progress

---

## Overview

TravelBridge currently fetches hotel availability from WebHotelier (external provider). This feature extends the system to manage and sell our own hotel rooms directly, with full inventory and pricing control.

### Goals
- Support multiple availability sources (WebHotelier, Owned inventory, future providers)
- Full inventory management for owned hotels (room types, pricing, availability calendar)
- Admin API for hotel owners to manage their properties
- Prevent double-bookings with hold/confirm flow
- Optional WordPress integration for hotel content sync

### Critical Constraint
**DO NOT change what Frontend reads or sends.** All existing API contracts in `TravelBridge.Contracts` must remain unchanged. The abstraction layer is internal plumbing only.

---

## Architecture Approach

### Project Dependencies (Critical Rule)

```
TravelBridge.Contracts              → Zero dependencies (API DTOs only - DO NOT CHANGE)
TravelBridge.Providers.Abstractions → Zero dependencies (provider interfaces + queries)
TravelBridge.Providers.WebHotelier  → References Abstractions ONLY (not Contracts!)
TravelBridge.Application            → Zero dependencies (business logic, future use)
TravelBridge.API                    → References Contracts + Abstractions + Providers + Application
```

### Layer Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Endpoints                           │
│     (SearchPluginEndpoints, HotelEndpoint, AdminEndpoints)      │
│                                                                 │
│     Maps: Contracts DTO → Abstractions Query → Provider         │
│     Returns: Provider Result → Contracts DTO (unchanged for FE) │
└─────────────────────────────────────────────────────────────────┘
                                │
            ┌───────────────────┼───────────────────┐
            ▼                   ▼                   ▼
┌───────────────────┐ ┌─────────────────┐ ┌─────────────────────┐
│ TravelBridge.     │ │ TravelBridge.   │ │ TravelBridge.       │
│ Contracts         │ │ Providers.      │ │ Application         │
│ (API DTOs)        │ │ Abstractions    │ │ (Business Logic)    │
│ ❌ DO NOT CHANGE  │ │ (Interfaces)    │ │                     │
└───────────────────┘ └─────────────────┘ └─────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                ▼                               ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│  WebHotelierHotelProvider │   │  OwnedInventoryProvider   │
│  (implements IHotelProvider)│ │  (Phase 3)                │
└───────────────────────────┘   └───────────────────────────┘
                │                               │
                ▼                               ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│   WebHotelier API         │   │   MySQL Database          │
│   (External)              │   │   (Owned Inventory)       │
└───────────────────────────┘   └───────────────────────────┘
```

### Hotel ID Format

Use prefix format for composite IDs (safer than splitting on `-`):
- WebHotelier: `wh:VAROSRESID`
- Owned: `owned:123`

Parser splits on first `:` only, handles IDs containing special characters.

**Note**: This is internal format. FE continues to receive IDs in whatever format they currently expect.

---

## Phase 1: Provider Abstraction Layer

**Goal**: Create a clean abstraction so the system can work with multiple availability sources without changing endpoint logic or FE contracts.

### New Project: TravelBridge.Providers.Abstractions

A tiny, zero-dependency project containing only:
- `AvailabilitySource` enum
- `IHotelProvider` interface
- Query/Result models
- `CompositeHotelId` value object

### Deliverables

| Component | Location | Purpose |
|-----------|----------|---------|
| `AvailabilitySource` enum | `Providers.Abstractions/` | Identifies provider: `Owned`, `WebHotelier` |
| `IHotelProvider` interface | `Providers.Abstractions/` | Unified interface for hotel operations |
| Query models | `Providers.Abstractions/Queries/` | Provider-neutral internal queries |
| Result models | `Providers.Abstractions/Results/` | Provider-neutral result types |
| `HotelProviderResolver` | `Providers.Abstractions/` | Resolves provider by source |
| `CompositeHotelId` | `Providers.Abstractions/` | Value object for parsing `wh:ID` format |
| `WebHotelierHotelProvider` | `Providers.WebHotelier/` | Implements `IHotelProvider` wrapping existing client |
| Refactored Endpoints | `TravelBridge.API/Endpoints/` | Use `IHotelProvider` via resolver |

### Interface Definition (Phase 1 - Read-Only)

Start with read-only operations. Booking methods will be added later when needed across providers.

```csharp
public interface IHotelProvider
{
    AvailabilitySource Source { get; }
    
    // Search operations
    Task<HotelSearchResult> SearchHotelsAsync(HotelSearchQuery query, CancellationToken ct = default);
    Task<IEnumerable<HotelSummary>> SearchPropertiesAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<HotelSummary>> GetAllPropertiesAsync(CancellationToken ct = default);
    
    // Single hotel operations
    Task<HotelAvailabilityResult> GetAvailabilityAsync(AvailabilityQuery query, CancellationToken ct = default);
    Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default);
    Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default);
}
```

**Note**: Booking operations (`CreateBookingAsync`, `CancelBookingAsync`) will be added in Phase 3 when OwnedInventoryProvider needs them.

### Resolver Design

Use `IEnumerable<IHotelProvider>` injection pattern:

```csharp
public class HotelProviderResolver
{
    private readonly Dictionary<AvailabilitySource, IHotelProvider> _providers;
    
    public HotelProviderResolver(IEnumerable<IHotelProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.Source);
    }
    
    public IHotelProvider GetProvider(AvailabilitySource source)
    {
        if (!_providers.TryGetValue(source, out var provider))
            throw new InvalidOperationException($"No provider registered for source: {source}");
        return provider;
    }
    
    public IHotelProvider GetProvider(CompositeHotelId hotelId) 
        => GetProvider(hotelId.Source);
}
```

This makes adding `OwnedInventoryProvider` later frictionless - just register it in DI.

### Query Models (in Abstractions)

```csharp
// Provider-neutral, no JSON attributes, no WebHotelier specifics
public record AvailabilityQuery(
    string ProviderHotelId,
    DateOnly CheckIn,
    DateOnly CheckOut,
    List<PartyConfiguration> Parties,
    string? CouponCode = null
);

public record HotelSearchQuery(
    DateOnly CheckIn,
    DateOnly CheckOut,
    List<PartyConfiguration> Parties,
    BoundingBox Location,
    string? SortBy = null,
    string? SortOrder = null
);

public record PartyConfiguration(int Adults, int[] ChildrenAges);
```

### Data Flow Example

```
FE sends:                  Existing request format (unchanged)
                                    ↓
API Endpoint receives:     MultiAvailabilityRequest (from Contracts - unchanged)
                                    ↓
Endpoint maps to:          HotelSearchQuery (Abstractions - internal)
                                    ↓
Resolver returns:          IHotelProvider (WebHotelier or Owned)
                                    ↓
Provider maps to:          WHAvailabilityRequest (WebHotelier-specific - internal)
                                    ↓
Provider returns:          HotelSearchResult (Abstractions - internal)
                                    ↓
Endpoint maps to:          PluginSearchResponse (Contracts - unchanged)
                                    ↓
FE receives:               Same response format as before (unchanged)
```

### Tests
- Unit tests: `HotelProviderResolver` logic
- Unit tests: `CompositeHotelId` parsing
- Integration tests: WebHotelier flow through abstraction (verify FE contract unchanged)

### Estimated Effort
~2-3 days

---

## Phase 2: Owned Inventory Domain Models & Database

**Goal**: Create the database schema for managing owned hotel inventory.

### New Entities

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `OwnedHotel` | Hotel we manage | `Id`, `Name`, `WordPressHotelId`, `Latitude`, `Longitude`, `Timezone`, `Currency` |
| `OwnedRoomType` | Room category | `Id`, `HotelId`, `Name`, `MaxAdults`, `MaxChildren`, `MaxOccupancy`, `BaseUnits` |
| `OwnedRatePlan` | Pricing/policy config | `Id`, `HotelId`, `Name`, `BoardType`, `CancellationPolicyJson`, `IsRefundable` |
| `DailyInventory` | Per-date availability | `RoomTypeId`, `Date`, `TotalUnits`, `HeldUnits`, `ConfirmedUnits`, `StopSell`, `MinStay` |
| `DailyRate` | Per-date pricing | `RatePlanId`, `RoomTypeId`, `Date`, `Price`, `ExtraAdultPrice`, `ExtraChildPrice` |
| `InventoryHold` | Temporary reservation | `Id`, `Token`, `ExpiresAt`, `Status`, `ReservationId` |

### Critical: Counter-Based Concurrency (MySQL/InnoDB)

Instead of computing `available = total - SUM(allocations)` at booking time, use counters:

```sql
-- DailyInventory table with counters
CREATE TABLE daily_inventory (
    room_type_id INT NOT NULL,
    date DATE NOT NULL,
    total_units INT NOT NULL,
    held_units INT NOT NULL DEFAULT 0,
    confirmed_units INT NOT NULL DEFAULT 0,
    stop_sell BOOLEAN NOT NULL DEFAULT FALSE,
    min_stay INT NOT NULL DEFAULT 1,
    PRIMARY KEY (room_type_id, date)
);
```

**Atomic hold creation** (no race conditions):
```sql
UPDATE daily_inventory
SET held_units = held_units + :qty
WHERE room_type_id = :rt
  AND date = :d
  AND (total_units - held_units - confirmed_units) >= :qty;
-- If affected_rows = 0, not enough availability
```

### Pre-generate Inventory Rows

**Critical**: Pre-generate `DailyInventory` rows for 18-24 months ahead per room type.
Without rows, there's nothing to lock/update atomically.

### Entity Relationships

```
OwnedHotel (1) ──────> (N) OwnedRoomType
OwnedHotel (1) ──────> (N) OwnedRatePlan

OwnedRoomType + Date ──────> DailyInventory (1:1 per date, pre-generated)
OwnedRoomType + OwnedRatePlan + Date ──────> DailyRate (1:1 per combo)

Reservation (1) ──────> (1) InventoryHold (for owned bookings)
```

### Tests
- Unit tests: Entity validation rules
- Integration tests: EF Core mappings

### Estimated Effort
~2 days

---

## Phase 3: Owned Inventory Provider Implementation

**Goal**: Implement availability calculation, search, and booking for owned hotels.

### Key Components

| Component | Purpose |
|-----------|---------|
| `OwnedInventoryHotelProvider` | Implements `IHotelProvider` for owned hotels |
| `InventoryService` | Manages holds, confirmations, expiry |
| `AvailabilityCalculator` | Computes available units from counters |

### Interface Extension (Phase 3)

Add booking methods to `IHotelProvider`:

```csharp
public interface IHotelProvider
{
    // ... existing read-only methods from Phase 1 ...
    
    // Booking operations (added in Phase 3)
    Task<BookingResult> CreateBookingAsync(CreateBookingCommand command, CancellationToken ct = default);
    Task<bool> CancelBookingAsync(CancelBookingCommand command, CancellationToken ct = default);
}
```

### Availability Calculation

```csharp
// Available = TotalUnits - HeldUnits - ConfirmedUnits
var available = dailyInventory.TotalUnits 
              - dailyInventory.HeldUnits 
              - dailyInventory.ConfirmedUnits;
```

For date range: `MinAvailable = MIN(available) across all nights`

**Important**: CheckIn=Jan10, CheckOut=Jan13 → allocate dates **10, 11, 12** (checkout night excluded)

### Booking Flow (Concurrency-Safe)

```
1. POST /checkout → Create Hold
   └── BEGIN TRANSACTION
   └── For each date (ascending order to avoid deadlocks):
       UPDATE daily_inventory SET held_units = held_units + :qty
       WHERE ... AND (total_units - held_units - confirmed_units) >= :qty
   └── If any UPDATE affects 0 rows → ROLLBACK, return "not available"
   └── INSERT inventory_hold with ExpiresAt = NOW + 10 minutes
   └── COMMIT

2. Payment processing...

3. POST /confirm → Convert Hold to Confirmed
   └── BEGIN TRANSACTION
   └── For each date:
       UPDATE daily_inventory 
       SET held_units = held_units - :qty,
           confirmed_units = confirmed_units + :qty
   └── UPDATE inventory_hold SET Status = Confirmed
   └── COMMIT

4. Background job (every minute):
   └── Find expired holds (ExpiresAt < NOW AND Status = Held)
   └── For each: decrement held_units, mark hold Expired
```

### Deadlock Handling

Even with date ordering, occasional deadlocks can occur. Retry strategy:
```csharp
for (int attempt = 0; attempt < 3; attempt++)
{
    try { return await ExecuteHoldTransaction(); }
    catch (MySqlException ex) when (ex.Number == 1213) // Deadlock
    { 
        await Task.Delay(50 * (attempt + 1)); 
    }
}
```

### Tests
- Unit tests: `AvailabilityCalculator` edge cases
- Integration tests: Hold/confirm/cancel flow
- Concurrency tests: Simultaneous booking attempts (verify no oversell)

### Estimated Effort
~4-5 days

---

## Phase 4: Admin API Endpoints

**Goal**: Create management endpoints for hotel owners/admins to control inventory.

### Endpoint Groups

#### Hotels Management
```
GET    /api/admin/hotels                    - List owned hotels
GET    /api/admin/hotels/{id}               - Get hotel details
POST   /api/admin/hotels                    - Create hotel
PUT    /api/admin/hotels/{id}               - Update hotel
DELETE /api/admin/hotels/{id}               - Delete hotel (soft delete)
```

#### Room Types Management
```
GET    /api/admin/hotels/{hotelId}/room-types
POST   /api/admin/hotels/{hotelId}/room-types
PUT    /api/admin/hotels/{hotelId}/room-types/{id}
DELETE /api/admin/hotels/{hotelId}/room-types/{id}
```

#### Rate Plans Management
```
GET    /api/admin/hotels/{hotelId}/rate-plans
POST   /api/admin/hotels/{hotelId}/rate-plans
PUT    /api/admin/hotels/{hotelId}/rate-plans/{id}
DELETE /api/admin/hotels/{hotelId}/rate-plans/{id}
```

#### Calendar Management (Bulk Operations)
```
GET    /api/admin/hotels/{hotelId}/calendar/inventory?roomTypeId=X&start=Y&end=Z
PUT    /api/admin/hotels/{hotelId}/calendar/inventory
       Body: { roomTypeId, startDate, endDate, totalUnits?, stopSell?, minStay? }

GET    /api/admin/hotels/{hotelId}/calendar/rates?roomTypeId=X&ratePlanId=Y&start=Z&end=W
PUT    /api/admin/hotels/{hotelId}/calendar/rates
       Body: { roomTypeId, ratePlanId, startDate, endDate, price, extraAdultPrice? }
```

#### Inventory Generation
```
POST   /api/admin/hotels/{hotelId}/room-types/{roomTypeId}/generate-inventory
       Body: { startDate, endDate }
       - Pre-generates DailyInventory rows for the date range
```

### Tests
- Integration tests: CRUD operations
- Validation tests: Date ranges, business rules
- Authorization tests: Admin-only access

### Estimated Effort
~3-4 days

---

## Phase Dependencies

```
Phase 1 (Abstraction)     ← CURRENT
    │
    ▼
Phase 2 (Domain Models)
    │
    ▼
Phase 3 (Provider Logic)
    │
    ▼
Phase 4 (Admin API)
```

Each phase is independently deployable and testable.

---

## Technical Decisions Log

| Decision | Rationale | Date |
|----------|-----------|------|
| New `Providers.Abstractions` project | Keeps abstractions truly standalone, no risk of inheriting future Application dependencies | Jan 2025 |
| Provider abstractions NOT in Contracts | Keeps Contracts for API DTOs only, WebHotelier stays standalone | Jan 2025 |
| Single `IHotelProvider` interface | Clearer for developers, not many functions | Jan 2025 |
| Read-only interface in Phase 1 | Don't implement booking methods until needed in Phase 3 | Jan 2025 |
| Resolver uses `IEnumerable<IHotelProvider>` | Makes adding new providers frictionless | Jan 2025 |
| Hotel ID format: `wh:ID` / `owned:ID` | Safer than splitting on `-`, handles IDs with special chars | Jan 2025 |
| Counter-based inventory (`held_units`, `confirmed_units`) | Atomic MySQL updates, no SUM computation at booking time | Jan 2025 |
| Pre-generate DailyInventory rows | Ensures rows exist for locking, prevents "sparse data" footgun | Jan 2025 |
| Update dates in ascending order | Reduces deadlocks in MySQL | Jan 2025 |
| Separate test projects (Unit/Integration) | Project will grow, different test setup needs | Jan 2025 |

---

## Future Considerations

- **Caching**: Cache availability for frequently searched dates
- **Background Jobs**: Hold expiration cleanup, inventory row generation
- **Webhooks**: Notify external systems on booking confirmed/cancelled
- **Multi-currency**: Store prices in multiple currencies
- **Channel Manager Integration**: Sync inventory with OTAs

---

## Changelog

| Date | Change |
|------|--------|
| Jan 2025 | Initial plan created |
| Jan 2025 | Revised: Move provider abstractions from Contracts to Application |
| Jan 2025 | Revised: Create new `Providers.Abstractions` project instead of using Application |
| Jan 2025 | Added: Counter-based inventory pattern for MySQL concurrency |
| Jan 2025 | Added: Pre-generate DailyInventory rows requirement |
| Jan 2025 | Changed: Hotel ID format to `wh:ID` prefix style |
| Jan 2025 | Added: Critical constraint - DO NOT change FE contracts |
| Jan 2025 | Changed: IHotelProvider is read-only in Phase 1, booking added in Phase 3 |
| Jan 2025 | Added: Resolver uses IEnumerable pattern for easy provider registration |
