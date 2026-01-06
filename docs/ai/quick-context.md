# TravelBridge AI Quick Context

> **Purpose:** Quick-load reference for AI assistants to understand the codebase instantly without re-analysis.

---

## 🎯 One-Liner

**TravelBridge** = .NET 9 Minimal API that searches hotels via WebHotelier, processes payments via Viva Wallet, stores bookings in MariaDB, serves a WordPress plugin.

---

## 🗺️ File Map

```
📁 TravelBridge.API/
   📁 Endpoints/                    → API routes (3 files, minimal API style)
      SearchPluginEndpoints.cs      → /api/plugin/* (search, autocomplete)
      HotelEndpoint.cs              → /api/hotel/* (hotel info, availability)
      ReservationEndpoints.cs       → /api/reservation/* (checkout, payment, booking)

   📁 Models/WebHotelier/
      WebHotelierPropertiesService.cs → Main service (search, avail, book, cancel, email)

   📁 Services/
      AvailabilityProcessor.cs      → Filters hotels by party availability
      CheckoutProcessor.cs          → Payment calculations
      ConsoleEmailSender.cs         → SMTP email (class: SmtpEmailSender)

   📁 Repositories/
      ReservationsRepository.cs     → DB CRUD (reservations, payments, coupons)

   📁 Models/DB/                    → EF Core entities
      Reservation, Customer, Payment, ReservationRate, Coupon, PartyItemDB, PartialPaymentDB

   📁 Contracts/                    → API DTOs
      CheckoutResponse, SingleAvailabilityResponse, PluginSearchResponse, etc.

   📁 Helpers/
      General.cs                    → Party JSON builders, date utils, price calculations
      Extensions/MappingExtensions.cs → Price/board/type mapping logic

   📁 DataBase/
      AppDbContext.cs               → EF Core context

📁 TravelBridge.Providers.WebHotelier/
   WebHotelierClient.cs             → HTTP client for WebHotelier API

📁 TravelBridge.Payments.Viva/
   Services/Viva/
      VivaService.cs                → Payment orders, validation
      VivaAuthService.cs            → OAuth2 token caching

📁 TravelBridge.Geo.Mapbox/
   MapBoxService.cs                 → Location autocomplete

📁 TravelBridge.Contracts/
   Common/, Models/, Plugin/        → Shared DTOs across projects
```

---

## 🔑 Key Conventions

| Concept | Format | Example |
|---------|--------|---------|
| Hotel ID | `{provider}-{code}` | `1-VAROSVILL` |
| Rate ID | `{rateId}-{adults}_{child1}_{child2}` | `328000-226-2_5_10` |
| Party JSON | `[{"adults":N,"children":[ages]}]` | `[{"adults":2,"children":[5,10]}]` |
| Date Input | `dd/MM/yyyy` | `15/06/2025` |
| Date WebHotelier | `yyyy-MM-dd` | `2025-06-15` |
| Provider enum | WebHotelier = 1 | - |

---

## 🔄 Core Flows

### Search Flow
```
autocomplete → MapBox + WebHotelier.SearchPropertyAsync()
submitSearch → WebHotelier.GetAvailabilityAsync() 
             → Merge multi-room 
             → AvailabilityProcessor.FilterHotelsByAvailability()
             → Apply filters
```

### Booking Flow
```
preparePayment → Validate → Re-check avail → Viva.GetPaymentCode() 
              → ReservationsRepository.CreateTemporaryExternalReservation() (Pending)

paymentSucceed → Viva.ValidatePayment() 
              → WebHotelierPropertiesService.CreateBooking() 
              → SendConfirmationEmail()
```

### Multi-Party Search
```
Party: [{"adults":2},{"adults":2},{"adults":3}]
Groups to: [{adults:2, count:2}, {adults:3, count:1}]
→ 2 parallel API calls
→ Merge results (only hotels available for ALL parties)
→ Sum prices across parties
```

---

## 📝 Adding Features Checklist

### New Endpoint
1. Create class in `TravelBridge.API/Endpoints/`
2. Add `MapEndpoints(IEndpointRouteBuilder app)` method
3. Register: `builder.Services.AddScoped<MyEndpoint>()`
4. Map: `serviceProvider.GetRequiredService<MyEndpoint>().MapEndpoints(app)`

### New DB Entity
1. Create in `TravelBridge.API/Models/DB/` extending `BaseModel`
2. Add `DbSet<T>` to `AppDbContext`
3. Configure in `OnModelCreating()` if needed
4. `dotnet ef migrations add MigrationName -p TravelBridge.API`

### New External Service
1. Options class in project's Models or root
2. Service class with HttpClient
3. Create `ServiceCollectionExtensions.cs` with `AddMyService()` method
4. Register in `Program.cs`: `builder.Services.AddMyService(builder.Configuration)`

---

## 🧪 How to Test Locally

```bash
# Run API
cd TravelBridge.API
dotnet run

# Swagger
http://localhost:5000/swagger

# Health check
GET /health

# Test search
GET /api/plugin/autocomplete?searchQuery=Trikala

GET /api/plugin/submitSearch?checkin=15/06/2025&checkOut=20/06/2025&bbox=[23.377,34.730,26.447,35.773]-35.340-25.134&adults=2&rooms=1&searchTerm=Crete
```

---

## 📚 Full Documentation

- [README](./README.md) - Overview and index
- [Architecture](./architecture-overview.md) - System design
- [API Endpoints](./api-endpoints.md) - Full API reference
- [Database Schema](./database-schema.md) - Entity details
- [Payment Flow](./payment-flow.md) - Viva integration
- [Hotel Provider](./hotel-provider-integration.md) - WebHotelier
- [Geo Services](./geo-services.md) - MapBox/HereMaps
- [Business Rules](./business-rules.md) - Pricing, filters, coupons
- [Deployment](./deployment-configuration.md) - Config and deploy
