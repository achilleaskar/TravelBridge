# External Service Integrations

## Overview

TravelBridge integrates with the following external services:

| Service | Purpose | Auth Method |
|---------|---------|-------------|
| WebHotelier | Hotel data, availability, bookings | Basic Auth |
| Viva Wallet | Payment processing | OAuth2 Client Credentials |
| MapBox | Location autocomplete | API Key |
| HereMaps | Location services (backup) | API Key |
| SMTP | Email delivery | Username/Password |

---

## WebHotelier Integration

### Configuration
```json
{
  "WebHotelierApi": {
    "BaseUrl": "https://rest.reserve-online.net/",
    "Username": "your_username",
    "Password": "your_password",
    "GuaranteeCard": {
      "Number": "card_number",
      "Type": "MC",
      "Name": "Cardholder Name",
      "Month": "MM",
      "Year": "YYYY",
      "CVV": "cvv"
    }
  }
}
```

### Authentication
Basic Auth header added to all requests:
```
Authorization: Basic base64(username:password)
```

### GuaranteeCard
The `GuaranteeCard` section contains payment guarantee credentials sent to WebHotelier when creating bookings. This card is used as a guarantee but is not actually charged - payments are processed through Viva Wallet.

### Endpoints Used

#### 1. Property Search
```
GET /property?name={searchTerm}
```
Returns hotels matching search term.

#### 2. All Properties
```
GET /property
```
Returns all available properties.

#### 3. Multi-Hotel Availability
```
GET /availability?party={party}&checkin={date}&checkout={date}&lat={lat}&lon={lon}&lat1={lat1}&lat2={lat2}&lon1={lon1}&lon2={lon2}&sort_by={sort}&sort_order={order}&payments=1
```
Returns available hotels in bounding box.

#### 4. Single Hotel Availability
```
GET /availability/{propertyId}?party={party}&checkin={date}&checkout={date}
```
Returns room availability for specific hotel.

#### 5. Flexible Calendar
```
GET /availability/{propertyId}/flexible-calendar?party={party}&startDate={date}&endDate={date}
```
Returns alternative dates when no availability.

#### 6. Hotel Info
```
GET /property/{propertyId}
```
Returns hotel details, photos, amenities.

#### 7. Room Info
```
GET /room/{propertyId}/{roomCode}
```
Returns room type details.

#### 8. Create Booking
```
POST /book/{propertyId}
Content-Type: application/x-www-form-urlencoded

checkin=2025-06-15
checkout=2025-06-20
rate={rateId}
price={netPrice}
rooms={quantity}
adults={adults}
party={partyJson}
firstName={firstName}
lastName={lastName}
email={email}
remarks={notes}
payment_method=CC
cardNumber={number}
cardType=MC
cardName={name}
cardMonth={mm}
cardYear={yyyy}
cardCVV={cvv}
```

#### 9. Cancel Booking
```
GET /purge/{reservationId}
```

### Service Implementation
**File:** `Services/WebHotelier/WebHotelierPropertiesService.cs`

**Key Methods:**
- `SearchPropertyAsync(string propertyName)` - Search hotels
- `GetAvailabilityAsync(MultiAvailabilityRequest request)` - Multi-hotel search
- `GetHotelAvailabilityAsync(SingleAvailabilityRequest req, ...)` - Single hotel
- `GetHotelInfo(string hotelId)` - Hotel details
- `CreateBooking(Reservation reservation, ReservationsRepository repo)` - Book room
- `CancelBooking(Reservation reservation, ReservationsRepository repo)` - Cancel
- `SendConfirmationEmail(Reservation reservation)` - Email notification

### Multi-Room Handling
For multi-room searches, separate API calls are made for each party composition:
```csharp
// Party: [{"adults":2,"children":[5]},{"adults":1}]
// Results in 2 API calls, then merged
Dictionary<PartyItem, Task<HttpResponseMessage>> tasks;
foreach (var partyItem in partyList)
{
    tasks.Add(partyItem, _httpClient.GetAsync($"availability?party={partyItem.party}..."));
}
await Task.WhenAll(tasks.Values);
// Merge results...
```

---

## Viva Wallet Integration

### Configuration
```json
{
  "VivaApi": {
    "ApiKey": "your_client_id.apps.vivapayments.com",
    "ApiSecret": "your_client_secret",
    "SourceCode": "default_source_code",
    "SourceCodeTravelProject": "travelproject_source_code",
    "BaseUrl": "https://api.vivapayments.com/",
    "AuthUrl": "https://accounts.vivapayments.com/connect/token"
  }
}
```

### Authentication
OAuth2 Client Credentials flow with token caching:
```csharp
// VivaAuthService.GetAccessTokenAsync()
// Caches token until expiry - 60 seconds
```

### Endpoints Used

#### 1. Create Payment Order
```
POST /checkout/v2/orders
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "amount": 18000,  // cents (180.00 EUR)
  "sourceCode": "9156",
  "customerTrns": "reservation for 15/06/25-20/06/25 in Hotel Name",
  "merchantTrns": "reservation for 15/06/25-20/06/25 in Hotel Name",
  "customer": {
    "email": "customer@example.com",
    "fullName": "John Doe",
    "phone": "+306977771645",
    "countryCode": "GR"
  }
}
```
Returns `orderCode` for payment redirect.

#### 2. Validate Transaction
```
GET /checkout/v2/transactions/{tid}
Authorization: Bearer {accessToken}
```
Returns transaction details for validation:
- `orderCode` - Must match
- `amount` - Must match expected
- `statusId` - Must be "F" (completed)

### Service Implementation
**Files:**
- `Services/Viva/VivaAuthService.cs` - OAuth2 token management
- `Services/Viva/VivaService.cs` - Payment operations

**Key Methods:**
- `GetPaymentCode(VivaPaymentRequest request)` - Create order
- `ValidatePayment(string orderCode, string tid, Reservation)` - Verify payment

### Multi-Tenant Source Code
Different source codes based on request origin:
```csharp
bool isTravelProject = origin.Contains("travelproject.gr");
request.SourceCode = isTravelProject 
    ? options.Value.SourceCodeTravelProject 
    : options.Value.SourceCode;
```

---

## MapBox Integration

### Configuration
```json
{
  "MapBoxApi": {
    "ApiKey": "pk.eyJ1...",
    "BaseUrl": "https://api.mapbox.com/"
  }
}
```

### Endpoints Used

#### Geocoding/Autocomplete
```
GET /search/geocode/v6/forward?q={query}&country=gr,cy&limit=10&types=neighborhood,region,country,place,district,locality&language=el&autocomplete=true&access_token={apiKey}&permanent=false
```

Returns locations with bounding boxes for hotel search.

### Service Implementation
**File:** `Services/ExternalServices/MapBoxService.cs`

**Key Method:**
- `GetLocations(string? param, string? lang)` - Location autocomplete

### Response Mapping
MapBox features converted to `AutoCompleteLocation`:
- `id` format: `[{bbox}]-{lat}-{lon}`
- Filters out country-level results
- Returns Greek names (`language=el`)

---

## SMTP Email Integration

### Configuration
```json
{
  "Smtp": {
    "Host": "mail.example.com",
    "Port": "587",
    "Username": "bookings@example.com",
    "Password": "password",
    "From": "bookings@example.com"
  }
}
```

### Service Implementation
**File:** `Services/ConsoleEmailSender.cs` (class: `SmtpEmailSender`)

**Key Methods:**
- `SendEmailAsync(string email, string subject, string htmlMessage)`
- `SendMailAsync(MailMessage mailMessage)` - Used internally

### Email Template
**Resource:** `Resources/BookingConfirmationEmailTemplate.html`

Placeholders replaced:
- `[Client Full Name]`, `[Hotel Name]`, `[ReservationCode]`
- `[CheckInString]`, `[CheckoutString]`, `[NightsString]`
- `[TotalAmount]`, `[PaidAmount]`, `[RemainingAmount]`
- `[RoomDetails]` - Dynamic room information block

---

## Error Handling

### WebHotelier
- HTTP errors throw `InvalidOperationException`
- Empty results return empty collections
- Booking failures trigger cancellation rollback

### Viva
- Token refresh on expiry
- HTTP errors throw `Exception` with status details
- Payment validation returns `false` on mismatch

### MapBox
- HTTP errors throw `InvalidOperationException`
- Invalid responses return empty collection

---

## Rate Limiting & Retry

### Current Implementation
- WebHotelier: No explicit rate limiting
- Viva: Token cached to minimize auth requests
- MapBox: Limit 10 results per request

### Database Retry
```csharp
options.UseMySql(..., mySqlOptions =>
{
    mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
});
```

---

## Security Considerations

### Secrets in Configuration
⚠️ **Current State:** API keys and credentials in `appsettings.json`

**Recommendation:** Use:
- Azure Key Vault
- Environment variables
- User Secrets (development)

### Credit Card Data
WebHotelier booking includes hardcoded test card data:
```csharp
{ "cardNumber", "5375346200033267" },
{ "cardType", "MC" },
{ "cardName", "Vasileios Kioroglou" },
{ "cardMonth", "05" },
{ "cardYear", "2026" },
{ "cardCVV", "590" }
```
⚠️ **This should be replaced with actual payment integration or tokenization.**
