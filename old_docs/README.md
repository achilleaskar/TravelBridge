# TravelBridge API Documentation

## Overview

TravelBridge is a **Hotel Booking API** built with **.NET 9 / ASP.NET Core Minimal APIs** that aggregates hotel availability from external providers (primarily **WebHotelier**), processes payments through **Viva Wallet**, and manages reservations in a **MariaDB** database.

The API serves as a backend for travel booking websites (currently `my-diakopes.gr` and `travelproject.gr`).

---

## Quick Links

| Document | Description |
|----------|-------------|
| [Architecture Overview](./architecture/overview.md) | Technical architecture, project structure, and data flow |
| [API Endpoints](./api/endpoints.md) | Complete API reference with examples |
| [Data Models](./architecture/data-models.md) | Database entities and relationships |
| [External Integrations](./integrations/external-services.md) | WebHotelier, Viva, MapBox integrations |
| [Suggested Improvements](./improvements.md) | Architectural and code improvements |

---

## Key Features

### 🏨 Hotel Search & Availability
- **Location autocomplete** via MapBox API
- **Hotel search** with filters (price, rating, board type, hotel type)
- **Real-time availability** from WebHotelier
- **Multi-room booking** support with party composition

### 💳 Payment Processing
- **Viva Wallet** integration for secure payments
- **Partial payments** with scheduled installments
- **Coupon/discount** support (percentage or flat)

### 📧 Booking Management
- **Reservation creation** and confirmation
- **Email notifications** with booking details
- **Cancellation handling**

---

## Technology Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 9, ASP.NET Core Minimal APIs |
| Database | MariaDB 10.11 with EF Core |
| Authentication | Basic Auth (WebHotelier), OAuth2 (Viva) |
| Logging | Serilog (Console + File) |
| API Docs | Swagger/OpenAPI |

---

## Project Structure

```
TravelBridge.API/
├── Endpoints/              # API endpoint definitions
│   ├── SearchPluginEndpoints.cs
│   ├── HotelEndpoint.cs
│   └── ReservationEndpoints.cs
├── Services/               # Business logic & external integrations
│   ├── WebHotelier/        # Hotel provider integration
│   ├── Viva/               # Payment processing
│   └── ExternalServices/   # MapBox, HereMaps
├── Repositories/           # Data access layer
├── Models/                 # Domain models
│   ├── DB/                 # EF Core entities
│   ├── WebHotelier/        # Provider DTOs
│   └── Apis/               # Configuration options
├── Contracts/              # API request/response DTOs
├── Helpers/                # Utilities and extensions
├── DataBase/               # DbContext
└── Migrations/             # EF Core migrations
```

---

## Getting Started

### Prerequisites
- .NET 9 SDK
- MariaDB 10.11+
- API keys for: WebHotelier, Viva Wallet, MapBox

### Configuration

All configuration is in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MariaDBConnection": "server=...;database=...;user=...;password=..."
  },
  "WebHotelierApi": {
    "BaseUrl": "https://rest.reserve-online.net/",
    "Username": "...",
    "Password": "..."
  },
  "VivaApi": {
    "ApiKey": "...",
    "ApiSecret": "...",
    "SourceCode": "...",
    "BaseUrl": "https://api.vivapayments.com/"
  },
  "MapBoxApi": {
    "ApiKey": "...",
    "BaseUrl": "https://api.mapbox.com/"
  }
}
```

### Run the API

```bash
cd TravelBridge.API
dotnet run
```

Access Swagger UI at: `http://localhost:5000/swagger`

---

## Main Workflows

### 1. Search Hotels
```
User → /api/plugin/autocomplete → MapBox + WebHotelier
User → /api/plugin/submitSearch → WebHotelier availability
```

### 2. Book a Room
```
User → /api/hotel/HotelFullInfo → Get hotel details + availability
User → /api/reservation/checkout → Get checkout summary
User → /api/reservation/preparePayment → Create reservation + Viva order
User → Viva Payment Page → Complete payment
Viva → /api/reservation/paymentSucceed → Confirm booking → WebHotelier
```

### 3. Cancel Booking
```
User → /api/reservation/cancelBooking → WebHotelier cancellation
```

---

## Support

For questions or issues, contact the development team.
