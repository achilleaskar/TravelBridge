# TravelBridge Documentation

TravelBridge is a hotel booking platform that serves as a backend API for a WordPress plugin. It provides hotel search, availability checking, room booking, and payment processing capabilities similar to platforms like Booking.com.

## Documentation Index

| Document | Description |
|----------|-------------|
| [Architecture Overview](./architecture-overview.md) | System architecture, project structure, and component interactions |
| [API Endpoints](./api-endpoints.md) | Complete API reference with request/response examples |
| [Database Schema](./database-schema.md) | Database models, relationships, and entity descriptions |
| [Payment Flow](./payment-flow.md) | Viva Wallet payment integration and payment lifecycle |
| [Hotel Provider Integration](./hotel-provider-integration.md) | WebHotelier API integration details |
| [Geo Services](./geo-services.md) | MapBox and HereMaps location services |
| [Business Rules](./business-rules.md) | Pricing calculations, availability logic, and coupon system |
| [Deployment & Configuration](./deployment-configuration.md) | Configuration settings and deployment guidelines |

### AI Assistant Resources

| Document | Description |
|----------|-------------|
| [Quick Context](./ai/quick-context.md) | Quick-load reference for AI assistants |
| [Coding Standards](./ai/coding-standards.md) | C#/.NET development best practices |
| [Testing Guidelines](./ai/testing-guidelines.md) | Test structure and workflow |
| [Workflows](./ai/workflows.md) | AI prompts and golden workflow |

## Quick Start

### Technology Stack

- **.NET 9** - Core framework
- **ASP.NET Core Minimal APIs** - Web API layer
- **Entity Framework Core** - ORM with MySQL/MariaDB
- **Serilog** - Structured logging
- **Swagger/OpenAPI** - API documentation

### External Services

- **WebHotelier** - Hotel inventory and booking provider
- **Viva Wallet** - Payment gateway
- **MapBox** - Location autocomplete and geocoding
- **HereMaps** - Alternative location services

### Key Features

1. **Hotel Search** - Search hotels by location, dates, and party configuration
2. **Availability Check** - Real-time room availability with pricing
3. **Room Booking** - Multi-room booking with flexible party configurations
4. **Payment Processing** - Secure payments via Viva Wallet with partial payment support
5. **Email Notifications** - Booking confirmation emails
6. **Coupon System** - Percentage and flat discount coupons

## Project Structure

```
TravelBridge/
├── TravelBridge.API/              # Main API project
│   ├── Endpoints/                 # API endpoint definitions
│   ├── Models/                    # Database and API models
│   ├── Services/                  # Business logic services
│   ├── Repositories/              # Data access layer
│   ├── Contracts/                 # DTOs and response models
│   └── Helpers/                   # Utility classes
├── TravelBridge.Contracts/        # Shared contracts and models
├── TravelBridge.Providers.WebHotelier/  # WebHotelier integration
├── TravelBridge.Payments.Viva/    # Viva payment integration
├── TravelBridge.Geo.Mapbox/       # MapBox location services
├── TravelBridge.Geo.HereMaps/     # HereMaps location services
├── TravelBridge.Application/      # Application layer models
└── TravelBridge.Tests/            # Unit and integration tests
```

## Version

- **API Version**: v1
- **Documentation Version**: 1.0
- **Last Updated**: 2025
