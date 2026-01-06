# TravelBridge Payment Flow

This document describes the payment integration with Viva Wallet and the complete payment lifecycle.

## Overview

TravelBridge uses **Viva Wallet** as its payment gateway. The integration supports:

- Full payments
- Partial/installment payments
- Payment validation
- Payment failure handling

## Payment Flow Diagram

```
┌──────────────────┐     ┌─────────────────┐     ┌─────────────────┐
│   WordPress FE   │     │  TravelBridge   │     │   Viva Wallet   │
│                  │     │      API        │     │                 │
└────────┬─────────┘     └────────┬────────┘     └────────┬────────┘
         │                        │                       │
         │ 1. POST /preparePayment│                       │
         │ (with booking details) │                       │
         │───────────────────────>│                       │
         │                        │                       │
         │                        │ 2. POST /checkout/v2/orders
         │                        │ (create payment order)│
         │                        │──────────────────────>│
         │                        │                       │
         │                        │<──────────────────────│
         │                        │ orderCode: 123456     │
         │                        │                       │
         │                        │ 3. Save to DB         │
         │                        │ (reservation pending) │
         │                        │                       │
         │<───────────────────────│                       │
         │ orderCode: 123456      │                       │
         │                        │                       │
         │ 4. Redirect to Viva    │                       │
         │────────────────────────────────────────────────>
         │ https://www.vivapayments.com/web/checkout?ref=123456
         │                        │                       │
         │                        │                       │
         │                        │    [User pays]        │
         │                        │                       │
         │<────────────────────────────────────────────────
         │ 5. Redirect back with tid                      │
         │                        │                       │
         │ 6. POST /paymentSucceed│                       │
         │ (tid + orderCode)      │                       │
         │───────────────────────>│                       │
         │                        │                       │
         │                        │ 7. GET /checkout/v2/transactions/{tid}
         │                        │──────────────────────>│
         │                        │                       │
         │                        │<──────────────────────│
         │                        │ transaction details   │
         │                        │                       │
         │                        │ 8. Validate & Update DB
         │                        │                       │
         │                        │ 9. Create booking     │
         │                        │    at WebHotelier     │
         │                        │                       │
         │                        │ 10. Send email        │
         │                        │                       │
         │<───────────────────────│                       │
         │ Booking confirmation   │                       │
         │                        │                       │
```

## Viva Wallet Integration

### Configuration

Configuration is in `appsettings.json`:

```json
{
  "VivaApi": {
    "BaseUrl": "https://api.vivapayments.com",
    "AuthUrl": "https://accounts.vivapayments.com",
    "ClientId": "<client_id>",
    "ClientSecret": "<client_secret>",
    "SourceCode": "<source_code>",
    "SourceCodeTravelProject": "<alternate_source_code>"
  }
}
```

### Services

#### VivaAuthService

Handles OAuth2 authentication with Viva Wallet.

```csharp
public class VivaAuthService
{
    public async Task<string> GetAccessTokenAsync();
}
```

- Obtains OAuth2 access tokens
- Tokens are used for all API calls

#### VivaService

Main service for payment operations.

```csharp
public class VivaService
{
    // Create a new payment order
    public async Task<string> GetPaymentCode(VivaPaymentRequest request);
    
    // Validate a completed payment
    public async Task<bool> ValidatePayment(
        string orderCode, 
        string tid, 
        decimal totalAmount, 
        decimal? prepayAmount);
}
```

### Payment Request Model

```csharp
public class VivaPaymentRequest
{
    public int Amount { get; set; }              // Amount in cents
    public string CustomerTrns { get; set; }     // Customer transaction description
    public string MerchantTrns { get; set; }     // Merchant transaction description
    public string SourceCode { get; set; }       // Viva source code
    public VivaCustomer Customer { get; set; }   // Customer details
}

public class VivaCustomer
{
    public string CountryCode { get; set; }      // "GR"
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Phone { get; set; }
}
```

## Payment Lifecycle

### 1. Prepare Payment

**Endpoint**: `POST /api/reservation/preparePayment`

1. Validate booking parameters
2. Verify availability hasn't changed
3. Verify prices match
4. Create Viva payment order
5. Store reservation with `BookingStatus.Pending`
6. Store payment with `PaymentStatus.Pending`
7. Return `orderCode` to frontend

```csharp
// Create Viva payment
var payment = new VivaPaymentRequest
{
    Amount = (int)(prepayAmount * 100),  // Convert to cents
    CustomerTrns = $"reservation for {checkin}-{checkout} in {hotelName}",
    Customer = new VivaCustomer
    {
        CountryCode = "GR",
        Email = customerEmail,
        FullName = $"{firstName} {lastName}",
        Phone = phone
    }
};

var orderCode = await viva.GetPaymentCode(payment);
```

### 2. User Payment

The frontend redirects the user to Viva Wallet's checkout page:

```
https://www.vivapayments.com/web/checkout?ref={orderCode}
```

After payment (success or failure), Viva redirects back to configured URLs with:
- `t` (tid) - Transaction ID
- `s` (orderCode) - Original order code

### 3. Payment Success

**Endpoint**: `POST /api/reservation/paymentSucceed`

1. Receive `tid` and `orderCode`
2. Validate payment with Viva API
3. Update payment status to `Success`
4. Create booking at WebHotelier
5. Send confirmation email

```csharp
public async Task<bool> ValidatePayment(
    string orderCode, 
    string tid, 
    decimal totalAmount, 
    decimal? prepayAmount)
{
    // Get transaction details from Viva
    var response = await _httpClient.GetAsync($"/checkout/v2/transactions/{tid}");
    
    // Validate:
    // - Order code matches
    // - Amount matches (full or prepay)
    // - Status is "F" (completed)
    return OrderCode == orderCode 
        && (Amount == totalAmount || Amount == prepayAmount) 
        && Status == "F";
}
```

### 4. Payment Failure

**Endpoint**: `POST /api/reservation/paymentFailed`

1. Receive `orderCode`
2. Update payment status to `Failed`
3. Return checkout data for retry

## Partial Payments

TravelBridge supports partial/installment payments based on provider payment schedules.

### Payment Schedule Calculation

The system merges payment schedules from the provider and calculates:

1. **Prepay Amount**: Due today
2. **Next Payments**: Future installments (max 2)

```csharp
public static PartialPayment? FillPartialPayment(
    List<PaymentWH>? payments, 
    DateTime checkIn)
{
    // Only create partial payment if:
    // 1. First payment is due today
    // 2. There are future payments
    
    if (payments[0].DueDate?.Date != DateTime.Today 
        || !payments.Any(p => p.DueDate?.Date > DateTime.Today))
    {
        return null;
    }
    
    return new PartialPayment
    {
        prepayAmount = payments
            .Where(p => p.DueDate?.Date <= DateTime.Today)
            .Sum(a => a.Amount),
        nextPayments = MergeNextPayments(futurePayments, checkIn)
    };
}
```

### Payment Merging Rules

1. Same-day payments are combined
2. Payments within 1-10 days are merged iteratively
3. Final result has max 2 installments
4. For bookings >12 days out, payments ≤3 days apart are merged

### Example

Provider schedule:
```
2025-01-15: €50   (today)
2025-01-16: €50
2025-02-01: €100
2025-02-15: €100
2025-03-01: €200
```

After merging:
```
Prepay (today): €100 (Jan 15 + Jan 16 merged)
Next Payment 1: €200 (Feb 1 + Feb 15 merged)
Next Payment 2: €200 (Mar 1)
```

## Source Code Selection

The system supports multiple Viva source codes for different booking sources:

```csharp
// Check if request is from travelproject.gr
bool isTravelProject = origin.Contains("travelproject.gr") 
    || referer.Contains("travelproject.gr");

request.SourceCode = isTravelProject
    ? options.Value.SourceCodeTravelProject 
    : options.Value.SourceCode;
```

## Database Records

### Payment Table

```sql
INSERT INTO Payments (
    Amount,
    OrderCode,
    PaymentProvider,
    PaymentStatus,
    ReservationId,
    DateCreated
) VALUES (
    135.00,
    '4918784106772600',
    1,  -- Viva
    1,  -- Pending
    @ReservationId,
    CURRENT_TIMESTAMP
);
```

### On Success

```sql
UPDATE Payments 
SET 
    PaymentStatus = 2,  -- Success
    TransactionId = 'dc90abcc-0350-4383-a624-5821811aedb9',
    DateFinalized = CURRENT_TIMESTAMP
WHERE OrderCode = '4918784106772600';
```

### On Failure

```sql
UPDATE Payments 
SET 
    PaymentStatus = 3,  -- Failed
    DateFinalized = CURRENT_TIMESTAMP
WHERE OrderCode = '4918784106772600';
```

## Error Handling

### Payment Creation Errors

If Viva API returns an error during order creation:
- Log the error
- Return error to frontend
- No database records created

### Validation Failures

If payment validation fails:
- Payment status remains `Pending`
- Return error to user with reservation ID
- Manual intervention may be required

### Booking Creation Errors

If WebHotelier booking fails after successful payment:
- Attempt to cancel any partial bookings
- Return error with reservation ID
- Manual refund may be required

## Security Considerations

1. **No card data stored**: Card details go directly to Viva/WebHotelier
2. **TID validation**: Always validate with Viva API, don't trust client data
3. **Amount verification**: Verify payment amount matches expected amount
4. **HTTPS only**: All API communication is over HTTPS

## Testing

For testing, configure test credentials in `appsettings.Development.json`:

```json
{
  "VivaApi": {
    "BaseUrl": "https://demo-api.vivapayments.com",
    "AuthUrl": "https://demo-accounts.vivapayments.com"
  },
  "TestCard": {
    "CardNumber": "4111111111111111",
    "CardType": "Visa",
    "CardName": "Test User",
    "CardMonth": "12",
    "CardYear": "2030",
    "CardCVV": "123"
  }
}
```
