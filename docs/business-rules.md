# TravelBridge Business Rules

This document describes the business rules, pricing calculations, availability logic, and coupon system implemented in TravelBridge.

## Pricing Rules

### Base Price Calculation

TravelBridge applies markup to provider net prices:

```csharp
public static List<Alternative> GetFinalPrice(
    this List<Alternative> source, 
    decimal disc, 
    string code, 
    CouponType couponType)
{
    decimal PricePerc = 0.95m;  // 5% discount from retail
    
    // Special hotels get full retail price
    if (hotelCodes.Contains(code))
    {
        PricePerc = 1m;
    }

    foreach (var alt in source)
    {
        var minMargin = alt.NetPrice * 10 / 100;  // Minimum 10% margin
        
        if (alt.MinPrice - alt.NetPrice < minMargin)
        {
            // If margin too low, set price to ensure 10% margin
            alt.MinPrice = decimal.Floor(
                (alt.NetPrice + minMargin) * PricePerc * extraDiscPer - extraDisc);
        }
        else
        {
            // Use provider retail price with adjustments
            alt.MinPrice = decimal.Floor(
                alt.NetPrice * PricePerc * extraDiscPer - extraDisc);
        }
    }
}
```

### Pricing Rules Summary

| Rule | Description |
|------|-------------|
| **Default Markup** | 5% below retail price (PricePerc = 0.95) |
| **Minimum Margin** | 10% above net price |
| **Special Hotels** | Full retail price (configurable list) |
| **Price Floor** | Always floor() to round down |

### Sale Price Logic

```csharp
// Show sale price only if significantly different (>= €5)
if (salePrice >= hotel.MinPrice + 5)
    hotel.SalePrice = salePrice;
else
    hotel.SalePrice = hotel.MinPrice;
```

### Per-Night Price

```csharp
hotel.MinPricePerDay = Math.Floor(hotel.MinPrice / nights);
```

## Availability Rules

### Multi-Room Availability

When searching with multiple rooms, a hotel must have availability for ALL party configurations:

```csharp
// Party: [{adults:2}, {adults:2}, {adults:3}]
// Grouped: [{adults:2, count:2}, {adults:3, count:1}]

// Hotel must appear in results for BOTH party queries
var GroupedHotels = responses
    .SelectMany(h => h.Value.Data.Hotels)
    .GroupBy(h => h.Id)
    .Where(h => h.Count() == responses.Count);  // Must have all parties
```

### Room Availability Check

```csharp
public static IEnumerable<WebHotel> FilterHotelsByAvailability(
    PluginSearchResponse response, 
    List<PartyItem> partyList)
{
    foreach (var hotel in response.Results)
    {
        // Rule 1: Total rooms requested <= total available
        if (partyList.Sum(a => a.RoomsCount) > 
            hotel.Rates.DistinctBy(h => h.Type).Sum(s => s.Remaining))
        {
            invalid.Add(hotel);
            continue;
        }
        
        // Rule 2: Each party config has enough rooms
        foreach (var party in partyList)
        {
            var available = hotel.Rates
                .Where(r => r.SearchParty?.Equals(party) == true)
                .GroupBy(r => r.Type)
                .Select(g => g.First())
                .Sum(s => s.Remaining);
                
            if (party.RoomsCount > available)
            {
                invalid.Add(hotel);
                break;
            }
        }
    }
}
```

### Checkout Availability Validation

Before confirming a booking, availability is re-checked:

```csharp
if (!AvailabilityProcessor.HasSufficientAvailability(availRes, selectedRates))
{
    return new PreparePaymentResponse
    {
        ErrorCode = "Error",
        ErrorMessage = "Not enough rooms"
    };
}
```

## Party Configuration Rules

### Single Room

```json
// Required: adults
// Optional: children (as ages)
[{"adults": 2}]                        // 2 adults
[{"adults": 2, "children": [5, 10]}]   // 2 adults, 2 children
```

### Multiple Rooms

```json
// Each object is a room
[
  {"adults": 2, "children": [5]},   // Room 1
  {"adults": 2},                     // Room 2
  {"adults": 1, "children": [3, 8]} // Room 3
]
```

### Validation Rules

```csharp
// Rule 1: At least 1 adult per room
if (adults == null || adults < 1)
{
    throw new ArgumentException("There must be at least one adult in the room.");
}

// Rule 2: Party required for multi-room
if (string.IsNullOrWhiteSpace(party) && rooms != 1)
{
    throw new InvalidOperationException("when room greater than 1 party must be used");
}
```

## Coupon System

### Coupon Types

| Type | Description | Example |
|------|-------------|---------|
| `percentage` | Percentage off total | 10% off |
| `flat` | Fixed amount off | €50 off |
| `none` | No coupon | - |

### Coupon Validation

```csharp
public async Task<Coupon?> RetrieveCoupon(string couponCode)
{
    return await db.Coupons.FirstOrDefaultAsync(
        c => c.Code == couponCode.ToUpper()  // Case-insensitive
          && c.Expiration > DateTime.UtcNow); // Not expired
}
```

### Coupon Application

```csharp
// Retrieve and validate coupon
var coupon = await reservationsRepository.RetrieveCoupon(couponCode.ToUpper());

if (coupon != null && coupon.CouponType == CouponType.percentage)
{
    disc = coupon.Percentage / 100m;  // e.g., 10 → 0.10
}
else if (coupon != null && coupon.CouponType == CouponType.flat)
{
    disc = coupon.Amount;  // e.g., 50.00
}

// Apply to pricing
if (couponType == CouponType.flat)
    extraDisc = disc;
else if (couponType == CouponType.percentage)
    extraDiscPer = 1 - disc;

finalPrice = basePrice * extraDiscPer - extraDisc;
```

### Coupon Database Model

```sql
CREATE TABLE Coupons (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Code VARCHAR(50) UNIQUE NOT NULL,
    CouponType ENUM('none', 'flat', 'percentage') NOT NULL,
    Percentage DECIMAL(5,2),
    Amount DECIMAL(10,2),
    Expiration DATETIME NOT NULL,
    DateCreated DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

## Payment Schedule Rules

### Partial Payment Eligibility

Partial payments are only offered when:
1. First payment is due TODAY
2. There are future payment dates

```csharp
if (payments[0].DueDate?.Date != DateTime.Today 
    || !payments.Any(p => p.DueDate?.Date > DateTime.Today))
{
    return null;  // Full payment required
}
```

### Payment Merging Rules

Provider may send many payment dates. These are merged:

1. **Same-day payments** are always combined
2. **Nearby payments** (1-10 days apart) are iteratively merged
3. **Final result** has maximum 2 installments
4. **Far bookings** (>12 days out): payments ≤3 days apart are merged

```csharp
// Step 1: Group same-day
var grouped = payments.GroupBy(p => p.DueDate.Date)
    .Select(g => new { DueDate = g.Key, Amount = g.Sum(p => p.Amount) });

// Step 2: Merge nearby (increasing window from 1 to 10 days)
for (int maxDays = 1; maxDays <= 10 && grouped.Count > 2; maxDays++)
{
    // Merge payments within maxDays
}

// Step 3: If still >2, split in half
if (grouped.Count > 2)
{
    var firstHalf = grouped.Take(grouped.Count / 2);
    var secondHalf = grouped.Skip(grouped.Count / 2);
    // Combine each half
}
```

### Example Payment Schedule

**Provider sends:**
```
Jan 15: €50   ← Today
Jan 16: €50
Feb 01: €100
Feb 15: €100
Mar 01: €200
```

**After merging:**
```
Prepay (today): €100      (Jan 15 + 16)
Next Payment 1: €200      (Feb 1 + 15 merged)
Next Payment 2: €200      (Mar 1)
```

## Board Types

### Board Type IDs

| ID | Greek Name | English | Has Board |
|----|------------|---------|-----------|
| 0 | Μόνο Δωμάτιο | Room Only | No |
| 1 | Πρωινό | Breakfast | Yes |
| 2 | Ημιδιατροφή | Half Board | Yes |
| 3 | Πλήρης Διατροφή | Full Board | Yes |
| 14 | Μόνο Δωμάτιο | Room Only | No |

### Board Determination

```csharp
public static int[] NoboardIds = new int[] { 0, 14 };

// In response building:
HasBoard = !NoboardIds.Contains(rate.BoardType ?? 0);
```

### Board Text Display

```csharp
public void SetBoardsText()
{
    if (Boards.IsNullOrEmpty()) return;
    
    var boardsText = new StringBuilder();
    foreach (var board in Boards.Distinct())
    {
        if (boardsText.Length > 0) boardsText.Append(", ");
        boardsText.Append(board.Name);
    }
    BoardsText = boardsText.ToString();
}
```

## Hotel Type Mapping

### Hotel Categories

Hotels are mapped from provider types to display categories:

```csharp
public static List<string> MapToType(this string originalType)
{
    // Maps like "Hotel" → ["Ξενοδοχείο"]
    // Maps like "Apartments" → ["Διαμερίσματα"]
    // etc.
}
```

### Filter Building

```csharp
var groupedHotels = hotels
    .SelectMany(hotel => hotel.MappedTypes, (hotel, type) => new { type, hotel })
    .GroupBy(x => x.type)
    .ToDictionary(g => g.Key, g => g.Select(x => x.hotel).Distinct().ToList());

// Creates filter like:
// "Ξενοδοχείο" → 50 hotels
// "Διαμερίσματα" → 30 hotels
```

## Sorting Options

| Option | SortBy | SortOrder | Description |
|--------|--------|-----------|-------------|
| `popularity` | POPULARITY | DESC | Most booked first |
| `distance` | DISTANCE | ASC | Nearest to center |
| `price_asc` | PRICE | ASC | Cheapest first |
| `price_desc` | PRICE | DESC | Most expensive first |

**Default**: `popularity DESC`

## Filter Application

Filters are applied in order:

1. **Price filter** (min/max per night)
2. **Hotel type filter** (comma-separated)
3. **Board type filter** (comma-separated IDs)
4. **Rating filter** (comma-separated stars)

```csharp
private static void ApplyFilters(PluginSearchResponse res, SubmitSearchParameters pars)
{
    var filtered = res.Results.AsEnumerable();

    if (pars.minPrice.HasValue)
        filtered = filtered.Where(h => h.MinPricePerDay >= pars.minPrice.Value);

    if (pars.maxPrice.HasValue)
        filtered = filtered.Where(h => h.MinPricePerDay <= pars.maxPrice.Value);

    if (!string.IsNullOrWhiteSpace(pars.hotelTypes))
    {
        var types = pars.hotelTypes.ToLower().Split(',');
        filtered = filtered.Where(h => h.MappedTypes.Any(t => types.Contains(t.ToLower())));
    }

    // Similar for boardTypes and rating
    
    res.Results = filtered.ToList();
}
```

## Date Rules

### Date Format

All dates use `dd/MM/yyyy` format in API requests/responses.

### Validation

```csharp
if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", 
    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedCheckin))
{
    throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
}
```

### Timezone Handling

Greek timezone is used for payment due dates:

```csharp
string timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
    ? "GTB Standard Time" 
    : "Europe/Athens";

TimeZoneInfo greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
int offset = (int)greekTimeZone.GetUtcOffset(DateTime.UtcNow).TotalHours;

// Compare against Greek time
DateTime.UtcNow.AddHours(offset).Date
```

## Booking Status Transitions

```
┌──────────────────────────────────────────────────────────────────┐
│                                                                  │
│  NEW ──► PENDING ──► RUNNING ──► CONFIRMED                       │
│              │           │                                        │
│              │           └──────► ERROR                          │
│              │                                                    │
│              └──────────────────► CANCELLED                       │
│                                                                  │
└──────────────────────────────────────────────────────────────────┘

Transitions:
- NEW → PENDING: Reservation created, awaiting payment
- PENDING → RUNNING: Payment confirmed, creating booking with provider
- RUNNING → CONFIRMED: Provider booking successful
- RUNNING → ERROR: Provider booking failed
- PENDING → CANCELLED: User/admin cancellation
- CONFIRMED → CANCELLED: Cancellation after confirmation
```
