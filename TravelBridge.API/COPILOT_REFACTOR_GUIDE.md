# COPILOT_REFACTOR_GUIDE.md
Goal: Refactor the current single project `TravelBridge.API` into a modular monolith with provider projects,
without changing behavior. Keep compilation green after each step. Do NOT do a big-bang rewrite.

## Target Solution Structure
- TravelBridge.API (Minimal API host / composition root)
- TravelBridge.Application (ports + resolvers + later use-cases)
- TravelBridge.Providers.WebHotelier (implements IHotelProvider)
- TravelBridge.Payments.Viva (implements IPaymentProvider)
- TravelBridge.Geo.Mapbox (implements IGeoProvider)
- TravelBridge.Geo.HereMaps (implements IGeoProvider)
Optional later:
- TravelBridge.Infrastructure.MariaDb (DbContext, migrations, repositories)
- TravelBridge.Domain (entities) / TravelBridge.Contracts (DTOs)

## Hard Rules (DO NOT VIOLATE)
1) Providers MUST NOT reference TravelBridge.API.
2) TravelBridge.Application contains provider interfaces ("ports") that providers implement.
3) TravelBridge.API is the only place where DI wiring happens (composition root).
4) Keep namespaces temporarily if that minimizes churn.
5) After each step, run `dotnet build` and fix immediately.

## Step-by-step Plan (must follow in order)

### Phase 1: Create projects & references (no file moves)
1. Create classlib projects:
   - TravelBridge.Application
   - TravelBridge.Providers.WebHotelier
   - TravelBridge.Payments.Viva
   - TravelBridge.Geo.Mapbox
   - TravelBridge.Geo.HereMaps
2. Add to TravelBridge.sln
3. Add references:
   - TravelBridge.API references TravelBridge.Application and all provider projects

### Phase 2: Add Ports & Resolvers
4. In TravelBridge.Application add ports:
   - IHotelProvider, IHotelProviderResolver
   - IPaymentProvider, IPaymentProviderResolver + payment records
   - IGeoProvider
   - ProviderHotelId helper
5. Add resolver implementations:
   - HotelProviderResolver, PaymentProviderResolver
6. Add AddTravelBridgeApplication() extension method registering resolvers.

### Phase 3: Move providers one-by-one (keep behavior)
7. Move WebHotelier code:
   - Move TravelBridge.API/Services/WebHotelier/* and Models/WebHotelier/* to TravelBridge.Providers.WebHotelier
   - Create WebHotelierHotelProvider : IHotelProvider with same logic as WebHotelierPropertiesService
   - Add AddWebHotelier(IServiceCollection, IConfiguration)
8. Move Viva code:
   - Move TravelBridge.API/Services/Viva/* to TravelBridge.Payments.Viva
   - Implement VivaPaymentProvider : IPaymentProvider
   - Remove IHttpContextAccessor usage from provider:
     - Accept PaymentSourceHint in CreatePaymentRequest and decide SourceCode there
   - Add AddVivaPayments(IServiceCollection, IConfiguration)
9. Move Geo:
   - Mapbox: implement IGeoProvider in TravelBridge.Geo.Mapbox
   - HereMaps: implement IGeoProvider in TravelBridge.Geo.HereMaps
   - Add corresponding AddMapboxGeo/AddHereMapsGeo

### Phase 4: Wire DI in TravelBridge.API Program.cs
10. Replace old registrations with:
    - services.AddTravelBridgeApplication()
    - services.AddWebHotelier(Configuration)
    - services.AddVivaPayments(Configuration)
    - services.AddMapboxGeo(Configuration)
    - services.AddHereMapsGeo(Configuration)

### Phase 5: Optional - Extract infrastructure
11. Create TravelBridge.Infrastructure.MariaDb and move:
    - DataBase/, Migrations/, Repositories/
12. Update EF Core config in API to use migrations assembly TravelBridge.Infrastructure.MariaDb

### Phase 6: Thin endpoints (later)
13. Create Application services (orchestrators) for Checkout/Reservation/Payment flows.
14. Endpoints call orchestrators instead of calling providers directly.

## Definition of Done (for this refactor)
- Build succeeds
- Existing endpoints return same responses
- Providers are isolated in their projects and implement ports
- No provider project references TravelBridge.API

## Common Pitfalls to Avoid
- Circular project references: never make Application reference providers.
- Moving too many files at once: move one provider at a time, build after each move.
- Injecting HttpContext into providers: pass only minimal hints/values from API/Application.
- Accidentally changing DTO shapes: keep Contracts stable until orchestration is moved.
