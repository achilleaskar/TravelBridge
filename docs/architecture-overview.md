# TravelBridge Architecture Overview

## System Architecture

TravelBridge follows a modular architecture with clear separation of concerns. The system is designed to be extensible for future hotel providers and payment gateways.

```
┌─────────────────────────────────────────────────────────────────────┐
│                        WordPress Plugin (FE)                         │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ HTTP/REST
                                 ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         TravelBridge.API                             │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────────┐    │
│  │   Search     │  │    Hotel     │  │     Reservation         │    │
│  │  Endpoints   │  │  Endpoints   │  │      Endpoints          │    │
│  └──────┬───────┘  └──────┬───────┘  └───────────┬─────────────┘    │
│         │                 │                       │                  │
│         └────────────────┬┴───────────────────────┘                  │
│                          ▼                                           │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │               WebHotelierPropertiesService                   │    │
│  │          (Orchestrates hotel operations)                     │    │
│  └──────────────────────────┬──────────────────────────────────┘    │
│                             │                                        │
│  ┌──────────────────────────┼──────────────────────────────────┐    │
│  │                          ▼                                   │    │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌────────────┐   │    │
│  │  │ Availability    │  │   Checkout      │  │   SMTP     │   │    │
│  │  │   Processor     │  │   Processor     │  │  Email     │   │    │
│  │  └─────────────────┘  └─────────────────┘  └────────────┘   │    │
│  │               Services Layer                                 │    │
│  └──────────────────────────────────────────────────────────────┘    │
│                             │                                        │
│  ┌──────────────────────────┼──────────────────────────────────┐    │
│  │                          ▼                                   │    │
│  │  ┌─────────────────────────────────────────────────────┐    │    │
│  │  │            ReservationsRepository                    │    │    │
│  │  └────────────────────────┬────────────────────────────┘    │    │
│  │               Data Access Layer                              │    │
│  └──────────────────────────┼──────────────────────────────────┘    │
└─────────────────────────────┼───────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────┐
│                          MySQL/MariaDB                               │
└─────────────────────────────────────────────────────────────────────┘

External Services:
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│   WebHotelier   │  │   Viva Wallet   │  │ MapBox/HereMaps │
│  (Hotel Data)   │  │   (Payments)    │  │   (Geocoding)   │
└─────────────────┘  └─────────────────┘  └─────────────────┘
```

## Project Dependencies

```
TravelBridge.API
├── TravelBridge.Contracts (Shared DTOs, enums, models)
├── TravelBridge.Providers.WebHotelier (Hotel provider)
├── TravelBridge.Payments.Viva (Payment gateway)
├── TravelBridge.Geo.Mapbox (Location services)
├── TravelBridge.Geo.HereMaps (Location services)
└── TravelBridge.Application (Application layer models)
```

## Component Overview

### 1. TravelBridge.API (Main Project)

The core API project that exposes REST endpoints and orchestrates all operations.

#### Endpoints

| Class | Path | Responsibility |
|-------|------|----------------|
| `SearchPluginEndpoints` | `/api/plugin/*` | Location autocomplete, hotel search |
| `HotelEndpoint` | `/api/hotel/*` | Hotel info, room info, availability |
| `ReservationEndpoints` | `/api/reservation/*` | Checkout, payments, bookings |

#### Key Services

| Service | Responsibility |
|---------|----------------|
| `WebHotelierPropertiesService` | Orchestrates WebHotelier API calls, booking creation |
| `AvailabilityProcessor` | Filters hotels by party availability |
| `CheckoutProcessor` | Calculates payment schedules |
| `SmtpEmailSender` | Sends booking confirmation emails |

#### Data Access

| Class | Responsibility |
|-------|----------------|
| `AppDbContext` | Entity Framework Core context |
| `ReservationsRepository` | Reservation CRUD operations |

### 2. TravelBridge.Contracts

Shared contracts used across all projects:

- **DTOs**: Response models for API
- **Enums**: `BookingStatus`, `PaymentStatus`, `Provider`, `CouponType`
- **Models**: `HotelData`, `SingleHotelRoom`, `SingleHotelRate`
- **Common**: `PartyItem`, `Alternative`, `BBox`

### 3. TravelBridge.Providers.WebHotelier

Integration with WebHotelier API for hotel inventory.

| Class | Responsibility |
|-------|----------------|
| `WebHotelierClient` | HTTP client for WebHotelier API |
| `WHAvailabilityRequest` | Multi-property availability request |
| `WHSingleAvailabilityRequest` | Single property availability request |

### 4. TravelBridge.Payments.Viva

Viva Wallet payment gateway integration.

| Class | Responsibility |
|-------|----------------|
| `VivaService` | Payment order creation and validation |
| `VivaAuthService` | OAuth2 authentication with Viva |

### 5. TravelBridge.Geo.Mapbox & TravelBridge.Geo.HereMaps

Location services for autocomplete and geocoding.

| Class | Responsibility |
|-------|----------------|
| `MapBoxService` | Geocoding and location search via MapBox |
| `HereMapsService` | Alternative geocoding via HereMaps |

## Data Flow Patterns

### 1. Hotel Search Flow

```
User Input → SearchPluginEndpoints.GetSearchResults()
    → Validate parameters (dates, bbox, party)
    → WebHotelierPropertiesService.GetAvailabilityAsync()
        → WebHotelierClient.GetAvailabilityAsync() [per party config]
        → Merge responses by hotel
        → AvailabilityProcessor.FilterHotelsByAvailability()
    → Apply filters (price, type, board, rating)
    → Return PluginSearchResponse
```

### 2. Booking Flow

```
User Selection → ReservationEndpoints.PreparePayment()
    → Validate rates and availability
    → CheckoutProcessor.CalculatePayments()
    → VivaService.GetPaymentCode() [creates Viva order]
    → ReservationsRepository.CreateTemporaryExternalReservation()
    → Return OrderCode to frontend

User Pays → Viva Wallet redirects to success/failure URL

Payment Success → ReservationEndpoints.ConfirmPayment()
    → VivaService.ValidatePayment()
    → ReservationsRepository.UpdatePaymentSucceed()
    → WebHotelierPropertiesService.CreateBooking()
        → WebHotelierClient.CreateBookingAsync() [per rate]
    → SendConfirmationEmail()
```

### 3. Autocomplete Flow

```
User Types → SearchPluginEndpoints.GetAutocompleteResults()
    → Parallel execution:
        → WebHotelierPropertiesService.SearchPropertyFromWebHotelierAsync()
        → MapBoxService.GetLocationsAsync()
    → Combine results
    → Return AutoCompleteResponse
```

## Configuration

Configuration is managed through `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MariaDBConnection": "<connection_string>"
  },
  "WebHotelierApi": {
    "BaseUrl": "<url>",
    "Username": "<user>",
    "Password": "<pass>"
  },
  "VivaApi": {
    "BaseUrl": "<url>",
    "ClientId": "<id>",
    "ClientSecret": "<secret>",
    "SourceCode": "<code>"
  },
  "MapBoxApi": {
    "BaseUrl": "<url>",
    "ApiKey": "<key>"
  },
  "Smtp": {
    "Host": "<host>",
    "Port": "<port>",
    "Username": "<user>",
    "Password": "<pass>",
    "From": "<email>"
  }
}
```

## Dependency Injection Setup

Services are registered in `Program.cs`:

```csharp
// Database
builder.Services.AddDbContext<AppDbContext>(options => ...);

// External providers (via extension methods)
builder.Services.AddHereMaps(builder.Configuration);
builder.Services.AddMapBox(builder.Configuration);
builder.Services.AddWebHotelier(builder.Configuration);

// Payment services
builder.Services.AddScoped<VivaService>();
builder.Services.AddScoped<VivaAuthService>();

// Application services
builder.Services.AddScoped<WebHotelierPropertiesService>();
builder.Services.AddScoped<ReservationsRepository>();
builder.Services.AddSingleton<SmtpEmailSender>();

// Endpoints
builder.Services.AddScoped<SearchPluginEndpoints>();
builder.Services.AddScoped<HotelEndpoint>();
builder.Services.AddScoped<ReservationEndpoints>();
```

## Logging Strategy

The project uses Serilog with:

- **Console sink**: For development
- **File sink**: Rolling daily logs with 30-day retention
- **Request logging middleware**: Tracks request/response timing

Log levels are configured to minimize noise:
- `Microsoft.AspNetCore`: Warning
- `Microsoft.EntityFrameworkCore`: Warning
- `System.Net.Http.HttpClient`: Warning
- Application code: Information

## Security Considerations

1. **CORS**: Currently configured as "AllowAll" - should be restricted in production
2. **Authentication**: No authentication implemented - relies on frontend security
3. **Payment Security**: Card details are passed to WebHotelier, not stored locally
4. **Input Validation**: All endpoints validate required parameters

## Extensibility Points

The architecture supports future extensions:

1. **New Hotel Providers**: Add new provider project, implement similar client pattern
2. **New Payment Gateways**: Add new payment project, implement payment interface
3. **New Geo Services**: Add new geo project with extension method
4. **Caching**: Add Redis/memory cache layer between services and clients
