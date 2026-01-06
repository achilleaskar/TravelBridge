# TravelBridge Database Schema

This document describes the database schema used by TravelBridge. The system uses MySQL/MariaDB with Entity Framework Core.

## Database Configuration

- **Database Engine**: MariaDB 10.11.10
- **Collation**: `utf8mb4_general_ci` (supports Greek and Latin characters)
- **ORM**: Entity Framework Core
- **Connection**: Configured in `appsettings.json` under `ConnectionStrings:MariaDBConnection`

## Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────────┐       ┌──────────────────┐
│    Customer     │       │    Reservation      │       │  ReservationRate │
├─────────────────┤       ├─────────────────────┤       ├──────────────────┤
│ Id (PK)         │──────<│ CustomerId (FK)     │       │ Id (PK)          │
│ FirstName       │       │ Id (PK)             │──────<│ ReservationId(FK)│
│ LastName        │       │ CheckIn             │       │ HotelCode        │
│ Email           │       │ CheckOut            │       │ RateId           │
│ Tel             │       │ HotelCode           │       │ Name             │
│ CountryCode     │       │ HotelName           │       │ Price            │
│ Notes           │       │ TotalAmount         │       │ NetPrice         │
│ DateCreated     │       │ TotalRooms          │       │ Quantity         │
└─────────────────┘       │ BookingStatus       │       │ Provider         │
         │                │ Party               │       │ BookingStatus    │
         │                │ Coupon              │       │ ProviderResId    │
         │                │ RemainingAmount     │       │ CancelationInfo  │
         │                │ CheckInTime         │       │ BoardInfo        │
         │                │ CheckOutTime        │       │ DateCreated      │
         │                │ DateCreated         │       │ DateFinalized    │
         │                │ DateFinalized       │       └──────────────────┘
         │                └─────────────────────┘               │
         │                         │                            │
         │                         │                   ┌────────┴────────┐
         │                         │                   │   PartyItemDB   │
         │                         │                   ├─────────────────┤
         │                         │                   │ Id (PK)         │
         │                         │                   │ ReservationRate │
         │                         │                   │   Id (FK)       │
         │                         │                   │ Adults          │
         │                         │                   │ Children        │
         │                         │                   │ Party           │
         │                         │                   └─────────────────┘
         │                         │
         │    ┌────────────────────┴──────────────────┐
         │    │                                       │
         ▼    ▼                                       ▼
┌─────────────────┐                    ┌─────────────────────────┐
│    Payment      │                    │    PartialPaymentDB     │
├─────────────────┤                    ├─────────────────────────┤
│ Id (PK)         │                    │ Id (PK)                 │
│ CustomerId (FK) │                    │ ReservationId (FK)      │
│ ReservationId(FK)│                   │ prepayAmount            │
│ Amount          │                    └─────────────────────────┘
│ OrderCode       │                              │
│ TransactionId   │                              │
│ PaymentProvider │                    ┌─────────┴─────────┐
│ PaymentStatus   │                    │  NextPaymentDB    │
│ DateCreated     │                    ├───────────────────┤
│ DateFinalized   │                    │ Id (PK)           │
└─────────────────┘                    │ PartialPaymentId  │
                                       │   (FK)            │
                                       │ Amount            │
┌─────────────────┐                    │ DueDate           │
│     Coupon      │                    └───────────────────┘
├─────────────────┤
│ Id (PK)         │
│ Code            │
│ CouponType      │
│ Percentage      │
│ Amount          │
│ Expiration      │
│ DateCreated     │
└─────────────────┘
```

## Entities

### Customer

Stores customer information for reservations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| FirstName | VARCHAR(50) | NOT NULL | Customer's first name |
| LastName | VARCHAR(50) | NOT NULL | Customer's last name |
| Email | VARCHAR(80) | NOT NULL | Customer's email address |
| Tel | VARCHAR(20) | Regex validated | Phone number (E.164 format) |
| CountryCode | CHAR(2) | NULL | ISO 2-letter country code |
| Notes | TEXT | NOT NULL | Special requests or notes |
| DateCreated | DATETIME | Default CURRENT_TIMESTAMP | Record creation timestamp |

**Relationships**:
- One-to-Many with `Payment`
- One-to-Many with `Reservation`

---

### Reservation

Stores booking information.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique reservation ID |
| CheckIn | DATE | NOT NULL | Check-in date |
| CheckOut | DATE | NOT NULL | Check-out date |
| HotelCode | VARCHAR(50) | NULL | Hotel identifier (e.g., "1-VAROSRESID") |
| HotelName | VARCHAR(70) | NULL | Hotel display name |
| TotalAmount | DECIMAL(10,2) | NOT NULL | Total reservation amount |
| TotalRooms | TINYINT UNSIGNED | 0-100 | Number of rooms booked |
| BookingStatus | ENUM | NOT NULL | Current booking status |
| Party | VARCHAR(150) | NULL | Party configuration JSON |
| Coupon | VARCHAR(50) | NULL | Applied coupon code |
| RemainingAmount | DECIMAL(10,2) | NOT NULL | Amount still to be paid |
| CheckInTime | VARCHAR(10) | NOT NULL | Hotel check-in time |
| CheckOutTime | VARCHAR(10) | NOT NULL | Hotel check-out time |
| CustomerId | INT | FK | Reference to Customer |
| DateCreated | DATETIME | Default CURRENT_TIMESTAMP | Record creation timestamp |
| DateFinalized | DATETIME | NULL | When booking was confirmed/cancelled |

**Relationships**:
- Many-to-One with `Customer`
- One-to-Many with `Payment`
- One-to-Many with `ReservationRate`
- One-to-One with `PartialPaymentDB`

---

### ReservationRate

Stores individual room/rate selections within a reservation.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| ReservationId | INT | FK, NOT NULL | Parent reservation |
| HotelCode | VARCHAR(50) | NULL | Hotel identifier |
| RateId | VARCHAR(50) | NOT NULL | Rate identifier from provider |
| Name | VARCHAR(100) | NULL | Room type name |
| Price | DECIMAL(10,2) | NOT NULL | Total price for this rate |
| NetPrice | DECIMAL(10,2) | NOT NULL | Net price (cost) |
| Quantity | INT | NOT NULL | Number of rooms with this rate |
| Provider | ENUM | NOT NULL | Hotel provider (WebHotelier) |
| BookingStatus | ENUM | NOT NULL | Status of this specific rate |
| ProviderResId | INT | NULL | Reservation ID from provider |
| CancelationInfo | VARCHAR(200) | NULL | Cancellation policy text |
| BoardInfo | VARCHAR(100) | NULL | Board type description |
| DateCreated | DATETIME | Default CURRENT_TIMESTAMP | Record creation timestamp |
| DateFinalized | DATETIME | NULL | When rate was confirmed |

**Relationships**:
- Many-to-One with `Reservation` (Cascade delete)
- One-to-One with `PartyItemDB`

---

### PartyItemDB

Stores party configuration for each rate.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| ReservationRateId | INT | FK | Parent rate |
| Adults | INT | NOT NULL | Number of adults |
| Children | VARCHAR(50) | NULL | Children ages (comma-separated) |
| Party | VARCHAR(100) | NULL | Original party JSON |

---

### Payment

Stores payment transactions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| CustomerId | INT | FK, NULL | Associated customer |
| ReservationId | INT | FK, NULL | Associated reservation |
| Amount | DECIMAL(10,2) | NOT NULL | Payment amount |
| OrderCode | VARCHAR(50) | NULL | Viva Wallet order code |
| TransactionId | VARCHAR(50) | NULL | Viva Wallet transaction ID |
| PaymentProvider | ENUM | NOT NULL | Payment provider (Viva) |
| PaymentStatus | ENUM | NOT NULL | Current payment status |
| DateCreated | DATETIME | Default CURRENT_TIMESTAMP | Record creation timestamp |
| DateFinalized | DATETIME | NULL | When payment completed/failed |

**Relationships**:
- Many-to-One with `Customer`
- Many-to-One with `Reservation`

---

### PartialPaymentDB

Stores partial payment configuration for reservations with installments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| ReservationId | INT | FK | Associated reservation |
| prepayAmount | DECIMAL(10,2) | NOT NULL | Initial prepayment amount |

**Relationships**:
- One-to-One with `Reservation`
- One-to-Many with `NextPaymentDB`

---

### NextPaymentDB

Stores scheduled future payments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| PartialPaymentId | INT | FK | Parent partial payment |
| Amount | DECIMAL(10,2) | NOT NULL | Payment amount due |
| DueDate | DATETIME | NULL | Due date for payment |

---

### Coupon

Stores discount coupon definitions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | INT | PK, Auto-increment | Unique identifier |
| Code | VARCHAR(50) | UNIQUE, NOT NULL | Coupon code (case-insensitive) |
| CouponType | ENUM | NOT NULL | Type (flat/percentage) |
| Percentage | DECIMAL(5,2) | NULL | Discount percentage (0-100) |
| Amount | DECIMAL(10,2) | NULL | Flat discount amount |
| Expiration | DATETIME | NOT NULL | Coupon expiry date |
| DateCreated | DATETIME | Default CURRENT_TIMESTAMP | Record creation timestamp |

---

## Enumerations

### BookingStatus

| Value | Int | Description |
|-------|-----|-------------|
| New | 0 | Initial state |
| Pending | 1 | Awaiting payment |
| Running | 2 | Processing booking with provider |
| Confirmed | 3 | Booking confirmed |
| Cancelled | 4 | Booking cancelled |
| Error | 5 | Error occurred |

### PaymentStatus

| Value | Int | Description |
|-------|-----|-------------|
| Pending | 1 | Awaiting payment |
| Success | 2 | Payment successful |
| Failed | 3 | Payment failed |

### PaymentProvider

| Value | Int | Description |
|-------|-----|-------------|
| Viva | 1 | Viva Wallet |

### Provider

| Value | Int | Description |
|-------|-----|-------------|
| WebHotelier | 1 | WebHotelier |

### CouponType

| Value | Int | Description |
|-------|-----|-------------|
| none | 0 | No coupon |
| flat | 1 | Fixed amount discount |
| percentage | 2 | Percentage discount |

---

## Indexes and Constraints

### Foreign Key Relationships

```sql
-- Customer → Payment
ALTER TABLE Payments
ADD CONSTRAINT FK_Payments_Customer
FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
ON DELETE RESTRICT;

-- Customer → Reservation
ALTER TABLE Reservations
ADD CONSTRAINT FK_Reservations_Customer
FOREIGN KEY (CustomerId) REFERENCES Customers(Id)
ON DELETE RESTRICT;

-- Reservation → Payment
ALTER TABLE Payments
ADD CONSTRAINT FK_Payments_Reservation
FOREIGN KEY (ReservationId) REFERENCES Reservations(Id)
ON DELETE RESTRICT;

-- Reservation → ReservationRate
ALTER TABLE ReservationRates
ADD CONSTRAINT FK_ReservationRates_Reservation
FOREIGN KEY (ReservationId) REFERENCES Reservations(Id)
ON DELETE CASCADE;
```

---

## Migrations

The project uses EF Core migrations located in `TravelBridge.API/Migrations/`.

Key migrations:
- `20250309220005_Initial` - Initial schema
- `20250310011204_emailInCustomer` - Added email to customer
- `20250503182342_searchParty` - Added party tracking
- `20250508053940_paymentFixes` - Payment improvements
- `20250528184209_coupons` - Added coupon system
- `20250601111509_coupons2` - Coupon enhancements

### Running Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName -p TravelBridge.API

# Update database
dotnet ef database update -p TravelBridge.API

# Generate SQL script
dotnet ef migrations script -p TravelBridge.API
```

---

## Database Context

The `AppDbContext` class in `TravelBridge.API/DataBase/AppDbContext.cs` configures:

1. **UTF8MB4 Collation** - For international character support
2. **Automatic DateCreated** - Default timestamp for all BaseModel entities
3. **Relationship configurations** - Proper foreign key constraints

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationRate> ReservationRates { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
}
```
