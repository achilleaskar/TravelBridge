Yes — and the clean way to do it (without drowning in duplicates) is to **stop treating provider DTOs as “your” API models** and introduce a *single seam*: an **Anti-Corruption Layer (ACL)** = mapping boundary.

Right now your pain is because models are doing **triple duty**:

1. **Provider wire DTOs** (WebHotelier JSON shapes)
2. **Your internal business data** (payments logic, coupon logic, etc.)
3. **Public API contracts** (what WordPress plugin consumes)

When one class plays all 3 roles, you can’t separate projects without everything pulling everything.

---

## The separation that actually works

### 1) Three model sets (with strict ownership)

**A. API Contracts (public)** – `TravelBridge.Contracts`

* Only request/response DTOs for your endpoints
* **No provider types, no DB types, no helper methods**
* Can have `[JsonPropertyName]` to preserve exact JSON shape for WP plugin

**B. Provider DTOs (wire)** – `TravelBridge.Providers.WebHotelier`

* DTOs that match WebHotelier request/response JSON
* Mark as `internal` whenever possible (prevents leaks)

**C. Application/Domain models (internal)** – `TravelBridge.Application` (or `Domain`)

* “Business meaning” objects: availability results, payment schedule, reservation draft, etc.

**Rule:** providers output **Application models**, not API DTOs.

---

## The trick to decouple without rewriting the plugin JSON

### Use “shadow DTOs” for the API

Create API DTOs that look **exactly** like what you return today, but *do not reference provider classes*.

Example: you currently return WebHotelier-ish stuff in `CheckoutResponse` (`PaymentWH`, `PartyItem`, `HotelOperation`, `BaseWebHotelierResponse`, etc.).

Do this instead:

* `Contracts/Checkout/CheckoutResponseDto.cs` (pure)
* `Application/Checkout/CheckoutResult.cs` (internal meaning)
* `Providers.WebHotelier/...` maps WH wire → `CheckoutResult`
* API maps `CheckoutResult` → `CheckoutResponseDto`

This gives you separation while keeping output identical.

---

## How to split “model used for provider mapping AND API response”

### Don’t duplicate “API vs WH” unless necessary

Instead:

* Keep **one API contract** (what client sees)
* Keep provider DTOs private to provider project
* Write mapping code

So for your examples:

### ✅ AutoCompleteHotel / AutoCompleteLocation

Keep **ONE** API DTO in `Contracts`:

* `AutoCompleteHotel`
* `AutoCompleteLocation`

Providers map into these (or into Application models first).

Only split if you truly need provider-only fields.

---

## How to split “models used for BOTH API requests AND provider service calls”

### Requests: keep API request, map to provider request

Keep:

* `MultiAvailabilityRequest` (API contract)
* `SingleAvailabilityRequest` (API contract)

Provider adapters should NOT accept these directly long-term.

Instead add Application commands:

* `SearchAvailabilityQuery`
* `GetHotelAvailabilityQuery`

API maps request DTO → query
Provider maps query → WH request DTO

This avoids having to create `WHMultiAvailabilityRequest` unless WebHotelier actually requires a very different shape.

---

## Practical steps (the “I can do this without madness” plan)

### Step 1 — Stop leaking provider types by making them `internal`

In provider projects:

* make all WH wire models `internal class ...`
* keep only the adapter class public (e.g., `WebHotelierHotelProvider`)

This forces you to map at the boundary (compiler helps).

### Step 2 — Pick ONE endpoint and create its API “shadow DTO”

Start with the worst offender (usually `CheckoutResponse`, `PluginSearchResponse`, `BookingResponse`).

For each, create a **pure** DTO in `TravelBridge.Contracts` that matches your current JSON.

Tip: **copy the class and replace provider types** gradually:

* `PaymentWH` → `PaymentDto`
* `PartyItem` → `PartyDto`
* `HotelOperation` → `HotelOperationDto`
* remove inheritance like `BaseWebHotelierResponse` → use composition (e.g. `Error`, `IsSuccess`)

### Step 3 — Mapping layer (two-stage)

* Provider returns `CheckoutResult` (Application model)
* API returns `CheckoutResponseDto` (Contracts)

So you don’t have to map provider → API directly everywhere.

### Step 4 — Move all business logic OUT of DTOs

Anything like `MergePayments()`, coupon checks, etc. must move to:

* Application service (e.g. `CheckoutComposer`)
* or extension methods in Application

DTOs should be “dumb bags of data”.

### Step 5 — Enforce “Contracts must not reference Providers”

In csproj terms:

* `TravelBridge.Contracts` references nothing
* `TravelBridge.Providers.*` may reference `TravelBridge.Application`
* `TravelBridge.API` references everything

This is the safety net.

---

## A super practical shortcut that reduces work a lot

When a provider model is currently embedded deep in responses (like WH payments/cancellation), use a **flattened API type** and keep extra provider fields in a bag temporarily:

```csharp
public sealed class PaymentDto
{
    public string DueDate { get; set; } = default!;
    public decimal Amount { get; set; }
    public Dictionary<string, object>? ProviderMeta { get; set; } // temporary escape hatch
}
```

This lets you separate now without losing data, then clean it later.

---

## If you want, I’ll make it concrete on *your* code

Send me one of the worst response models you currently return (paste the class, e.g. `CheckoutResponse` or `PluginSearchResponse`) and I’ll:

* design the **pure API DTO** version
* design the **Application model** version
* give you the **exact mapping methods** (minimal changes, same JSON)

That’s the fastest way to break the WebHotelier model dependency chain without duplicating everything blindly.
