# Gradual Decoupling Strategy

## Goal
Decouple WebHotelier provider from Contracts without changing API responses.

## Current State (Step 4)
- ✅ Created internal WH wire models (WHPartyItem, WHPayment, WHBaseResponse)
- ✅ Created Application domain models (PartyItem, Payment)
- ✅ Created mapping layer in WebHotelier provider
- ⏳ Contracts still contains models used by both Provider and API

## Migration Pattern (Per Endpoint)

### For Each Endpoint:
1. **Keep API response DTOs in Contracts** (for now)
2. **Provider returns Application models** (not Contracts)
3. **API maps Application → Contracts DTOs** (for backward compatibility)
4. **When all endpoints migrated**: move Contracts DTOs to API project

### Example Flow:
```
WebHotelier API → WHPartyItem (internal wire)
                ↓
WebHotelier Provider → PartyItem (Application domain)
                ↓
API Endpoint → PartyItem (Contracts DTO - temporary)
                ↓
WordPress Plugin (unchanged JSON)
```

## Current Dependencies

### TravelBridge.Contracts (will be removed)
- Used by: API, WebHotelier Provider
- Contains: API response DTOs, domain models (mixed)
- **Problem**: Provider shouldn't reference API contracts

### TravelBridge.Application (new)
- Used by: WebHotelier Provider, API
- Contains: Domain models, business logic
- **Goal**: Single source of truth for domain

### TravelBridge.Providers.WebHotelier
- References: Application, Contracts (temporary)
- Contains: Wire models (internal), Mappers, Provider implementation
- **Goal**: Only reference Application

## Next Steps

1. ✅ Create Application models (PartyItem, Payment)
2. ✅ Create mappers in WebHotelier
3. 🔄 Add compatibility layer documentation
4. ⏳ Pick ONE endpoint to migrate fully (HotelInfo)
5. ⏳ Verify build succeeds
6. ⏳ Test endpoint works identically
7. ⏳ Repeat pattern for other endpoints
8. ⏳ Remove Contracts reference from WebHotelier when done

## Models Status Tracker

| Model | Location | Status | Migration Plan |
|-------|----------|--------|----------------|
| `WHPartyItem` | WebHotelier (internal) | ✅ Done | Wire model |
| `WHPayment` | WebHotelier (internal) | ✅ Done | Wire model |
| `WHBaseResponse` | WebHotelier (internal) | ✅ Done | Wire model |
| `PartyItem` | Application | ✅ Done | Domain model |
| `Payment` | Application | ✅ Done | Domain model |
| `PartyItem` | Contracts | 🔄 Compatibility | Keep for API DTOs temporarily |
| `PaymentWH` | Contracts | 🔄 Compatibility | Keep for API DTOs temporarily |
| `BaseWebHotelierResponse` | Contracts | 🔄 Compatibility | Keep for API DTOs temporarily |
| `WebHotel` | Contracts | ⏳ Pending | To migrate |
| `HotelData` | Contracts | ⏳ Pending | To migrate |
| `Alternative` | Contracts | ⏳ Pending | To migrate |

## Notes
- **No API response changes** - JSON output must remain identical
- **Internal only** - WebHotelier models are not exposed
- **Gradual** - One endpoint at a time
- **Safe** - Build and test after each migration
