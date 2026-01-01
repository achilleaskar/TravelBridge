# TravelBridge.Core

## Purpose
This is the **domain layer** of TravelBridge. It contains:
- **Pure business logic** with zero dependencies on infrastructure
- **Domain entities** with rich behavior (not just property bags)
- **Interfaces** that infrastructure must implement
- **Value objects** for immutable domain concepts

## Architecture Rules
1. ✅ **No dependencies** on external libraries (except .NET BCL)
2. ✅ **No database** or HTTP code
3. ✅ **Interfaces only** - implementations live in Infrastructure
4. ✅ **Testable** without mocks (pure logic)

## Folder Structure
- `Entities/` - Rich domain models (Reservation, Booking, etc.)
- `Interfaces/` - Contracts for repositories and services
- `Services/` - Business logic services (pricing, booking rules)
- `ValueObjects/` - Immutable types (Money, DateRange, etc.)

## Dependencies
- ✅ None (Core is dependency-free)
- Infrastructure → Core (allowed)
- API → Core (allowed)
- Core → Infrastructure (❌ NEVER!)
