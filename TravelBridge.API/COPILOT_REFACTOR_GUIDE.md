Got it. I’ll base the plan on the **current structure in your zip**:

* `TravelBridge.API` (endpoints + DI + current `WebHotelierPropertiesService`)
* `TravelBridge.Providers.WebHotelier` (HTTP client + WH wire models, no Contracts dependency)
* `TravelBridge.Contracts` (public DTOs/enums used by the API)
* Tests: `TravelBridge.Tests` (MSTest)

And we **keep** the ID scheme exactly:

* `0-<hotelId>` = **Owned**
* `1-<hotelId>` = **WebHotelier**
* future providers: `2-...`, `3-...` etc
  **Do not change this format.**

Below is a carefully structured **Phase 1 plan** that gives the project the *ability* to support multiple providers, without creating any new provider implementations (no Owned provider yet, no WH adapter rewrite yet). It’s designed so Phase 2 can plug providers in with minimal churn.

---

## Phase 1 Goal

Introduce a **provider-agnostic abstraction layer** + **centralized composite-id parsing** so the codebase is ready to route requests by `providerId` later, while keeping current behavior unchanged.

### Non-goals in Phase 1

* No Owned inventory provider
* No moving WebHotelier logic around
* No changing endpoint URLs or response shapes
* No changing the `0-` / `1-` prefix format
* No performance regressions (avoid reflection, avoid repeated `Split`, avoid per-request allocations where we can)

---

# Step-by-step plan (Phase 1)

## Step 0 — Create a dedicated branch and freeze behavior

**Copilot rules**

* Do not alter endpoint signatures or output DTOs.
* All existing tests must pass.
* Any new code must be additive or low-risk refactor (e.g., replacing `Split('-')` with a faster/safer parser).

**Acceptance**

* Build succeeds
* `TravelBridge.Tests` passes

---

## Step 1 — Add a minimal “abstractions” project (no coupling, no heavy deps)

Create a new project:

### `TravelBridge.Providers.Abstractions`

**Why this project exists**

* Providers should not reference `TravelBridge.API`
* Providers should not be forced to reference `TravelBridge.Contracts`
* This project must stay *tiny* so future providers don’t inherit random dependencies

**Project rules**

* Target `net9.0`
* **No** `FrameworkReference Microsoft.AspNetCore.App`
* **No** JSON attributes, no HTTP, no EF, no DI extensions (keep it pure)
* Only BCL types (`System.*`) and maybe `System.Collections.*`

**Acceptance**

* Solution compiles with the new project (even if unused)

---

## Step 2 — Introduce ONE canonical composite-id type (fast + safe)

In `TravelBridge.Providers.Abstractions`, create:

### `CompositeId` (or `ProviderPrefixedId`)

A tiny `readonly record struct` with:

* `int ProviderId`
* `string Value` (the part after the dash, unmodified)

**Parsing requirements**

* Format: `"{providerId}-{value}"`
* Split on the **first** `-` only
  (IDs may contain additional dashes now or in the future)
* `providerId` must be an integer (`int.TryParse`)
* Must have non-empty `value`
* Expose:

  * `static bool TryParse(string input, out CompositeId id)`
  * `static CompositeId Parse(string input)` (throws a clear `ArgumentException`)
  * `override ToString()` returns the original format `"{ProviderId}-{Value}"`

**Performance rules**

* Do NOT use `string.Split`
* Use `IndexOf('-')` and slicing
* Avoid allocations except for `Value` substring (unavoidable)

**Add constants**

* `static class ProviderIds { const int Owned = 0; const int WebHotelier = 1; }`
* (Optional) keep a `HashSet<int>` of known provider IDs in API later, but not required now.

**Acceptance**

* Unit tests pass for parsing edge cases:

  * `"1-VAROSRESID"` ok
  * `"0-123"` ok
  * `"1-A-B-C"` ok → ProviderId=1, Value=`"A-B-C"`
  * `"-ABC"` fail
  * `"1-"` fail
  * `"ABC-123"` fail
  * `" 1-ABC "` should either trim or fail consistently (choose one and test it)

---

## Step 3 — Replace all `Split('-')` usage for hotel IDs in API with the new parser

This is the most important “prep” refactor because your code currently does:

```csharp
var parts = hotelId.Split('-'); // unsafe + alloc-heavy
var providerId = parts[0];
var hotelCode = parts[1];
```

### Where to refactor (based on your current code)

* `TravelBridge.API/Endpoints/HotelEndpoint.cs` (hotelId parsing)
* `TravelBridge.API/Endpoints/ReservationEndpoints.cs` (hotelId / hotelCode parsing)
* Any other place that expects `X-YYY` and uses `Split('-')`

**Rule**

* Refactor only parsing logic; don’t change business flow.

**Guidelines for implementation**

* Create a small API helper wrapper to reduce noise:

  * `TryParseHotelId(string input, out CompositeId id, out IResult errorResult)`
  * returns `Results.BadRequest(...)` with consistent message
* Or just call `CompositeId.Parse` and throw `ArgumentException` like current code does (but keep messages consistent).

**Important**

* Keep the scheme:

  * provider `0` and `1` remain as-is
* Do not add new formats (`wh:`, `owned:` etc.)

**Acceptance**

* All endpoint behavior remains the same for existing valid IDs
* Safer handling of IDs that contain dashes

---

## Step 4 — Define provider interfaces (but DO NOT implement them yet)

Now we lay the groundwork for multiple providers.

In `TravelBridge.Providers.Abstractions`, add **interfaces** and **provider-neutral request models**.

### 4.1 Provider interface shape

Start simple and aligned with what you already do today:

Create `IHotelProvider` with:

* `int ProviderId { get; }`
* Methods that correspond to “hotel info + availability”, using **provider-neutral** models and `CancellationToken`

Example method set (don’t over-design):

* `Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct);`
* `Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct);`
* `Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityQuery query, CancellationToken ct);`
* `Task<HotelFullInfoResult> GetHotelFullInfoAsync(HotelFullInfoQuery query, CancellationToken ct);` *(optional if you want parity with existing endpoint)*

**Rules**

* These must not reference `TravelBridge.Contracts`
* These must not reference WebHotelier types
* These must not do JSON formatting (party JSON belongs in provider implementations later)

### 4.2 Query models (provider-neutral)

Add models that represent intent, not HTTP shape:

* `HotelInfoQuery { string HotelId }` *(this is the provider-specific hotel id WITHOUT prefix — it’s `CompositeId.Value`)*
* `RoomInfoQuery { string HotelId, string RoomId }`
* `HotelAvailabilityQuery`

  * `string HotelId`
  * `DateOnly CheckIn`, `DateOnly CheckOut`
  * `PartyConfiguration Party` (typed)
  * `string? CouponCode` if needed
* `PartyConfiguration`

  * `int Rooms`
  * `List<PartyRoom>` or array
* `PartyRoom`

  * `int Adults`
  * `int[] ChildrenAges` (or `List<int>`)

**Rules**

* Prefer `DateOnly` internally (availability is date-based)
* Keep properties required and small; avoid nullable soup

### 4.3 Result models (provider-neutral)

Create minimal result models that match what the API needs *conceptually*, not necessarily 1:1 with JSON DTOs.

* `HotelInfoResult`
* `RoomInfoResult`
* `HotelAvailabilityResult`
* `HotelFullInfoResult`

**Rule**

* Keep them “data only”.

**Acceptance**

* Abstractions compiles
* No implementations added
* No references from providers to Contracts are required

---

## Step 5 — Add a resolver contract (optional now, useful later)

In Abstractions:

* `IHotelProviderResolver`

  * `IHotelProvider GetRequired(int providerId);`
  * `bool TryGet(int providerId, out IHotelProvider provider);`

**But**: do not implement it yet, OR implement it inside `TravelBridge.API` (not Abstractions) so Abstractions stays DI-agnostic.

**Acceptance**

* Interfaces exist; nothing wired yet.

---

## Step 6 — Add MSTest unit tests for `CompositeId`

Put tests in existing test project:

* `TravelBridge.Tests/Unit/CompositeIdTests.cs`

**Rules**

* Use MSTest (do not introduce xUnit/NUnit)
* Only test parsing/formatting behavior

**Acceptance**

* Tests pass
* Parsing behavior is locked in (prevents future accidental format changes like `wh:`)

---

## Step 7 — Add “Copilot implementation rules” document

Create a doc file (recommended location):

* `docs/PROVIDERS_PHASE1_RULES.md`

Include these rules so Copilot doesn’t drift:

### Required rules

1. **ID format is fixed:** `"{providerId}-{id}"`. No other formats.
2. **Provider projects must not reference `TravelBridge.API`.**
3. **Provider projects must not reference `TravelBridge.Contracts`** *(unless you explicitly decide otherwise later)*.
4. Abstractions is “data only”: no JSON, no HTTP, no DI, no EF.
5. No `string.Split` for composite IDs.
6. Provider selection must be via `providerId` integer (0 owned, 1 WH, others future).
7. All new provider-neutral models must be in Abstractions, not in API Contracts.

**Acceptance**

* Document exists and matches the plan

---

# Definition of Done (Phase 1)

✅ You have a new **Abstractions** project with:

* `CompositeId` parsing that preserves your `0-` / `1-` scheme
* provider interfaces + neutral query/result models (unused but ready)
* MSTest coverage for parsing
* API code refactored to stop using `Split('-')` for hotel IDs (safer + faster)

❗ No provider implementations added, no routing changes yet.

---

## Two quick assumptions I made (tell me if either is wrong)

1. It’s acceptable in Phase 1 to refactor **only parsing** (no endpoint behavior changes).
2. Providers will eventually be selected strictly by the `providerId` prefix, not by some DB lookup.

If those hold, this plan is the cleanest low-risk foundation.

If you want, paste the current list of endpoints you consider “core” for provider routing (HotelInfo / Availability / FullInfo / Booking / Plugin search) and I’ll tailor Step 4’s interface surface so it’s **exactly** the minimum you need—no extra methods.


I ASK: yew these sound good. only one question, you say no changes will happen to webhotelier in phase 1. how is this possible? web hotelier sould implement the interfaces and use the new query models. will this happen to pahse 2?

chatgpt says:
Exactly — **implementing the interfaces in WebHotelier is Phase 2**, not Phase 1.

Phase 1 is only about making the *core project* (API + shared abstractions) **ready** to support multiple providers, without touching existing behavior.

### Why Phase 1 can be “no WebHotelier changes”

Because in Phase 1 we’re only adding:

* the **Abstractions** project (interfaces + query/result models)
* a robust **`providerId-hotelId` parser**
* (optional) a resolver interface (but not used yet)

…and we **do not re-route any endpoints** through providers yet. Endpoints still call your existing `WebHotelierPropertiesService` directly, so nothing in WebHotelier has to change.

That gives you:

* clean foundation
* no risk of breaking production behavior
* a small PR that’s easy to review

### When does WebHotelier implement the interfaces?

**Phase 2**.

Phase 2 steps (just the idea, not detailed yet):

1. Add project ref: `TravelBridge.Providers.WebHotelier` → `TravelBridge.Providers.Abstractions`
2. Create `WebHotelierHotelProvider : IHotelProvider`
3. Create resolver implementation in API (or a small composition project)
4. Switch *one endpoint at a time* to:

   * parse composite id
   * resolve provider
   * call provider via Abstractions models
   * map to Contracts DTOs
5. Confirm output parity with current endpoints (tests / snapshot)

### Why this split is the safest approach

If you try to do “interfaces + WebHotelier implementation + endpoint rewiring” all in Phase 1, you’ll inevitably touch a lot of code and risk subtle behavior changes (especially around party parsing, date handling, errors).

By separating:

* Phase 1 = *foundation*
* Phase 2 = *WebHotelier adapter + switching endpoints*

…you keep each PR small and verifiable.

If you want, I can also add to the Phase 1 plan an explicit “guardrail” item: **compile-time unused** provider abstractions are allowed, but endpoints must not be changed to use them yet.
