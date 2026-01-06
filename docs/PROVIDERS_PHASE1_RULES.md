# Provider Abstraction Rules (Phase 1)

This document defines the rules and constraints for the multi-provider abstraction layer implemented in Phase 1.

## ID Format (FIXED - DO NOT CHANGE)

The composite ID format is:

```
{providerId}-{value}
```

Where:
- `providerId` is an integer (0 = Owned, 1 = WebHotelier, 2+ = future providers)
- `value` is the provider-specific hotel/resource ID (may contain additional dashes)

### Examples
- `0-123` - Owned hotel with ID "123"
- `1-VAROSRESID` - WebHotelier hotel "VAROSRESID"
- `1-A-B-C` - WebHotelier hotel "A-B-C" (value can contain dashes)

### Parsing Rules
- Split only on the **first** dash
- `providerId` must parse as `int`
- `value` must be non-empty

> ⚠️ **Never introduce alternative formats** (no `wh:`, `owned:`, etc.)

## Project Dependencies

### TravelBridge.Providers.Abstractions
This project must remain minimal and dependency-free:

- ✅ Target `net9.0`
- ❌ No `FrameworkReference Microsoft.AspNetCore.App`
- ❌ No JSON attributes or System.Text.Json
- ❌ No HTTP clients or HttpClientFactory
- ❌ No Entity Framework
- ❌ No DI extensions (Microsoft.Extensions.DependencyInjection)
- ✅ Only BCL types (`System.*`)

### Provider Projects (e.g., TravelBridge.Providers.WebHotelier)
- ❌ Must NOT reference `TravelBridge.API`
- ❌ Must NOT reference `TravelBridge.Contracts` (unless explicitly decided otherwise)
- ✅ May reference `TravelBridge.Providers.Abstractions`

## Provider IDs

| Provider | ID | Notes |
|----------|-----|-------|
| Owned | 0 | Future: owned inventory |
| WebHotelier | 1 | Current external provider |
| Reserved | 2+ | Future providers |

Use constants from `TravelBridge.Providers.Abstractions.ProviderIds`.

## Code Rules

### DO NOT use `string.Split('-')` for composite IDs
Use `CompositeId.TryParse()` or `CompositeId.Parse()` instead.

```csharp
// ❌ BAD - unsafe, allocates array, doesn't handle edge cases
var parts = hotelId.Split('-');
var providerId = parts[0];
var hotelCode = parts[1];

// ✅ GOOD - safe, fast, handles all edge cases
if (!CompositeId.TryParse(hotelId, out var id))
{
    throw new ArgumentException("Invalid hotelId format");
}
var providerId = id.ProviderId;
var hotelCode = id.Value;
```

### Provider selection
Provider selection must be based on the integer `providerId` prefix, not string matching or DB lookups.

```csharp
// ✅ GOOD
if (compositeId.ProviderId == ProviderIds.WebHotelier)
{
    // Use WebHotelier provider
}

// ❌ BAD
if (hotelId.StartsWith("wh:"))
{
    // Don't use string prefixes
}
```

### Provider-neutral models
All new provider-neutral models must be in `TravelBridge.Providers.Abstractions`, not in API Contracts.

## Files Changed in Phase 1

### New Files
- `TravelBridge.Providers.Abstractions/` - New project
  - `CompositeId.cs` - Composite ID parsing
  - `ProviderIds.cs` - Provider ID constants
  - `IHotelProvider.cs` - Provider interface
  - `IHotelProviderResolver.cs` - Resolver interface
  - `Models/` - Provider-neutral query/result models

### Modified Files
- `TravelBridge.API/Endpoints/HotelEndpoint.cs` - Uses `CompositeId`
- `TravelBridge.API/Endpoints/ReservationEndpoints.cs` - Uses `CompositeId`

### Test Files
- `TravelBridge.Tests/Unit/CompositeIdTests.cs` - Unit tests for parsing

## Phase 2 Preview

In Phase 2, we will:
1. Add reference: `TravelBridge.Providers.WebHotelier` → `TravelBridge.Providers.Abstractions`
2. Implement `WebHotelierHotelProvider : IHotelProvider`
3. Implement `IHotelProviderResolver` in API
4. Gradually switch endpoints to use the provider abstraction
5. Add an Owned provider implementation

## Verification

To verify the Phase 1 implementation:

```bash
# Build the solution
dotnet build

# Run CompositeId tests
dotnet test --filter "FullyQualifiedName~CompositeIdTests"

# Run all tests
dotnet test
```
