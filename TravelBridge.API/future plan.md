# TravelBridge Phase 2 – Final Code Review (static)

This review is based on **static inspection** of the code inside `TravelBridge.zip`.

> I **cannot run** `dotnet test` / build here because this environment doesn’t include the .NET SDK. I validated the implementation by reading the code paths and comparing them with the `TravelBridge-PROD` reference folder included in the zip.

## ✅ What matches the Phase 2 goals

### 1) Availability endpoint now uses the provider layer
- `TravelBridge.API/Endpoints/HotelEndpoint.cs` calls `IAvailabilityService` for `/hotelRoomAvailability`.
- `TravelBridge.API/Services/AvailabilityService.cs` resolves the provider and calls:
  - `provider.GetHotelAvailabilityAsync(query)`
  - and, when no rates exist, `provider.GetAlternativesAsync(alternativesQuery)`

This achieves the intended flow:
`Endpoint → AvailabilityService → IHotelProvider → MapToResponse (pricing/coupons) → SingleAvailabilityResponse`

### 2) WebHotelier provider implements provider-neutral availability + alternatives
- `TravelBridge.Providers.WebHotelier/WebHotelierHotelProvider.cs`
  - `GetHotelAvailabilityAsync()` builds provider-neutral `HotelAvailabilityResult` with rooms/rates populated.
  - `GetAlternativesAsync()` calls flexible calendar, computes alternative windows, then intersects common dates across party configs.

### 3) Alternatives behavior parity is restored
- `AvailabilityService` triggers alternatives only when there are no rates (`Rooms.Any(r => r.Rates.Count > 0)` is false).
- Provider-side alternatives logic mirrors the old “keep only common date pairs across parties” approach.

### 4) RoomsCount weighting for alternatives is in place
In `WebHotelierHotelProvider.GetAlternativesAsync`, alternatives are multiplied by `RoomsCount` before intersection (so 2 identical rooms are priced as 2×).

### 5) Location.Name is mapped in the final response
In `TravelBridge.API/Helpers/Extensions/MappingExtensions.cs`, `MapToResponse(HotelAvailabilityResult ...)` includes:
`Location.Name = result.Data.Location.Name`.

## ⚠️ Things to double-check / minor risks

### A) RateId party suffix assumes single-digit adults (existing limitation)
Your existing `FillPartyFromId()` logic (in `TravelBridge.API/Helpers/General.cs`) reads **only the first digit** as adults.
So a suffix like `-10_5` would be interpreted incorrectly.
This isn’t introduced by Phase 2, but the current RateId format still depends on that assumption.

**If adults > 9 is even remotely possible**, update `FillPartyFromId()` to parse the full adults segment (up to the first `_`).

### B) `ProviderToContractsMapper.ToSingleHotelAvailabilityInfo()` still drops `Location.Name`
`MappingExtensions.MapToResponse()` is correct (it includes `Location.Name`), but `ProviderToContractsMapper.ToSingleHotelAvailabilityInfo()` maps only lat/long.
If someone reuses `ToSingleHotelAvailabilityInfo()` later, the name will be missing.

### C) Unit tests are mostly algorithmic (not “wiring” tests)
The added tests mostly validate formatting/intersection logic with in-memory data, but they don’t strongly verify:
- that `AvailabilityService` actually calls `GetAlternativesAsync()` on the provider (via a mock),
- or that `GetHotelAvailabilityAsync()` end-to-end produces the expected response shape.

If you want extra safety, add a small test using a mocking framework (e.g., Moq) to assert provider method calls.

## Quick checklist

- Provider-layer availability call: ✅
- Alternatives call on “no rates”: ✅
- Alternatives intersection logic: ✅
- RoomsCount weighting for alternatives: ✅
- Location.Name mapped in availability response: ✅
- Potential adult parsing edge-case: ⚠️ (pre-existing)
