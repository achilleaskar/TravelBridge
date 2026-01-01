# TravelBridge Architecture Overview

> **Purpose**: Quick reference for AI assistants and developers to understand the codebase without re-analyzing the entire project.

---

## 📐 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT APPLICATIONS                          │
│              (my-diakopes.gr, travelproject.gr)                    │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ HTTP/REST
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      TRAVELBRIDGE API (.NET 9)                      │
│  ┌─────────────┐  ┌─────────────┐  ┌────────────────────────────┐  │
│  │  Endpoints  │  │  Services   │  │      Repositories          │  │
│  │             │  │             │  │                            │  │
│  │ - Search    │──│ - WebHotel  │──│ - ReservationsRepository   │  │
│  │ - Hotel     │  │ - Viva      │  │                            │  │
│  │ - Reserv.   │  │ - MapBox    │  │                            │  │
│  └─────────────┘  └──────┬──────┘  └────────────┬───────────────┘  │
└──────────────────────────┼─────────────────────┼────────────────────┘
                           │                     │
           ┌───────────────┴────────┐            │
           ▼           ▼            ▼            ▼
    ┌───────────┐ ┌─────────┐ ┌─────────┐ ┌───────────┐
    │WebHotelier│ │  Viva   │ │ MapBox  │ │  MariaDB  │
    │   API     │ │ Wallet  │ │   API   │ │           │
    └───────────┘ └─────────┘ └─────────┘ └───────────┘
```

---

## 🧩 Component Breakdown

### 1. Endpoints Layer (`/Endpoints/`)

| File | Purpose | Key Endpoints |
|------|---------|---------------|
| `SearchPluginEndpoints.cs` | Hotel search and autocomplete | `/api/plugin/autocomplete`, `/api/plugin/submitSearch` |
| `HotelEndpoint.cs` | Hotel details and availability | `/api/hotel/hotelInfo`, `/api/hotel/HotelFullInfo`, `/api/hotel/roomInfo` |
| `ReservationEndpoints.cs` | Booking workflow | `/api/reservation/checkout`, `/api/reservation/preparePayment`, `/api/reservation/paymentSucceed` |

**Pattern**: Each endpoint class:
- Injects services via constructor
- Defines `MapEndpoints(IEndpointRouteBuilder app)` method
- Uses record types for request parameters
- Adds Swagger customization via `WithOpenApi()`

### 2. Services Layer (`/Services/`)

| Service | Responsibility |
|---------|----------------|
| `WebHotelierPropertiesService` | Hotel search, availability, booking creation, cancellation, email sending |
| `VivaService` | Payment order creation, payment validation |
| `VivaAuthService` | OAuth2 token management with caching |
| `MapBoxService` | Location autocomplete |
| `SmtpEmailSender` | Booking confirmation emails |

### 3. Repository Layer (`/Repositories/`)

| Repository | Responsibility |
|------------|----------------|
| `ReservationsRepository` | CRUD for reservations, payments, coupons |

**Key Methods**:
- `CreateTemporaryExternalReservation()` - Creates pending reservation before payment
- `UpdatePaymentSucceed()` - Marks payment as successful
- `GetReservationBasicDataByPaymentCode()` - Fetches reservation by Viva order code
- `RetrieveCoupon()` - Validates and retrieves coupon

### 4. Models (`/Models/`)

```
Models/
├── DB/                    # EF Core entities (persisted)
│   ├── Reservation.cs
│   ├── Customer.cs
│   ├── Payment.cs
│   ├── ReservationRate.cs
│   ├── Coupon.cs
│   └── BaseModel.cs       # Common: Id, DateCreated
├── WebHotelier/           # DTOs from WebHotelier API
├── ExternalModels/        # DTOs for Viva, MapBox
└── Apis/                  # Configuration option classes
```

### 5. Contracts (`/Contracts/`)

API request/response DTOs separate from domain models:
- `CheckoutResponse`, `SingleAvailabilityResponse`, `PluginSearchResponse`
- `BookingResponse`, `PreparePaymentResponse`, `SuccessfullPaymentResponse`

---

## 🔄 Key Business Flows

### Search Flow
```
1. /autocomplete → MapBoxService.GetLocations() + WebHotelierPropertiesService.SearchPropertyAsync()
2. /submitSearch → WebHotelierPropertiesService.GetAvailabilityAsync()
   - Builds party JSON from adults/children
   - Parallel requests for multi-room searches
   - Merges results, applies filters
   - Returns PluginSearchResponse with filters
```

### Booking Flow
```
1. /checkout or /HotelFullInfo → Get availability + hotel info
2. /preparePayment:
   a. Validate dates, party, rates
   b. Re-check availability (price validation)
   c. Create Viva payment order (VivaService.GetPaymentCode)
   d. Save reservation to DB (status: Pending)
   e. Return orderCode for redirect to Viva
3. User pays on Viva
4. /paymentSucceed:
   a. Validate payment with Viva (VivaService.ValidatePayment)
   b. Update payment status in DB
   c. Create booking in WebHotelier (WebHotelierPropertiesService.CreateBooking)
   d. Send confirmation email
```

### Cancellation Flow
```
1. /cancelBooking → WebHotelierPropertiesService.CancelBooking()
   - Calls WebHotelier /purge/{resId}
   - Updates DB status
```

---

## 🗄️ Database Schema

### Entity Relationships
```
                           ┌─────────────┐
                           │   Coupon    │  (standalone)
                           └─────────────┘

┌──────────────┐
│   Customer   │
└──────┬───────┘
       │
       ├────────────────────────────────────┐
       │ (1:N)                              │ (1:N)
       ▼                                    ▼
┌──────────────┐                    ┌─────────────┐
│ Reservation  │◄───────────────────│   Payment   │
└──────┬───────┘      (1:N)         └─────────────┘
       │
       ├─────────────────────┐
       │ (1:N, Cascade)      │ (1:1, Owned)
       ▼                     ▼
┌─────────────────┐   ┌──────────────────┐
│ ReservationRate │   │ PartialPaymentDB │
└────────┬────────┘   └────────┬─────────┘
         │ (1:1, Owned)        │ (1:N)
         ▼                     ▼
  ┌─────────────┐       ┌───────────────┐
  │ PartyItemDB │       │ NextPaymentDB │
  └─────────────┘       └───────────────┘
```

**Relationship Details:**
- `Customer` → `Reservation`: One customer can have many reservations
- `Customer` → `Payment`: One customer can have many payments  
- `Reservation` → `Payment`: One reservation can have many payments
- `Reservation` → `ReservationRate`: One reservation has many room rates (cascade delete)
- `Reservation` → `PartialPaymentDB`: One reservation has one partial payment schedule (owned)
- `ReservationRate` → `PartyItemDB`: Each rate has one party composition (owned)
- `PartialPaymentDB` → `NextPaymentDB`: One partial payment has many scheduled payments

---

## ⚙️ Configuration & DI

### HttpClient Registration (Program.cs)
```csharp
// Named HttpClients with typed configuration
builder.Services.Configure<WebHotelierApiOptions>(config.GetSection("WebHotelierApi"));
builder.Services.AddHttpClient("WebHotelierApi", (sp, client) => {
    // Basic auth header added
});

builder.Services.Configure<VivaApiOptions>(config.GetSection("VivaApi"));
builder.Services.AddHttpClient("VivaApi", ...);

builder.Services.Configure<MapBoxApiOptions>(config.GetSection("MapBoxApi"));
builder.Services.AddHttpClient("MapBoxApi", ...);
```

### Service Registration
```csharp
builder.Services.AddScoped<WebHotelierPropertiesService>();
builder.Services.AddScoped<VivaService>();
builder.Services.AddScoped<VivaAuthService>();
builder.Services.AddScoped<MapBoxService>();
builder.Services.AddScoped<ReservationsRepository>();
builder.Services.AddSingleton<SmtpEmailSender>();
```

---

## 🔑 Key Patterns & Conventions

### 1. Party JSON Format
Multi-room bookings use a party array:
```json
[{"adults":2,"children":[5,10]},{"adults":1}]
```

### 2. Hotel ID Format
`{provider}-{hotelCode}` e.g., `1-VAROSVILL`

### 3. Rate ID Format  
`{rateId}-{adults}_{child1}_{child2}` e.g., `328000-226-2_5_10`

### 4. Date Formats
- API input: `dd/MM/yyyy`
- WebHotelier: `yyyy-MM-dd`

### 5. Pricing Logic
- 10% minimum margin over net price
- Coupon discounts applied after margin
- Partial payments calculated with merge logic for installments

### 6. Board Type Mapping
Board IDs mapped to Greek/English names. Key mappings:
- `0` → `14` (Room Only)
- `10` → Full Board
- `12` → Half Board

---

## 🛡️ Error Handling

- Most errors throw exceptions that bubble up
- Payment failures update payment status and return error response
- Booking failures attempt rollback/cancellation

---

## 📝 Logging

Serilog configured with:
- Console sink
- File sink (`logs/log.txt`, 50MB rolling)
- Request/Response body logging middleware

---

## 🌐 CORS

All origins allowed (`AllowAll` policy) - suitable for multi-tenant frontend.

---

## 📦 External API Integrations

| Provider | Auth | Purpose |
|----------|------|---------|
| WebHotelier | Basic Auth | Hotel data, availability, bookings |
| Viva Wallet | OAuth2 (client credentials) | Payments |
| MapBox | API Key | Location autocomplete |
| HereMaps | API Key | (Available but MapBox used primarily) |

---

## 🔧 Quick Reference: Adding New Features

### Add New Endpoint
1. Create class in `/Endpoints/`
2. Implement `MapEndpoints(IEndpointRouteBuilder app)`
3. Register in `Program.cs`: `builder.Services.AddScoped<MyEndpoint>()`
4. Map in scope: `serviceProvider.GetRequiredService<MyEndpoint>().MapEndpoints(app)`

### Add New External Service
1. Create options class in `/Models/Apis/`
2. Create service class in `/Services/`
3. Configure in `Program.cs`:
   ```csharp
   builder.Services.Configure<MyApiOptions>(config.GetSection("MyApi"));
   builder.Services.AddHttpClient("MyApi", ...);
   builder.Services.AddScoped<MyService>();
   ```

### Add New DB Entity
1. Create class in `/Models/DB/` extending `BaseModel`
2. Add `DbSet<T>` to `AppDbContext`
3. Configure relationships in `OnModelCreating`
4. Create migration: `dotnet ef migrations add MyMigration`
