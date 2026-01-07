# TravelBridge — Roadmap After Phase 2 (Phase 3+)

This document is a forward plan for implementing Owned inventory and then evolving TravelBridge into a robust multi-provider booking platform.

---

## Phase 3 — Owned Inventory Provider (Nightly counts MVP)

**Goal:** providerId=0 works for hotel/room info, availability, alternatives, and (MVP) destination search.  
**Core:** nightly inventory rows per room type (`OwnedInventoryDaily`).

Deliverables:
- MySQL schema + seed job
- Admin endpoints for capacity/stop-sell
- `TravelBridge.Providers.Owned` implementation
- Tests + smoke tests

(See dedicated Phase 3 plan document.)

---

## Phase 4 — Holds, Confirmation, and Concurrency Safety

**Goal:** prevent overbooking under concurrency and support booking lifecycle transitions.

### 4.1 Source-of-truth tables
- `OwnedHold` + `OwnedHoldItem` (expires)
- `OwnedReservation` + `OwnedReservationItem` (confirmed/canceled)

### 4.2 Transactional flows
All must run in a DB transaction and lock inventory rows for the date range:
- Place hold: validate availability → `HeldUnits += qty`
- Confirm booking: `HeldUnits -= qty`, `ConfirmedUnits += qty`, create reservation
- Cancel hold / expire hold: `HeldUnits -= qty`
- Cancel reservation: `ConfirmedUnits -= qty`

### 4.3 Expiry + cleanup
- Background job marks holds expired and releases held units.
- Ensure idempotency (job can re-run safely).

### 4.4 Reconciliation (self-healing)
- Scheduled job recomputes `HeldUnits`/`ConfirmedUnits` from holds/reservations and overwrites counters for a rolling window.
- Alert on mismatches beyond threshold.

**DoD (Phase 4):**
- Concurrency test: two threads cannot overbook last unit.
- Holds expire and release units.
- Reconciliation job runs and fixes drift.

---

## Phase 5 — Pricing, RatePlans, and Restrictions

**Goal:** owned inventory supports realistic hotel pricing and selling rules.

### 5.1 RatePlans
Tables:
- `OwnedRatePlan` (Refundable, Non-refundable, Breakfast included, etc.)
- `OwnedRatePlanRoomType` (which plans apply to which room types)

### 5.2 Per-night pricing
Tables:
- `OwnedRatePlanPriceDaily` (RoomTypeId, RatePlanId, Date, BasePrice, Currency)
Optional:
- `OwnedSeason`, `OwnedPriceRule` for bulk generation

### 5.3 Restrictions / selling rules
Tables:
- `OwnedRestrictionsDaily`:
  - MinStay / MaxStay
  - CTA (Closed to Arrival) / CTD (Closed to Departure)
  - StopSell (rateplan-level)
  - MaxOccupancy overrides, etc.

### 5.4 Occupancy pricing rules
Approaches:
- Simple: `ExtraAdultFee`, `ExtraChildFee`, age bands
- Advanced: tiered occupancy grids

**DoD (Phase 5):**
- Multiple rateplans returned per room type
- Restrictions enforced during availability calculation
- Output remains provider-neutral; API mapping stays stable

---

## Phase 6 — Multi-provider Destination Search & Merging

**Goal:** search endpoints query multiple providers and merge results consistently.

Key work:
- parallel provider calls with cancellation + timeouts
- merge by:
  - provider-specific ids (no collisions)
  - consistent sorting + stable deterministic totals
- caching:
  - hotel info: long TTL
  - availability: short TTL (seconds/minutes) if safe

**DoD (Phase 6):**
- Search returns mix of WH + Owned hotels
- Clear observability of per-provider latencies and failures

---

## Phase 7 — Observability, Performance, and Reliability

Instrumentation:
- metrics: request duration, provider duration, success/failure
- structured logs: providerId, providerHotelId, correlation id
- tracing across API → provider calls
- error budgets + alert rules

Performance:
- load tests for:
  - search with many hotels
  - availability with many parties
  - hold/confirm concurrency
- DB indexes + query plans validated
- caching strategy validated

**DoD (Phase 7):**
- dashboards + alerts in place
- load test results meet targets

---

## Phase 8 — Admin UI / Content Management / WordPress Sync (Optional)

Depending on where “truth” for hotel content lives:

Options:
- A) Owned DB is source of truth → admin UI to manage hotels/rooms/photos
- B) WordPress is source of truth → sync WP → Owned tables
- C) Hybrid → WP for content, TravelBridge for inventory/pricing

Deliverables:
- import/sync jobs
- mapping rules (stable ids)
- conflict handling + audit logs

---

## Phase 9 — Booking Engine & External Integrations (Optional)

- payment integration (Stripe/etc.)
- invoices/receipts
- cancellation policies enforcement and fees
- channel manager integrations (push confirmed bookings / pull inventory)

---

## Cross-cutting principles (apply to all phases)

- Keep provider contracts stable (Abstractions evolve carefully).
- Prefer additive changes; avoid breaking composite ids.
- Maintain deterministic ids and stable sorting/aggregation logic.
- Favor idempotent operations for holds/confirmations and jobs.
- Treat reconciliation as a safety net, not the main logic path.

---

## Suggested implementation order

1) Phase 3 (Owned provider + inventory + admin + seed)
2) Phase 4 (holds/confirm + reconciliation)
3) Phase 5 (rateplans + pricing + restrictions)
4) Phase 6 (merged multi-provider search)
5) Phase 7 (observability + perf hardening)
6) Phase 8/9 as business requires
