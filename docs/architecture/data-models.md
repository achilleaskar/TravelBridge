# TravelBridge Data Models

## Database Entities (EF Core)

### Entity Relationship Diagram

```
                              ┌─────────────┐
                              │   Coupon    │  (standalone)
                              └─────────────┘

┌─────────────────┐
│    Customer     │
├─────────────────┤
│ Id (PK)         │
│ FirstName       │
│ LastName        │
│ Email           │
│ Tel             │
│ CountryCode     │
│ Notes           │
│ DateCreated     │
└────────┬────────┘
         │
         ├────────────────────────────────────────────┐
         │ 1:N                                        │ 1:N
         ▼                                            ▼
┌─────────────────────┐                      ┌─────────────────┐
│    Reservation      │◄─────────────────────│    Payment      │
├─────────────────────┤       1:N            ├─────────────────┤
│ Id (PK)             │                      │ Id (PK)         │
│ CustomerId (FK)     │                      │ CustomerId (FK) │
│ CheckIn             │                      │ ReservationId   │
│ CheckOut            │                      │ Amount          │
│ HotelCode           │                      │ PaymentProvider │
│ HotelName           │                      │ PaymentStatus   │
│ TotalAmount         │                      │ OrderCode       │
│ TotalRooms          │                      │ TransactionId   │
│ Party               │                      │ DateCreated     │
│ BookingStatus       │                      │ DateFinalized   │
│ RemainingAmount     │                      └─────────────────┘
│ CheckInTime         │
│ CheckOutTime        │
│ Coupon              │
│ DateCreated         │
│ DateFinalized       │
└──────────┬──────────┘
           │
           ├─────────────────────────┐
           │ 1:N (Cascade)           │ 1:1 (Owned)
           ▼                         ▼
┌───────────────────┐     ┌─────────────────────┐
│  ReservationRate  │     │  PartialPaymentDB   │
├───────────────────┤     ├─────────────────────┤
│ Id (PK)           │     │ Id (PK)             │
│ ReservationId(FK) │     │ ReservationId (FK)  │
│ HotelCode         │     │ prepayAmount        │
│ RateId            │     └──────────┬──────────┘
│ Price             │                │ 1:N
│ NetPrice          │                ▼
│ Quantity          │     ┌─────────────────────┐
│ Provider          │     │   NextPaymentDB     │
│ BookingStatus     │     ├─────────────────────┤
│ Name              │     │ Id (PK)             │
│ CancelationInfo   │     │ PartialPaymentId FK │
│ BoardInfo         │     │ Amount              │
│ ProviderResId     │     │ DueDate             │
│ DateCreated       │     └─────────────────────┘
│ DateFinalized     │
└─────────┬─────────┘
          │ 1:1 (Owned)
          ▼
┌─────────────────────┐
│   PartyItemDB       │
├─────────────────────┤
│ Id (PK)             │
│ ReservationRateId   │
│ Adults              │
│ Children (string)   │
│ Party (JSON string) │
└─────────────────────┘

┌─────────────────┐
│     Coupon      │
├─────────────────┤
│ Id (PK)         │
│ Code            │
│ CouponType      │
│ UsageLimit      │
│ UsageLeft       │
│ Percentage      │
│ Amount          │
│ Expiration      │
│ DateCreated     │
└─────────────────┘
```

---

## Entity Details

### BaseModel
All entities inherit from `BaseModel`:
```csharp
public class BaseModel
{
    public int Id { get; set; }
    public DateTime DateCreated { get; set; } // Default: CURRENT_TIMESTAMP
}
```

### Customer
```csharp
public class Customer : BaseModel
{
    [MaxLength(50)] public string FirstName { get; set; }
    [MaxLength(50)] public string LastName { get; set; }
    [MaxLength(80)] public string Email { get; set; }
    [MaxLength(20)] public string Tel { get; set; }      // E.164 format
    [Column(TypeName = "CHAR(2)")] public string? CountryCode { get; set; }
    public string Notes { get; set; }
    
    // Navigation
    public IEnumerable<Payment> Payments { get; set; }
    public IEnumerable<Reservation> Reservations { get; set; }
}
```

### Reservation
```csharp
public class Reservation : BaseModel
{
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    [MaxLength(50)] public string? HotelCode { get; set; }  // Format: "1-HOTELCODE"
    [MaxLength(70)] public string? HotelName { get; set; }
    [Column(TypeName = "DECIMAL(10,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "TINYINT UNSIGNED")] public int TotalRooms { get; set; }
    public BookingStatus BookingStatus { get; set; }
    [MaxLength(150)] public string? Party { get; set; }  // JSON array
    [Column(TypeName = "DECIMAL(10,2)")] public decimal RemainingAmount { get; set; }
    public DateTime DateFinalized { get; set; }
    public string CheckInTime { get; set; }
    public string CheckOutTime { get; set; }
    public string? Coupon { get; set; }
    
    // Navigation
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public List<ReservationRate> Rates { get; set; }
    public List<Payment> Payments { get; set; }
    public PartialPaymentDB PartialPayment { get; set; }
}
```

### Payment
```csharp
public class Payment : BaseModel
{
    public DateTime DateFinalized { get; set; }
    public PaymentProvider PaymentProvider { get; set; }  // Viva = 1
    public PaymentStatus PaymentStatus { get; set; }      // Pending/Success/Failed
    [Column(TypeName = "DECIMAL(10,2)")] public decimal Amount { get; set; }
    [MaxLength(50)] public string? TransactionId { get; set; }  // Viva tid
    [MaxLength(50)] public string? OrderCode { get; set; }      // Viva orderCode
    
    // Navigation
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
}
```

### ReservationRate
```csharp
public class ReservationRate : BaseModel
{
    [MaxLength(50)] public string? HotelCode { get; set; }
    [MaxLength(20)] public string RateId { get; set; }  // Format: "328000-226-2_5"
    public BookingStatus BookingStatus { get; set; }
    [Column(TypeName = "DECIMAL(10,2)")] public decimal Price { get; set; }
    [Column(TypeName = "DECIMAL(10,2)")] public decimal NetPrice { get; set; }
    [Column(TypeName = "TINYINT UNSIGNED")] public int Quantity { get; set; }
    public Provider? Provider { get; set; }  // WebHotelier = 1
    public string? Name { get; set; }
    public string? CancelationInfo { get; set; }
    public string? BoardInfo { get; set; }
    public int ProviderResId { get; set; }  // WebHotelier reservation ID
    public DateTime DateFinalized { get; set; }
    
    // Navigation
    public int? ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
    public PartyItemDB? SearchParty { get; set; }
}
```

### Coupon
```csharp
public class Coupon : BaseModel
{
    [MaxLength(50)] public string Code { get; set; }  // Uppercase
    public CouponType CouponType { get; set; }        // percentage or flat
    public int UsageLimit { get; set; }
    public int UsageLeft { get; set; }
    public int Percentage { get; set; }  // e.g., 10 for 10%
    public int Amount { get; set; }      // Flat amount in EUR
    public DateTime Expiration { get; set; }
}
```

---

## Enums

```csharp
public enum BookingStatus
{
    New = 0,
    Pending = 1,    // Reservation created, awaiting payment
    Running = 2,    // Booking in progress with provider
    Confirmed = 3,  // Successfully booked
    Cancelled = 4,
    Error = 5
}

public enum Provider
{
    WebHotelier = 1
}

public enum PaymentProvider
{
    Viva = 1
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3
}

public enum CouponType
{
    none = 0,
    flat = 1,       // Fixed EUR amount
    percentage = 2  // Percentage discount
}
```

---

## DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    public DbSet<Customer> Customers { get; set; }
    public DbSet<Reservation> Reservations { get; set; }
    public DbSet<ReservationRate> ReservationRates { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Coupon> Coupons { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // UTF8MB4 for Greek support
        modelBuilder.UseCollation("utf8mb4_general_ci");

        // Auto DateCreated for all BaseModel entities
        // DateCreated.HasDefaultValueSql("CURRENT_TIMESTAMP")

        // Relationships with Restrict delete
        // Customer -> Payments (1:N)
        // Customer -> Reservations (1:N)
        // Reservation -> Payments (1:N)
        // Reservation -> ReservationRates (1:N, Cascade delete)
    }
}
```

---

## API DTOs (Contracts)

### Key Request/Response Types

| Type | Purpose |
|------|---------|
| `CheckoutResponse` | Checkout page data with rooms, prices, partial payment info |
| `SingleAvailabilityResponse` | Single hotel availability with rooms and rates |
| `PluginSearchResponse` | Multi-hotel search results with filters |
| `PreparePaymentResponse` | Viva order code for payment redirect |
| `SuccessfullPaymentResponse` | Payment confirmation result |

### Party/Rate DTOs

```csharp
// Party item (room composition)
public class PartyItem
{
    public int adults { get; set; }
    public int[]? children { get; set; }  // Ages
    public int RoomsCount { get; set; }
    public string? party { get; set; }     // JSON string
}

// Selected rate for booking
public class SelectedRate
{
    public string rateId { get; set; }     // "328000-226-2_5_10"
    public string roomId { get; set; }
    public int count { get; set; }
    public string roomType { get; set; }
    public string searchParty { get; set; }
    public int adults { get; set; }
    public int children { get; set; }
}
```

---

## Data Format Conventions

### Hotel ID
- Format: `{providerId}-{providerHotelCode}`
- Example: `1-VAROSVILL`
- Provider 1 = WebHotelier

### Rate ID
- Format: `{baseRateId}-{adults}_{child1Age}_{child2Age}`
- Example: `328000-226-2_5_10` (2 adults, children ages 5 and 10)

### Party JSON
```json
[
  {"adults": 2, "children": [5, 10]},
  {"adults": 1}
]
```

### Date Formats
- API Input: `dd/MM/yyyy` (Greek format)
- Internal/DB: `DateOnly` or `DateTime`
- WebHotelier API: `yyyy-MM-dd`
