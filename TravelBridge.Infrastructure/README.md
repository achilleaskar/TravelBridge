# TravelBridge.Infrastructure

## Purpose
This is the **infrastructure layer** of TravelBridge. It contains:
- **Data access** (EF Core DbContext, Repositories)
- **External API integrations** (WebHotelier, Viva, MapBox, HereMaps)
- **Email services** (SMTP)
- **Caching implementations**

## Architecture Rules
1. ✅ **Depends only on Core** (implements Core interfaces)
2. ✅ **No business logic** - only data access and external calls
3. ✅ **All implementations** of Core interfaces live here
4. ❌ **Never referenced by Core** (dependency flows inward)

## Folder Structure
- `Data/` - EF Core DbContext and configurations
- `Data/Repositories/` - Repository implementations
- `Integrations/` - External API clients
  - `WebHotelier/` - Hotel provider integration
  - `Viva/` - Payment provider integration
  - `MapBox/` - Geocoding service
  - `HereMaps/` - Geocoding service
  - `Email/` - SMTP email sender
- `Caching/` - Cache implementations

## Dependencies
- ✅ TravelBridge.Core (interfaces to implement)
- ✅ Microsoft.EntityFrameworkCore
- ✅ Pomelo.EntityFrameworkCore.MySql
- ✅ Microsoft.Extensions.Http.Polly
- ✅ Microsoft.Extensions.Caching.Memory
