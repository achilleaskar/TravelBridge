# Provider Abstraction - Phase 2: Availability

This document describes the Phase 2 implementation of the multi-provider architecture, focusing on hotel availability and alternatives functionality.

## Overview

**Phase 2 Goal**: Route single-hotel availability requests through the provider abstraction layer while maintaining full backward compatibility with the existing API contract.

**Status**: ✅ Complete (137 tests passing)

## What Changed in Phase 2

### New Flow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         TravelBridge.API                             │
│  ┌──────────────┐                                                    │
│  │ HotelEndpoint│ → /hotelRoomAvailability                          │
│  └──────┬───────┘                                                    │
│         │                                                            │
│         ▼                                                            │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │               AvailabilityService (NEW)                      │    │
│  │  - Resolves provider via IHotelProviderResolver              │    │
│  │  - Calls provider.GetHotelAvailabilityAsync()                │    │
│  │  - Fetches alternatives when no rates (via provider)         │    │
│  │  - Applies coupon logic                                      │    │
│  │  - Maps to SingleAvailabilityResponse                        │    │
│  └──────────────────────────┬──────────────────────────────────┘    │
│                             │                                        │
│  ┌──────────────────────────┼──────────────────────────────────┐    │
│  │          IHotelProviderResolver                              │    │
│  │          (resolves providerId → IHotelProvider)              │    │
│  └──────────────────────────┼──────────────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│           TravelBridge.Providers.WebHotelier                         │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │            WebHotelierHotelProvider : IHotelProvider         │    │
│  │  - GetHotelAvailabilityAsync() → HotelAvailabilityResult     │    │
│  │  - GetAlternativesAsync() → AlternativesResult               │    │
│  │  - Maps WH DTOs to provider-neutral models                   │    │
│  └──────────────────────────┬──────────────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        WebHotelier API                               │
└─────────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Location | Responsibility |
|-----------|----------|----------------|
| `IAvailabilityService` | API/Services | Service contract for availability |
| `AvailabilityService` | API/Services | Orchestrates provider calls + mapping |
| `IHotelProviderResolver` | Providers.Abstractions | Resolves provider by ID |
| `HotelProviderResolver` | API/Providers | DI-based provider resolution |
| `WebHotelierHotelProvider` | Providers.WebHotelier | WH-specific implementation |

## Provider Interface Extensions

### IHotelProvider (Extended in Phase 2)

```csharp
public interface IHotelProvider
{
    int ProviderId { get; }
    
    // Phase 1
    Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct);
    Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct);
    
    // Phase 2 - NEW
    Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(
        HotelAvailabilityQuery query, CancellationToken ct);
    Task<SearchAvailabilityResult> SearchAvailabilityAsync(
        SearchAvailabilityQuery query, CancellationToken ct);
    Task<AlternativesResult> GetAlternativesAsync(
        AlternativesQuery query, CancellationToken ct);
}
```

### Provider-Neutral Models (New)

```
TravelBridge.Providers.Abstractions/Models/
├── HotelAvailabilityQuery.cs      # Input: hotel, dates, party
├── HotelAvailabilityResult.cs     # Output: rooms, rates, alternatives
├── SearchAvailabilityQuery.cs     # Input: bbox, dates, party
├── SearchAvailabilityResult.cs    # Output: list of hotels with pricing
├── AlternativesQuery.cs           # Input: hotel, dates, party, range
└── AlternativesResult.cs          # Output: list of alternative date windows
```

## Rate ID Format

The rate ID format is designed to be compatible with existing `FillPartyFromId()` logic:

```
{baseRateId}-{adults}[_{childAge1}_{childAge2}...]

Examples:
328000-2        → 2 adults, no children
328000-2_5_10   → 2 adults, children ages 5 and 10
328000-10_3_7   → 10 adults, children ages 3 and 7
```

**Important**: `RoomsCount` is NOT included in the rate ID. It's stored in `SearchParty.RoomsCount` for internal accumulation logic.

## Multi-Room Party Handling

### Grouping Logic

When multiple rooms have identical configurations, they're grouped:

```csharp
// Input party: [{"adults":2},{"adults":2},{"adults":3}]
// After grouping:
[
  { adults: 2, children: [], RoomsCount: 2, party: "[{\"adults\":2}]" },
  { adults: 3, children: [], RoomsCount: 1, party: "[{\"adults\":3}]" }
]
```

### Price Accumulation

Prices are accumulated across party configurations:

```csharp
// For availability:
totalMinPrice = sum(partyMinPrice * partyRoomsCount)

// For alternatives:
alternatives = alternatives.Select(a => a with {
    MinPrice = a.MinPrice * partyItem.RoomsCount,
    NetPrice = a.NetPrice * partyItem.RoomsCount
});
```

## Alternatives Logic

Alternatives are fetched only when **no rates are available**:

```csharp
var hasRates = providerResult.Data?.Rooms?.Any(r => r.Rates.Count > 0) == true;
if (!hasRates)
{
    var alternativesResult = await provider.GetAlternativesAsync(alternativesQuery);
    // Attach to response
}
```

### KeepCommon Algorithm

When multiple party configurations exist, only dates available for ALL configurations are returned:

1. Fetch flexible calendar per grouped party
2. Apply RoomsCount weighting to prices
3. Intersect date pairs across all parties
4. Sum prices for common dates

## Mapping Flow

```
WebHotelier Response (WH DTOs)
        │
        ▼
WebHotelierHotelProvider.GetHotelAvailabilityAsync()
        │
        ▼
HotelAvailabilityResult (Provider-Neutral)
        │
        ▼
AvailabilityService.GetHotelAvailabilityAsync()
        │
        ▼
MappingExtensions.MapToResponse()
        │
        ▼
SingleAvailabilityResponse (API Contract)
```

### Key Mapping Files

| File | Purpose |
|------|---------|
| `WHMappingHelpers.cs` | WH DTOs → Provider-neutral models |
| `MappingExtensions.cs` | Provider-neutral → API contracts |
| `ProviderToContractsMapper.cs` | Alternative mapping utilities |

## Configuration

No new configuration required. Uses existing `WebHotelierApi` settings.

## Testing

### Test Coverage (137 tests)

| Test Class | Tests | Coverage |
|------------|-------|----------|
| `AvailabilityServiceTests` | 11 | Alternatives flow, query/result models |
| `WebHotelierHotelProviderTests` | 19 | Rate grouping, RoomsCount weighting |
| `HotelProviderResolverTests` | 5 | Provider resolution |
| `ProviderRoutingTests` | 9 | End-to-end routing |
| `CompositeIdTests` | 10+ | ID parsing |

### Running Tests

```bash
# All unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Specific test class
dotnet test --filter "FullyQualifiedName~AvailabilityServiceTests"
```

## Breaking Changes

**None** - Phase 2 maintains full backward compatibility:

- API contract unchanged (`SingleAvailabilityResponse`)
- Endpoint paths unchanged
- Request parameters unchanged
- Response structure unchanged

## Known Limitations

1. **Single-digit adults assumption (FIXED)**: `FillPartyFromId()` now properly parses multi-digit adults
2. **WebHotelier-only**: Only WebHotelier provider implemented; Owned provider is Phase 3

## Files Changed

### New Files
- `TravelBridge.API/Services/IAvailabilityService.cs`
- `TravelBridge.API/Services/AvailabilityService.cs`
- `TravelBridge.Providers.Abstractions/Models/HotelAvailabilityResult.cs`
- `TravelBridge.Providers.Abstractions/Models/SearchAvailabilityResult.cs`
- `TravelBridge.Providers.Abstractions/Models/AlternativesResult.cs`
- `TravelBridge.Tests/Unit/AvailabilityServiceTests.cs` (expanded)
- `TravelBridge.Tests/Unit/WebHotelierHotelProviderTests.cs` (expanded)

### Modified Files
- `TravelBridge.Providers.Abstractions/IHotelProvider.cs` - Added availability methods
- `TravelBridge.Providers.WebHotelier/WebHotelierHotelProvider.cs` - Full implementation
- `TravelBridge.API/Endpoints/HotelEndpoint.cs` - Now uses `IAvailabilityService`
- `TravelBridge.API/Helpers/Extensions/MappingExtensions.cs` - New MapToResponse overload
- `TravelBridge.API/Helpers/General.cs` - Fixed multi-digit adults parsing
- `TravelBridge.API/Providers/ProviderToContractsMapper.cs` - Added Location.Name

## Verification Checklist

- [x] Provider-layer availability call
- [x] Alternatives call only when no rates
- [x] Alternatives intersection logic (KeepCommon)
- [x] RoomsCount weighting for alternatives pricing
- [x] Location.Name preserved in all responses
- [x] Multi-digit adults parsing fixed
- [x] All 137 tests pass
- [x] No breaking changes to API contract

## Next Steps (Phase 3)

1. **Owned Provider**: Implement `OwnedHotelProvider : IHotelProvider`
2. **Search Endpoint**: Route multi-hotel search through provider layer
3. **Booking Endpoint**: Abstract booking creation per provider
4. **Provider Selection UI**: Allow frontend to filter by provider
