# TravelBridge AI Context Guide

> **Purpose:** Quick-load reference for AI assistants to understand the codebase instantly without re-analysis.

---

## 🎯 One-Liner
**TravelBridge** = .NET 9 Minimal API that searches hotels via WebHotelier, processes payments via Viva Wallet, stores bookings in MariaDB.

---

## 🗺️ File Map

```
📁 Endpoints/           → API routes (3 files, minimal API style)
   SearchPluginEndpoints.cs  → /api/plugin/* (search, autocomplete)
   HotelEndpoint.cs          → /api/hotel/* (hotel info, availability)
   ReservationEndpoints.cs   → /api/reservation/* (checkout, payment, booking)

📁 Services/
   WebHotelier/
      WebHotelierPropertiesService.cs → Main service (search, avail, book, cancel, email)
   Viva/
      VivaService.cs         → Payment orders, validation
      VivaAuthService.cs     → OAuth2 token caching
   ExternalServices/
      MapBoxService.cs       → Location autocomplete
   ConsoleEmailSender.cs     → SMTP email (class: SmtpEmailSender)

📁 Repositories/
   ReservationsRepository.cs → DB CRUD (reservations, payments, coupons)

📁 Models/DB/            → EF Core entities
   Reservation, Customer, Payment, ReservationRate, Coupon, PartyItemDB, PartialPaymentDB

📁 Contracts/            → API DTOs
   CheckoutResponse, SingleAvailabilityResponse, PluginSearchResponse, etc.

📁 Helpers/
   General.cs            → Party JSON builders, date utils, price calculations
   Extensions/MappingExtensions.cs → Price/board/type mapping logic

📁 DataBase/
   AppDbContext.cs       → EF Core context
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

### Search
```
autocomplete → MapBox + WebHotelier.SearchPropertyAsync()
submitSearch → WebHotelier.GetAvailabilityAsync() → Merge multi-room → Apply filters
```

### Book
```
preparePayment → Validate → Re-check avail → Viva.GetPaymentCode() → Save to DB (Pending)
paymentSucceed → Viva.ValidatePayment() → WebHotelier.CreateBooking() → Email
```

---

## ⚠️ Watch Out For

1. **`Task.WaitAll`** used instead of `await Task.WhenAll` (blocking)
2. **Hardcoded test card** in WebHotelierPropertiesService.CreateBooking()
3. **No cancellation tokens** on async operations
4. **Secrets in appsettings.json** (not secure)
5. **Request/response body logging** in middleware (perf impact)

---

## 🧪 How to Test Locally

```bash
# Run API
cd TravelBridge.API
dotnet run

# Swagger
http://localhost:5000/swagger

# Test search
GET /api/plugin/autocomplete?searchQuery=Trikala
GET /api/plugin/submitSearch?checkin=15/06/2025&checkOut=20/06/2025&bbox=[23.377,34.730,26.447,35.773]-35.340-25.134&adults=2&rooms=1&searchTerm=Crete
```

---

## 📝 Adding Features Checklist

### New Endpoint
1. Create class in `/Endpoints/`
2. Add `MapEndpoints(IEndpointRouteBuilder app)` method
3. Register: `builder.Services.AddScoped<MyEndpoint>()`
4. Map: `serviceProvider.GetRequiredService<MyEndpoint>().MapEndpoints(app)`

### New DB Entity
1. Create in `/Models/DB/` extending `BaseModel`
2. Add `DbSet<T>` to `AppDbContext`
3. Configure in `OnModelCreating()` if needed
4. `dotnet ef migrations add MigrationName`

### New External Service
1. Options class in `/Models/Apis/`
2. Service class in `/Services/`
3. Configure HttpClient in `Program.cs`
4. Register service: `builder.Services.AddScoped<MyService>()`

---

## 📚 Full Docs
- [README](./README.md) - User overview
- [Architecture](./architecture/overview.md) - Technical deep-dive
- [Data Models](./architecture/data-models.md) - DB schema
- [API Reference](./api/endpoints.md) - Endpoint details
- [Integrations](./integrations/external-services.md) - External APIs
- [Improvements](./improvements.md) - TODO list
