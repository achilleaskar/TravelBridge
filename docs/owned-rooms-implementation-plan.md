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

---

## Architecture Approach

```
┌─────────────────────────────────────────────────────────────────┐
│                         API Endpoints                           │
│     (SearchPluginEndpoints, HotelEndpoint, AdminEndpoints)      │
└─────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    HotelProviderResolver                        │
│         (Determines provider by AvailabilitySource enum)        │
└─────────────────────────────────────────────────────────────────┘
                                │
                ┌───────────────┴───────────────┐
                ▼                               ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│   IHotelProvider          │   │   IHotelProvider          │
│   (WebHotelier)           │   │   (OwnedInventory)        │
└───────────────────────────┘   └───────────────────────────┘
                │                               │
                ▼                               ▼
┌───────────────────────────┐   ┌───────────────────────────┐
│   WebHotelier API         │   │   MySQL Database          │
│   (External)              │   │   (Owned Inventory)       │
└───────────────────────────┘   └───────────────────────────┘
```

---

## Phase 1: Provider Abstraction Layer

**Goal**: Create a clean abstraction so the system can work with multiple availability sources without changing endpoint logic.

### Deliverables

| Component | Location | Purpose |
|-----------|----------|---------|
| `AvailabilitySource` enum | `TravelBridge.Contracts/Providers/` | Identifies provider: `Owned`, `WebHotelier` |
| `IHotelProvider` interface | `TravelBridge.Contracts/Providers/` | Unified interface for all hotel operations |
| Provider DTOs | `TravelBridge.Contracts/Providers/` | Provider-agnostic request/response types |
| `WebHotelierHotelProvider` | `TravelBridge.Providers.WebHotelier/` | Implements `IHotelProvider` wrapping existing service |
| `HotelProviderResolver` | `TravelBridge.API/Services/` | Resolves provider by `AvailabilitySource` + hotel ID |
| Refactored Endpoints | `TravelBridge.API/Endpoints/` | Use `IHotelProvider` instead of direct service calls |

### Interface Definition (Preview)

```csharp
public interface IHotelProvider
{
    AvailabilitySource Source { get; }
    
    // Search operations
    Task<HotelSearchResult> SearchHotelsAsync(HotelSearchRequest request, CancellationToken ct = default);
    Task<IEnumerable<AutoCompleteHotel>> SearchPropertiesAsync(string searchTerm, CancellationToken ct = default);
    Task<IEnumerable<AutoCompleteHotel>> GetAllPropertiesAsync(CancellationToken ct = default);
    
    // Single hotel operations
    Task<HotelAvailabilityResult> GetAvailabilityAsync(HotelAvailabilityRequest request, CancellationToken ct = default);
    Task<HotelInfoResult> GetHotelInfoAsync(string hotelId, CancellationToken ct = default);
    Task<RoomInfoResult> GetRoomInfoAsync(string hotelId, string roomId, CancellationToken ct = default);
    
    // Booking operations
    Task<BookingResult> CreateBookingAsync(BookingRequest request, CancellationToken ct = default);
    Task<bool> CancelBookingAsync(int reservationId, CancellationToken ct = default);
}
```

### Tests
- Unit tests: `HotelProviderResolver` logic
- Integration tests: WebHotelier flow through abstraction

### Estimated Effort
~2-3 days

---

## Phase 2: Owned Inventory Domain Models & Database

**Goal**: Create the database schema for managing owned hotel inventory.

### New Entities

| Entity | Purpose | Key Fields |
|--------|---------|------------|
| `OwnedHotel` | Hotel we manage | `Id`, `Name`, `WordPressHotelId`, `Location`, `Timezone`, `Currency` |
| `OwnedRoomType` | Room category | `Id`, `HotelId`, `Name`, `MaxAdults`, `MaxChildren`, `MaxOccupancy`, `BaseUnits` |
| `OwnedRatePlan` | Pricing/policy config | `Id`, `HotelId`, `Name`, `BoardType`, `CancellationPolicyJson`, `IsRefundable` |
| `DailyInventory` | Per-date availability | `HotelId`, `RoomTypeId`, `Date`, `TotalUnits`, `StopSell`, `MinStay`, `MaxStay` |
| `DailyRate` | Per-date pricing | `RatePlanId`, `RoomTypeId`, `Date`, `Price`, `ExtraAdultPrice`, `ExtraChildPrice` |
| `InventoryHold` | Temporary reservation | `Id`, `Token`, `ExpiresAt`, `Status`, `ReservationId` |
| `InventoryAllocation` | Booking consuming inventory | `ReservationId`, `RoomTypeId`, `Date`, `Quantity`, `Status` |

### Entity Relationships

```
OwnedHotel (1) ──────> (N) OwnedRoomType
OwnedHotel (1) ──────> (N) OwnedRatePlan

OwnedRoomType + Date ──────> DailyInventory (1:1 per date)
OwnedRoomType + OwnedRatePlan + Date ──────> DailyRate (1:1 per combo)

Reservation (1) ──────> (N) InventoryAllocation
InventoryHold (1) ──────> (1) Reservation (optional, for pending bookings)
```

### Database Considerations
- Composite indexes on `(RoomTypeId, Date)` for calendar queries
- `DailyInventory` and `DailyRate` may have sparse data (only store overrides from defaults)
- Consider partitioning by date for large datasets (future optimization)

### Tests
- Unit tests: Entity validation rules
- Integration tests: EF Core mappings, cascade deletes

### Estimated Effort
~2 days

---

## Phase 3: Owned Inventory Provider Implementation

**Goal**: Implement availability calculation, search, and booking for owned hotels.

### Key Components

| Component | Purpose |
|-----------|---------|
| `OwnedInventoryHotelProvider` | Implements `IHotelProvider` for owned hotels |
| `InventoryCalculator` | Calculates available units per date range |
| `InventoryHoldService` | Manages temporary holds with TTL |
| `OwnedBookingService` | Converts holds to confirmed allocations |

### Availability Calculation

```
AvailableUnits(RoomType, Date) = 
    DailyInventory.TotalUnits (or RoomType.BaseUnits if no override)
    - SUM(InventoryAllocation.Quantity WHERE Status IN [Held, Confirmed])
```

For a date range search:
```
MinAvailable = MIN(AvailableUnits) across all dates in range
```

### Search Flow
1. Query `OwnedHotel` by location/bbox
2. For each hotel, get room types matching guest capacity
3. Calculate min available units across requested dates
4. Filter by: `StopSell = false`, `MinStay <= nights`, capacity rules
5. Get pricing from `DailyRate` (sum across dates)
6. Return results in same format as WebHotelier

### Booking Flow (Double-Booking Prevention)

```
1. POST /checkout → Create InventoryHold (10-min TTL)
   └── INSERT InventoryAllocation with Status = Held (in transaction)
   └── Verify availability BEFORE insert (SELECT FOR UPDATE or optimistic concurrency)

2. Payment processing...

3. POST /confirm → Convert Hold to Confirmed
   └── UPDATE InventoryAllocation SET Status = Confirmed
   └── UPDATE InventoryHold SET Status = Confirmed

4. On hold expiry (background job):
   └── DELETE InventoryAllocation WHERE Status = Held AND Hold.ExpiresAt < NOW()
```

### Tests
- Unit tests: `InventoryCalculator` (edge cases, multiple rooms, date boundaries)
- Unit tests: Availability filtering (capacity, stop-sell, min-stay)
- Integration tests: Hold/confirm/cancel flow
- Concurrency tests: Simultaneous booking attempts

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

#### WordPress Sync (Optional)
```
POST   /api/admin/hotels/sync-from-wordpress
       Body: { wordpressHotelId }
       - Imports hotel info (name, description, photos, location)
       - Creates OwnedHotel record with mapping
```

### Authorization
- All `/api/admin/*` endpoints require admin authentication
- Consider per-hotel authorization for multi-tenant scenarios

### Tests
- Integration tests: CRUD operations
- Validation tests: Date ranges, business rules, required fields
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
| Single `IHotelProvider` interface | Clearer for developers, not many functions | Jan 2025 |
| Interfaces in `TravelBridge.Contracts/Providers/` | Avoids new Core project, keeps structure simple | Jan 2025 |
| `HotelProviderResolver` accepts enum + hotel ID | Same ID might exist in multiple providers | Jan 2025 |
| Provider enum: `Owned`, `WebHotelier` | Clear naming, extensible for future providers | Jan 2025 |
| Separate test projects (Unit/Integration) | Project will grow, different test setup needs | Jan 2025 |

---

## Future Considerations

- **Caching**: Cache `DailyInventory`/`DailyRate` for frequently accessed dates
- **Background Jobs**: Hold expiration cleanup, rate import from external sources
- **Webhooks**: Notify external systems on booking confirmed/cancelled
- **Multi-currency**: Store prices in multiple currencies or convert on-the-fly
- **Channel Manager Integration**: Sync inventory with OTAs (Booking.com, Expedia)

---

## Changelog

| Date | Change |
|------|--------|
| Jan 2025 | Initial plan created |
