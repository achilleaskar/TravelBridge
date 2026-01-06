# TravelBridge Hotel Provider Integration

This document describes the integration with WebHotelier, the hotel inventory and booking provider.

## Overview

WebHotelier is a hotel distribution platform that provides:
- Property listings
- Real-time availability
- Room and rate information
- Booking creation and management
- Cancellation handling

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    TravelBridge.API                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │           WebHotelierPropertiesService                   │    │
│  │  - Orchestrates all WebHotelier operations               │    │
│  │  - Handles multi-party searches                          │    │
│  │  - Manages booking lifecycle                             │    │
│  │  - Sends confirmation emails                             │    │
│  └───────────────────────────┬─────────────────────────────┘    │
└──────────────────────────────┼──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│              TravelBridge.Providers.WebHotelier                  │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │                 WebHotelierClient                        │    │
│  │  - HTTP client for WebHotelier API                       │    │
│  │  - Handles authentication (Basic Auth)                   │    │
│  │  - JSON serialization/deserialization                    │    │
│  └───────────────────────────┬─────────────────────────────┘    │
└──────────────────────────────┼──────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    WebHotelier API                               │
│                 (External Service)                               │
└─────────────────────────────────────────────────────────────────┘
```

## Configuration

### appsettings.json

```json
{
  "WebHotelierApi": {
    "BaseUrl": "https://rest.reserve-online.net/",
    "Username": "<username>",
    "Password": "<password>",
    "GuaranteeCard": {
      "Number": "<card_number>",
      "Type": "MC",
      "Name": "<cardholder_name>",
      "Month": "MM",
      "Year": "YYYY",
      "CVV": "<cvv>"
    }
  }
}
```

### GuaranteeCard

The `GuaranteeCard` section contains payment guarantee credentials sent to WebHotelier when creating bookings. This card is used as a **guarantee only** and is **not actually charged** - actual payments are processed through Viva Wallet.

### Service Registration

```csharp
// In TravelBridge.Providers.WebHotelier/ServiceCollectionExtensions.cs
public static IServiceCollection AddWebHotelier(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.Configure<WebHotelierApiOptions>(
        configuration.GetSection("WebHotelierApi"));

    services.AddHttpClient("WebHotelierApi", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<WebHotelierApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.Add("Accept-Language", "el");

        // Basic authentication
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Basic", credentials);
    });

    services.AddScoped<WebHotelierClient>();
    return services;
}
```

## API Endpoints Used

### 1. Property Search

**GET** `/property?name={searchTerm}`

Searches properties by name.

```csharp
public async Task<WHHotel[]> SearchPropertiesAsync(string propertyName)
{
    var response = await _httpClient.GetAsync(
        $"property?name={Uri.EscapeDataString(propertyName)}");
    // Returns array of WHHotel objects
}
```

### 2. All Properties

**GET** `/property`

Returns all available properties.

```csharp
public async Task<WHHotel[]> GetAllPropertiesAsync()
{
    var response = await _httpClient.GetAsync("property");
    // Returns array of WHHotel objects
}
```

### 3. Multi-Property Availability

**GET** `/availability?party={party}&checkin={date}&checkout={date}&lat={lat}&lon={lon}&lat1={lat1}&lat2={lat2}&lon1={lon1}&lon2={lon2}&sort_by={sort}&sort_order={order}&payments=1`

Returns availability for multiple properties in a geographic area.

```csharp
public async Task<WHMultiAvailabilityResponse?> GetAvailabilityAsync(
    WHAvailabilityRequest request, 
    string party)
{
    var url = $"availability?party={party}" +
        $"&checkin={request.CheckIn}" +
        $"&checkout={request.CheckOut}" +
        $"&lat={request.Lat}" +
        $"&lon={request.Lon}" +
        $"&lat1={request.BottomLeftLatitude}" +
        $"&lat2={request.TopRightLatitude}" +
        $"&lon1={request.BottomLeftLongitude}" +
        $"&lon2={request.TopRightLongitude}" +
        $"&sort_by={request.SortBy}" +
        $"&sort_order={request.SortOrder}" +
        $"&payments=1";
    // Returns availability with payment schedules
}
```

### 4. Single Property Availability

**GET** `/availability/{propertyId}?party={party}&checkin={date}&checkout={date}`

Returns detailed availability for a single property.

```csharp
public async Task<WHSingleAvailabilityData?> GetSingleAvailabilityAsync(
    string propertyId, 
    string checkIn, 
    string checkOut, 
    string party)
```

### 5. Flexible Calendar

**GET** `/availability/{propertyId}/flexible-calendar?party={party}&startDate={date}&endDate={date}`

Returns availability calendar for alternative dates.

```csharp
public async Task<WHAlternativeDaysData?> GetFlexibleCalendarAsync(
    string propertyId, 
    string party, 
    DateTime startDate, 
    DateTime endDate)
```

### 6. Property Info

**GET** `/property/{propertyId}`

Returns detailed property information.

```csharp
public async Task<WHHotelInfoResponse?> GetHotelInfoAsync(string hotelId)
```

### 7. Room Info

**GET** `/room/{propertyId}/{roomCode}`

Returns detailed room information.

```csharp
public async Task<WHRoomInfoResponse?> GetRoomInfoAsync(
    string hotelId, 
    string roomCode)
```

### 8. Create Booking

**POST** `/book/{propertyId}`

Creates a reservation.

```csharp
public async Task<WHBookingResponse?> CreateBookingAsync(
    string hotelCode, 
    Dictionary<string, string> parameters)
```

**Parameters**:
- `checkin`: Check-in date (yyyy-MM-dd)
- `checkout`: Check-out date (yyyy-MM-dd)
- `rate`: Rate ID
- `price`: Net price
- `rooms`: Number of rooms
- `adults`: Number of adults
- `party`: Party JSON
- `firstName`: Guest first name
- `lastName`: Guest last name
- `email`: Guest email
- `remarks`: Special requests
- `payment_method`: "CC"
- `cardNumber`, `cardType`, `cardName`, `cardMonth`, `cardYear`, `cardCVV`

### 9. Cancel Booking

**GET** `/purge/{reservationId}`

Cancels a reservation.

```csharp
public async Task<bool> CancelBookingAsync(int reservationId)
```

## Data Models

### WHHotel (Property)

```csharp
public class WHHotel
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public int? Rating { get; set; }
    public string Description { get; set; }
    public WHLocation Location { get; set; }
    public List<WHPhotoInfo> PhotosItems { get; set; }
    public List<WHHotelRate> Rates { get; set; }
}
```

### WHHotelRate (Rate)

```csharp
public class WHHotelRate
{
    public string Id { get; set; }
    public string RoomType { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }           // Net price
    public decimal RetailPrice { get; set; }     // Display price
    public int Remaining { get; set; }
    public int BoardType { get; set; }
    public WHCancellationFee[] CancellationFees { get; set; }
    public WHPayment[] Payments { get; set; }
    public WHPartyItem SearchParty { get; set; }
}
```

### WHPartyItem

```csharp
public class WHPartyItem
{
    public int adults { get; set; }
    public int[]? children { get; set; }
    public int RoomsCount { get; set; }
    public string? party { get; set; }  // JSON representation
}
```

## Multi-Party Search Logic

When searching with multiple rooms, the system:

1. **Groups party configurations**:
```csharp
// Input: [{"adults":2},{"adults":2},{"adults":3}]
// Groups to: [{adults:2, count:2}, {adults:3, count:1}]
```

2. **Makes parallel API calls** for each unique party:
```csharp
Dictionary<WHPartyItem, Task<WHMultiAvailabilityResponse?>> tasks = new();
foreach (var partyItem in partyList)
{
    tasks.Add(partyItem, _whClient.GetAvailabilityAsync(request, partyItem.party));
}
await Task.WhenAll(tasks.Values);
```

3. **Merges responses** - only hotels with availability for ALL party configurations are returned:
```csharp
var GroupedHotels = respones
    .SelectMany(h => h.Value.Data!.Hotels)
    .GroupBy(h => h.Id)
    .Where(h => h.Count() == respones.Count);  // Must have results for all parties
```

4. **Calculates combined pricing**:
```csharp
contractsHotel.MinPrice = hotels.Sum(h => h.MinPrice * h.SearchParty?.RoomsCount ?? 1);
```

## Availability Filtering

The `AvailabilityProcessor` ensures hotels have enough rooms:

```csharp
public static IEnumerable<WebHotel> FilterHotelsByAvailability(
    PluginSearchResponse response, 
    List<PartyItem> partyList)
{
    foreach (var hotel in response.Results)
    {
        // Total rooms requested vs total available
        if (partyList.Sum(a => a.RoomsCount) > 
            hotel.Rates.DistinctBy(h => h.Type).Sum(s => s.Remaining))
        {
            invalid.Add(hotel);
            continue;
        }
        
        // Check each party configuration has enough rooms
        foreach (var party in partyList)
        {
            var availableForParty = hotel.Rates
                .Where(r => r.SearchParty?.Equals(party) == true)
                .GroupBy(r => r.Type)
                .Select(g => g.First())
                .Sum(s => s.Remaining);
                
            if (party.RoomsCount > availableForParty)
            {
                invalid.Add(hotel);
                break;
            }
        }
    }
    return response.Results.Except(invalid);
}
```

## Alternative Dates

When no availability is found, the system fetches alternative dates:

```csharp
private async Task<List<WHAlternative>> GetAlternatives(
    List<WHPartyItem>? partyList, 
    string propertyId, 
    string checkIn, 
    string checkOut)
{
    var from = DateTime.Parse(checkIn).AddDays(-14);
    var to = DateTime.Parse(checkOut).AddDays(14);
    
    // Get flexible calendar for each party
    // Keep only dates available for ALL party configurations
    // Calculate combined pricing
}
```

## Booking Creation

### Process

1. **For each rate in the reservation**:
   - Update rate status to `Running`
   - Call WebHotelier booking API
   - Update rate status to `Confirmed` with provider ID

2. **If all rates confirmed**:
   - Update reservation status to `Confirmed`
   - Send confirmation email

3. **On error**:
   - Attempt to cancel any confirmed rates
   - Update statuses accordingly

```csharp
internal async Task CreateBooking(Reservation reservation, ReservationsRepository repo)
{
    foreach (var rate in reservation.Rates)
    {
        var parameters = new Dictionary<string, string>
        {
            { "checkin", reservation.CheckIn.ToString("yyyy-MM-dd") },
            { "checkout", reservation.CheckOut.ToString("yyyy-MM-dd") },
            { "rate", rate.RateId.Split('-')[0] },
            { "price", rate.NetPrice.ToString(CultureInfo.InvariantCulture) },
            { "rooms", rate.Quantity.ToString() },
            { "adults", rate.SearchParty?.Adults.ToString() },
            { "party", BuildPartyString(rate) },
            { "firstName", reservation.Customer.FirstName },
            { "lastName", reservation.Customer.LastName },
            { "email", reservation.Customer.Email },
            { "remarks", reservation.Customer.Notes },
            { "payment_method", "CC" },
            // Card details from configuration
        };

        await repo.UpdateReservationRateStatus(rate.Id, BookingStatus.Running);
        var res = await _whClient.CreateBookingAsync(hotelCode, parameters);
        await repo.UpdateReservationRateStatusConfirmed(rate.Id, BookingStatus.Confirmed, res.data.res_id);
    }
}
```

## Rate ID Encoding

Rate IDs include party information for tracking:

```
{baseRateId}-{adults}{_childAge1_childAge2...}

Examples:
328000-226-2          → 2 adults
328000-226-2_5_10     → 2 adults, children ages 5 and 10
273063-3-1_8          → 1 adult, child age 8
```

This allows the checkout process to match rates to their original party configuration.

## Board Types

| ID | Name (Greek) | Name (English) |
|----|--------------|----------------|
| 0 | Μόνο Δωμάτιο | Room Only |
| 1 | Πρωινό | Breakfast |
| 2 | Ημιδιατροφή | Half Board |
| 3 | Πλήρης Διατροφή | Full Board |
| 14 | Μόνο Δωμάτιο | Room Only (alt) |

IDs 0 and 14 are treated as equivalent "Room Only".

## Error Handling

### HTTP Errors

```csharp
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "GetAvailabilityAsync failed");
    throw new InvalidOperationException($"Error calling WebHotelier API: {ex.Message}", ex);
}
```

### Booking Errors

If booking fails:
1. Attempt cancellation of any confirmed rates
2. Throw exception with details
3. Reservation remains in `Pending` status for manual review

```csharp
catch (Exception ex)
{
    if (await CancelBooking(reservation, repo))
    {
        throw new InvalidOperationException(
            $"Error calling WebHotelier API: {ex.Message} - Error cancelling booking. ", ex);
    }
    throw new InvalidOperationException(
        $"Error calling WebHotelier API: {ex.Message}", ex);
}
```

## Logging

Comprehensive logging at all stages:

```csharp
_logger.LogInformation("GetAvailabilityAsync started - CheckIn: {CheckIn}, CheckOut: {CheckOut}");
_logger.LogDebug("Processing {PartyCount} party configurations", partyList.Count);
_logger.LogInformation("GetAvailabilityAsync completed in {ElapsedMs}ms - ResultsCount: {Count}");
_logger.LogError(ex, "GetAvailabilityAsync failed after {ElapsedMs}ms");
```

## Future Provider Extensions

To add a new hotel provider:

1. Create `TravelBridge.Providers.{ProviderName}` project
2. Implement client class similar to `WebHotelierClient`
3. Create service extension method
4. Add to `Program.cs` DI registration
5. Update `WebHotelierPropertiesService` or create abstract orchestrator
