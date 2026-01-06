# TravelBridge Architecture

## Overview
TravelBridge is a hotel aggregation API that integrates with multiple hotel providers. This document outlines the architectural principles and patterns used in the system.

## Project Structure

```
TravelBridge/
├── TravelBridge.API/        # Main API project
│   ├── Contracts/           # API response/request contracts (public API)
│   ├── Endpoints/           # API endpoint definitions
│   ├── Services/            # Service implementations
│   │   └── WebHotelier/     # WebHotelier provider implementation
│   ├── Models/              # Internal models and enums
│   └── Helpers/             # Utility classes
├── TravelBridge.Common/     # Shared common utilities
└── TravelBridge.Models/     # Shared domain models
```

## Provider Architecture

### Design Principles

1. **Provider Independence**: Provider implementations (e.g., WebHotelier) should NOT depend on API contracts
2. **Separation of Concerns**: Keep provider-specific logic separate from API contracts
3. **Extensibility**: The architecture should allow adding new providers without modifying existing code

### Correct Provider Integration Pattern

When adding a new hotel provider:

1. **Create Provider-Specific Models** in `TravelBridge.API/Models/{ProviderName}/`
   - Provider request/response models
   - Provider-specific data structures

2. **Create Provider Service** in `TravelBridge.API/Services/{ProviderName}/`
   - Service class that communicates with the provider's API
   - Mapping logic from provider models to API contracts

3. **Reuse API Contracts** in `TravelBridge.API/Contracts/`
   - These are the public API contracts returned to clients
   - Provider services should map their internal models to these contracts
   - DO NOT create duplicate request/response classes for each provider

4. **Provider Abstraction Location**
   - If creating a provider abstraction interface (e.g., `IHotelProvider`), place it in `TravelBridge.API/Services/` or a dedicated `Providers/` folder under Services
   - **NEVER** place provider abstractions in `TravelBridge.API/Contracts/` as this would force providers to depend on contracts

### Anti-Patterns to Avoid

❌ **DO NOT**:
- Place provider interfaces in `TravelBridge.API/Contracts/`
- Create duplicate request/response classes for each provider
- Use fragile string parsing (e.g., `Split('-')`) without validation
- Throw `NotSupportedException` for unimplemented providers in endpoints

✅ **DO**:
- Reuse existing API contract classes across all providers
- Use helper classes for parsing composite IDs (see `CompositeIdHelper`)
- Implement complete provider support or use graceful degradation
- Map provider-specific models to shared API contracts

## Composite ID Format

The system uses composite IDs to encode multiple pieces of information:

### Hotel ID Format
```
{providerId}-{hotelId}
```
Example: `1-VAROSRESID`

**Parsing**: Use `CompositeIdHelper.ParseHotelId()` which correctly handles hotel IDs containing dashes.

### Location BBox ID Format
```
{bbox}-{latitude}-{longitude}
```
Example: `bbox1-37.9838-23.7275`

**Parsing**: Use `CompositeIdHelper.ParseBBoxId()` which correctly handles coordinates containing dashes or negative values.

### Why Not Simple Split?

Using `string.Split('-')` is fragile because:
- Hotel IDs may contain dashes (e.g., `HOTEL-ABC-123`)
- Coordinates may be negative (e.g., `-37.9838`)
- The parsing logic cannot distinguish between structural dashes and data dashes

The `CompositeIdHelper` class uses `IndexOf` to find only the structural separators, allowing the data portions to contain dashes.

## Request/Response Flow

```
Client Request
    ↓
API Endpoint (Contracts)
    ↓
Provider Service (e.g., WebHotelierPropertiesService)
    ↓
Provider Models (e.g., WebHotelier internal models)
    ↓
External Provider API
    ↓
Provider Models (response)
    ↓
Mapping to API Contracts
    ↓
API Endpoint (Contracts)
    ↓
Client Response
```

## Adding a New Provider

1. Create provider models in `Models/{ProviderName}/`
2. Create provider service in `Services/{ProviderName}/`
3. Map provider responses to existing API contracts (in `Contracts/`)
4. Update `Models/Enums.cs` to add the new provider enum value
5. Update endpoints to support the new provider ID
6. DO NOT modify or duplicate contract classes

## Key Classes

- **CompositeIdHelper**: Robust parsing for composite IDs
- **Provider Enum**: Defines available providers (currently WebHotelier)
- **API Contracts**: Public API request/response models (in `Contracts/`)
- **Provider Services**: Provider-specific implementations (in `Services/`)
- **Provider Models**: Provider-specific internal models (in `Models/`)
