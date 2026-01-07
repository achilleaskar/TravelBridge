# Provider Abstraction Roadmap

This document outlines the multi-phase plan for implementing a provider abstraction layer in TravelBridge, enabling support for multiple hotel inventory sources.

## Goals

1. **Extensibility**: Add new hotel providers without modifying core API logic
2. **Clean Separation**: Providers handle fetch+map; API handles business logic
3. **Backward Compatibility**: No breaking changes to existing API contracts
4. **Testability**: Provider implementations are independently testable

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         TravelBridge.API                             │
│                                                                      │
│  Endpoints → Services → IHotelProviderResolver → IHotelProvider      │
│                                                                      │
│  Business Logic:                                                     │
│  - Pricing calculations                                              │
│  - Coupon application                                                │
│  - Availability filtering                                            │
│  - Response mapping                                                  │
└────────────────────────────────────┬────────────────────────────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    ▼                ▼                ▼
┌─────────────────────┐ ┌─────────────────────┐ ┌─────────────────────┐
│   WebHotelier       │ │   Owned (Future)    │ │   Provider N        │
│   Provider          │ │   Provider          │ │   (Future)          │
│                     │ │                     │ │                     │
│ - Fetch WH API      │ │ - Query local DB    │ │ - External API      │
│ - Map to neutral    │ │ - Map to neutral    │ │ - Map to neutral    │
└─────────────────────┘ └─────────────────────┘ └─────────────────────┘
```

## Provider IDs

| ID | Provider | Description | Status |
|----|----------|-------------|--------|
| 0 | Owned | Self-managed hotel inventory | Phase 3 |
| 1 | WebHotelier | External distribution platform | ✅ Complete |
| 2+ | Reserved | Future providers | Planned |

## Composite ID Format

All hotel IDs use a composite format:

```
{providerId}-{providerSpecificId}

Examples:
1-VAROSRESID     → WebHotelier hotel "VAROSRESID"
0-123            → Owned hotel with DB ID "123"
1-A-B-C          → WebHotelier hotel "A-B-C" (values can contain dashes)
```

## Phase Summary

| Phase | Focus | Status | Tests |
|-------|-------|--------|-------|
| Phase 1 | Infrastructure + Hotel Info | ✅ Complete | 116 |
| Phase 2 | Single-Hotel Availability | ✅ Complete | 137 |
| Phase 3 | Owned Provider + Search | 🔲 Planned | - |
| Phase 4 | Booking Abstraction | 🔲 Planned | - |

---

## Phase 1: Infrastructure (Complete)

**Objective**: Establish core abstractions and routing infrastructure.

### Deliverables
- [x] `TravelBridge.Providers.Abstractions` project
- [x] `CompositeId` for parsing/creating composite IDs
- [x] `IHotelProvider` interface (info methods only)
- [x] `IHotelProviderResolver` interface
- [x] `WebHotelierHotelProvider` (info methods)
- [x] Hotel info routing through provider layer

### Key Files
- `TravelBridge.Providers.Abstractions/CompositeId.cs`
- `TravelBridge.Providers.Abstractions/IHotelProvider.cs`
- `TravelBridge.Providers.WebHotelier/WebHotelierHotelProvider.cs`

### Documentation
- [Phase 1 Rules](./PROVIDERS_PHASE1_RULES.md)

---

## Phase 2: Availability (Complete)

**Objective**: Route single-hotel availability through provider abstraction with alternatives support.

### Deliverables
- [x] `IAvailabilityService` and implementation
- [x] Provider availability methods
- [x] Alternatives fetching when no rates
- [x] RoomsCount weighting for multi-room pricing
- [x] Provider-neutral result models
- [x] Comprehensive test coverage

### Key Files
- `TravelBridge.API/Services/AvailabilityService.cs`
- `TravelBridge.Providers.Abstractions/Models/HotelAvailabilityResult.cs`
- `TravelBridge.Providers.WebHotelier/WebHotelierHotelProvider.cs`

### Documentation
- [Phase 2 Availability](./PROVIDERS_PHASE2_AVAILABILITY.md)

---

## Phase 3: Owned Provider + Search (Planned)

**Objective**: Implement owned hotel provider and abstract multi-hotel search.

### Planned Deliverables
- [ ] `OwnedHotelProvider : IHotelProvider`
- [ ] Owned hotel database schema
- [ ] Owned hotel admin endpoints
- [ ] Multi-hotel search through provider layer
- [ ] Merged search results from multiple providers

### Architecture Considerations

```csharp
// Multi-provider search
public async Task<PluginSearchResponse> SearchAsync(SearchRequest request)
{
    var providers = _resolver.GetAll();
    var tasks = providers.Select(p => p.SearchAvailabilityAsync(query));
    var results = await Task.WhenAll(tasks);
    return MergeResults(results);
}
```

### Database Schema (Draft)

```sql
-- Owned Hotels
CREATE TABLE OwnedHotel (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Code VARCHAR(50) UNIQUE,
    Name VARCHAR(255),
    Description TEXT,
    Type VARCHAR(100),
    Rating INT,
    Latitude DECIMAL(10,7),
    Longitude DECIMAL(10,7),
    -- ... additional fields
);

-- Owned Rooms
CREATE TABLE OwnedRoom (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    HotelId INT FOREIGN KEY,
    Code VARCHAR(50),
    Name VARCHAR(255),
    MaxAdults INT,
    MaxChildren INT,
    -- ... additional fields
);

-- Owned Rates (simplified)
CREATE TABLE OwnedRate (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    RoomId INT FOREIGN KEY,
    Name VARCHAR(255),
    BasePrice DECIMAL(10,2),
    BoardType INT,
    -- ... pricing rules
);
```

---

## Phase 4: Booking Abstraction (Planned)

**Objective**: Abstract booking creation per provider.

### Planned Deliverables
- [ ] `IBookingProvider` interface
- [ ] Provider-specific booking implementations
- [ ] Unified booking flow in API
- [ ] Provider-specific cancellation handling

### Interface Design (Draft)

```csharp
public interface IBookingProvider
{
    Task<BookingResult> CreateBookingAsync(BookingRequest request, CancellationToken ct);
    Task<CancellationResult> CancelBookingAsync(string bookingId, CancellationToken ct);
    Task<BookingDetailsResult> GetBookingAsync(string bookingId, CancellationToken ct);
}
```

---

## Design Principles

### 1. Provider Responsibility
Providers should **fetch and map**, not apply business logic:
- ✅ Call external API
- ✅ Map to provider-neutral models
- ✅ Handle provider-specific error formats
- ❌ Apply pricing rules (that's API layer)
- ❌ Apply coupons (that's API layer)
- ❌ Filter by availability (that's API layer)

### 2. Model Ownership
- **Provider-neutral models** → `TravelBridge.Providers.Abstractions/Models/`
- **API contracts** → `TravelBridge.API/Contracts/` and `TravelBridge.Contracts/`
- **Provider DTOs** → Individual provider projects

### 3. No Cross-Provider References
```
TravelBridge.Providers.Abstractions ← (no dependencies)
TravelBridge.Providers.WebHotelier  → Abstractions (only)
TravelBridge.Providers.Owned        → Abstractions (only)
TravelBridge.API                    → All providers
```

### 4. Graceful Degradation
If one provider fails, others should continue:
```csharp
foreach (var provider in providers)
{
    try
    {
        results.Add(await provider.SearchAsync(query));
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Provider {Id} failed", provider.ProviderId);
        // Continue with other providers
    }
}
```

---

## Testing Strategy

### Unit Tests
- Provider method logic (mapping, grouping)
- CompositeId parsing
- Service orchestration (with mocked providers)

### Integration Tests
- End-to-end endpoint tests
- Real provider calls (in staging)

### Test Naming Convention
```
{MethodUnderTest}_{Scenario}_{ExpectedBehavior}
```

---

## Migration Notes

### For Future Providers
1. Create `TravelBridge.Providers.{Name}` project
2. Reference only `TravelBridge.Providers.Abstractions`
3. Implement `IHotelProvider`
4. Create service collection extension
5. Register in `Program.cs`
6. Add to `IHotelProviderResolver`

### Backward Compatibility
All phases maintain API contract compatibility. Existing WordPress plugin requires no changes.

---

## References

- [Phase 1 Rules](./PROVIDERS_PHASE1_RULES.md)
- [Phase 2 Availability](./PROVIDERS_PHASE2_AVAILABILITY.md)
- [Architecture Overview](./architecture-overview.md)
- [Hotel Provider Integration](./hotel-provider-integration.md)
