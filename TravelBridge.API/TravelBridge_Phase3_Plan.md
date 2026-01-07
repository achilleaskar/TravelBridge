# TravelBridge — Phase 3 Plan (Owned Inventory ProviderId=0)

**Context:** Phase 2 introduced a provider abstraction (`TravelBridge.Providers.Abstractions`) and migrated WebHotelier availability/search to provider-neutral models + API mapping. Phase 3 adds **Owned inventory** as a new provider (`providerId = 0`) using the same abstraction so the existing endpoints work for `0-<OwnedHotelId>` with minimal/no API changes beyond DI.

---

## Goals (Phase 3)

1. **Owned provider works end-to-end** for:
   - `GetHotelInfoAsync`
   - `GetRoomInfoAsync`
   - `GetHotelAvailabilityAsync`
   - `GetAlternativesAsync`
   - `SearchAvailabilityAsync` (MVP)
2. Availability is based on **nightly unit counts** (simple + fast).
3. Admin/internal tooling exists to manage:
   - capacity (`totalUnits`)
   - stop-sell blocks (`closedUnits`)
4. Data model is built to evolve into Phase 4 (holds/confirm) and Phase 5 (rateplans/pricing/restrictions) without rewrites.

**Non-goals (Phase 3):**
- Full-blown rateplan engine (refundables/non-ref, occupancy pricing, min-stay/CTA/CTD). That’s Phase 5.
- Payments, confirmed reservations, holds/expiry (Phase 4). Phase 3 can keep `heldUnits/confirmedUnits` at 0 or allow manual setting for testing, but no real booking lifecycle yet unless you choose to pull a small subset of Phase 4 forward.

---

## Architecture guardrails (keep these)

- `TravelBridge.Providers.Owned` references **only** `TravelBridge.Providers.Abstractions`.
- Providers should **fetch + map**. Business rules that apply across providers stay in API (e.g., coupons, generic filtering).
- Composite ID format stays: `"{providerId}-{providerHotelId}"` and is split on the **first dash** only.

---

## Data model (MySQL) — Phase 3 baseline

### Core reference tables

#### `OwnedHotel`
- `HotelId` (BIGINT PK)
- `Name` (VARCHAR)
- `Latitude` (DECIMAL(10,7))
- `Longitude` (DECIMAL(10,7))
- `City` / `Address` (optional)
- `IsActive` (TINYINT)
- timestamps

#### `OwnedRoomType`
- `RoomTypeId` (BIGINT PK)
- `HotelId` (FK)
- `Code` (VARCHAR) — stable code used as `RoomCode`
- `Name` (VARCHAR)
- `MaxAdults` (INT), `MaxChildren` (INT) (optional but useful)
- `BasePricePerNight` (DECIMAL(10,2)) — Phase 3 pricing shortcut (can be replaced later)
- `IsActive` (TINYINT)
- timestamps

### Nightly inventory table (the “calendar”)

#### `OwnedInventoryDaily`
One row per **room type per night**.

- `RoomTypeId` (BIGINT, FK)
- `InvDate` (DATE)  **(night)**
- `TotalUnits` (INT)     — physical capacity for that night
- `ClosedUnits` (INT)    — admin stop-sell/blocks
- `HeldUnits` (INT)      — Phase 4 (can stay 0 for Phase 3)
- `ConfirmedUnits` (INT) — Phase 4 (can stay 0 for Phase 3)
- timestamps (optional)

**Primary key:** `(RoomTypeId, InvDate)`

**Derived availability:**
`AvailableUnits = TotalUnits - ClosedUnits - HeldUnits - ConfirmedUnits`

**Range semantics:** for a stay `[checkIn, checkOut)` you consume nights `checkIn .. checkOut-1`.

### Suggested indexes
- `OwnedRoomType(HotelId, IsActive)`
- `OwnedInventoryDaily(RoomTypeId, InvDate)` (PK covers)
- `OwnedHotel(IsActive)`

---

## Inventory seeding & maintenance

### Seed job (required)
Create rows for each active room type for a rolling window, e.g. **today → +400 days**.
- Inserts missing `(RoomTypeId, InvDate)` rows with defaults:
  - `TotalUnits` from room type capacity (you can store in `OwnedRoomType.DefaultTotalUnits`)
  - `ClosedUnits = 0`, `HeldUnits = 0`, `ConfirmedUnits = 0`

Run:
- on deployment
- and daily (extend window)

**Why:** makes availability queries simple/fast and supports reconciliation later.

---

## Admin / internal API endpoints (Phase 3)

Create internal endpoints (auth-protected) to manage inventory:

1. **Set capacity**
   - `PUT /admin/owned/inventory/{roomTypeId}/capacity`
   - body: `{ startDate, endDate, totalUnits }`
2. **Close/open units (stop-sell)**
   - `PUT /admin/owned/inventory/{roomTypeId}/closed`
   - body: `{ startDate, endDate, closedUnits }`
3. **Close entire hotel**
   - `PUT /admin/owned/inventory/hotel/{hotelId}/closed`
   - body: `{ startDate, endDate, closedUnits }` (usually = TotalUnits)
4. **Read inventory**
   - `GET /admin/owned/inventory/{roomTypeId}?startDate&endDate`

Validation rules:
- `0 <= ClosedUnits <= TotalUnits`
- `TotalUnits >= ClosedUnits + HeldUnits + ConfirmedUnits` (even if held/confirmed are 0 in Phase 3)

---

## Provider implementation (TravelBridge.Providers.Owned)

### Project
- `TravelBridge.Providers.Owned`
- Dependencies: Abstractions + a DB client (recommend: minimal DAL in API layer and inject an interface, OR put DB access in provider but avoid referencing API projects).

### ProviderId
- `ProviderIds.Owned = 0`
- `OwnedHotelProvider : IHotelProvider`

### Method behavior

#### `GetHotelInfoAsync(HotelInfoQuery)`
- Load `OwnedHotel` + related room types
- Map to `HotelInfoResult` (Abstractions)
- Fill:
  - hotel name, location, photos (can be empty in Phase 3)
  - basic room summaries if your contract expects them

#### `GetRoomInfoAsync(RoomInfoQuery)`
- Load `OwnedRoomType` by `HotelId + RoomId/RoomCode`
- Map to `RoomInfoResult`

#### `GetHotelAvailabilityAsync(HotelAvailabilityQuery)`
Inputs:
- `HotelId` (provider hotel id, not composite)
- `CheckIn`, `CheckOut`
- `Party` (including RoomsCount, Adults/Children)

Algorithm (Phase 3 nightly counts only):
1. Determine requested rooms = sum(RoomsCount over party items) or derived from your party model.
2. For each room type in the hotel:
   - query inventory rows for `[checkIn, checkOut)` (one row per night)
   - compute `minAvailableUnitsAcrossNights`
   - if `minAvailableUnitsAcrossNights >= requestedRooms` then it’s sellable
3. For each sellable room type, return one or more `RoomRateData`:
   - simplest: 1 rate per room type
   - `RateId` must be compatible with `FillPartyFromId()`:
     - `"{baseRateId}-{adults}[_{childAges}...]"` (no RoomsCount in id)
4. Pricing (Phase 3):
   - `BasePricePerNight * nights * roomsCount` (or sum per night if you store per-night prices)
   - Populate `Pricing/Retail` consistently (even if simplified)

Return:
- `HotelAvailabilityData` includes `HotelName`, `Location`, room list, alternatives empty (service will call `GetAlternativesAsync` when needed).

#### `GetAlternativesAsync(AlternativesQuery)`
MVP alternatives for owned:
- Search window: default 14 days (or your existing default)
- Find date ranges of `requestedNights` where `minAvailableUnitsAcrossNights >= requestedRooms`
- Compute price similarly
- Order by check-in date
- Return `AlternativeDateData` list

#### `SearchAvailabilityAsync(SearchAvailabilityQuery)`
MVP:
- Filter hotels by distance (lat/lon radius) or bounding box prefilter + haversine
- For each candidate hotel, check at least one room type has availability across range
- Produce `HotelSummaryData` with min price and rates summary (similar to WH accumulator logic)

---

## API wiring changes (expected to be small)

1. Register owned provider in DI:
   - `services.AddScoped<IHotelProvider, OwnedHotelProvider>();`
2. Ensure `HotelProviderResolver` sees it in the `IEnumerable<IHotelProvider>`
3. No endpoint changes expected if they already route by providerId.

---

## Tests (Phase 3)

### Unit tests
- CompositeId parse: `"0-123"` works; invalid formats return 400.
- Owned availability:
  - range consumes nights correctly (`[checkIn, checkOut)`)
  - fails if any night lacks units
  - RoomsCount scaling affects availability and totals
- Alternatives:
  - only returned when no rates (API service behavior)
  - correct RoomsCount weighting

### Integration tests (recommended)
- Seed 1 hotel + 1 room type + 30 inventory rows
- Call real endpoints:
  - `/hotelRoomAvailability` for `0-<id>`
  - multi-search includes owned hotel

---

## Definition of Done (Phase 3)

- ProviderId=0 fully supported for availability + alternatives + basic info endpoints.
- Admin endpoints allow manipulating `TotalUnits` and `ClosedUnits`.
- Seed job creates inventory rows for rolling window.
- Smoke test confirms:
  - availability works across ranges
  - alternatives appear when no rates
  - RoomsCount influences totals correctly

---

## Phase 3 Deliverables Checklist

- [ ] MySQL migration scripts for `OwnedHotel`, `OwnedRoomType`, `OwnedInventoryDaily`
- [ ] Seed job (command or hosted service)
- [ ] Admin endpoints + validation
- [ ] `TravelBridge.Providers.Owned` provider implementation
- [ ] DI registration + resolver routing
- [ ] Unit + integration tests
- [ ] Basic sample data script for local dev
