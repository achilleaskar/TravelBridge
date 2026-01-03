Yep — continuing, not restarting. I unpacked **TravelBridge-PROD.zip** and **TravelBridge-upgraded_2.zip** and compared the *actual code* for the endpoints you can’t easily test (reservation/payment + hotel).

Here are the **remaining “logic/contract drift” issues** I can clearly see (these *will* change runtime behavior / JSON compared to PROD), plus exactly what to change.

---

## 1) 🚨 `/api/hotel/hotelInfo` response shape changed (breaking)

**PROD (`TravelBridge-PROD.zip`)**
`GET /api/hotel/hotelInfo` returns a wrapper:

```json
{ "error_code": "...", "error_msg": "...", "data": { ... } }
```

**Upgraded_2**
In `TravelBridge.API/Endpoints/HotelEndpoint.cs`, `GetHotelInfo` now returns **HotelData directly** (no wrapper), and uses `res.Data!` which can turn an API error into a **500**.

✅ **Fix (match PROD 1:1):**

* Re-introduce `HotelInfoResponse` (in Contracts or API like prod had it)
* Return the wrapper again, and map `Data = res.Data?.ToContracts()` (no `!`)

**File to change:** `TravelBridge.API/Endpoints/HotelEndpoint.cs`

**Target pattern:**

```csharp
private async Task<HotelInfoResponse> GetHotelInfo(string hotelId)
{
    if (string.IsNullOrWhiteSpace(hotelId))
        throw new ArgumentException("Invalid hotelId");

    var res = await webHotelierPropertiesService.GetHotelInfoAsync(hotelId);

    return new HotelInfoResponse
    {
        ErrorCode = res.ErrorCode,
        ErrorMsg  = res.ErrorMessage,     // keep JSON name error_msg in DTO
        Data      = res.Data?.ToContracts()
    };
}
```

---

## 2) 🚨 `paymentSucceed` response JSON changed: `successfullPayment` → `successfulPayment`

**PROD** response contains:

```json
"successfullPayment": true
```

**Upgraded_2** uses:

* `TravelBridge.Contracts/Contracts/Responses/SuccessfulPaymentResponse.cs`
* property name is `SuccessfulPayment` → JSON becomes `"successfulPayment"` (different spelling)

That’s a silent breaking change for the WordPress plugin if it expects the old field name.

✅ **Fix options (pick one):**

### Option A (best: no behavior change)

Keep the nice C# name, force the **old JSON name**:

```csharp
[JsonPropertyName("successfullPayment")]
public bool SuccessfulPayment { get; set; }
```

### Option B

Rename the property back to `SuccessfullPayment` like PROD had.

**File to change:** `TravelBridge.Contracts/Contracts/Responses/SuccessfulPaymentResponse.cs`

---

## 3) ⚠️ Partial payment null-safety regression in `ConfirmPayment`

In upgraded_2, `ConfirmPayment` calls:

```csharp
await viva.ValidatePayment(pay.OrderCode, pay.Tid, reservation.TotalAmount, reservation.PartialPayment.prepayAmount);
```

If `PartialPayment` is null in DB for “full payment” reservations (EF won’t enforce non-null just because the property is non-nullable), this becomes a **NullReferenceException**.

✅ **Fix (match old behavior):**

```csharp
await viva.ValidatePayment(
    pay.OrderCode,
    pay.Tid,
    reservation.TotalAmount,
    reservation.PartialPayment?.prepayAmount
);
```

**File to change:** `TravelBridge.API/Endpoints/ReservationEndpoints.cs`

---

## 4) ⚠️ Booking creation now depends on `TestCard` config (can break MYC / partial-pay bookings)

You moved hardcoded test card fields into `TestCardOptions`:

* `TravelBridge.API/Models/Apis/TestCardOptions.cs`
* bound in `Program.cs` from `"TestCard"` section
* used in `WebHotelierPropertiesService.CreateBooking(...)`

If your PROD config doesn’t include `TestCard:*`, then **MYC/partial-payment bookings** will send empty card fields and WebHotelier may reject the booking.

✅ Fix: ensure PROD has these settings (or set defaults equal to the old hardcoded values).

**Add to appsettings / environment:**

```json
"TestCard": {
  "CardNumber": "4111111111111111",
  "CardType": "visa",
  "CardName": "Jhon Doe",
  "CardMonth": "12",
  "CardYear": "2027",
  "CardCVV": "737"
}
```

---

## What I’d do next (so you don’t get surprised in prod)

1. Fix **#1 and #2** first (these are definite contract breaks).
2. Fix **#3** (cheap, removes a potential 500).
3. Confirm PROD config includes **#4** if you use the MYC/partial payment path.

If you upload your **latest “fixed most stuff” zip** (the one after upgraded_2), I can re-run the same comparison and tell you whether these are already fixed and what’s still drifting.
