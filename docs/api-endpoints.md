# TravelBridge API Endpoints

This document provides a complete reference for all TravelBridge API endpoints.

## Base URL

All endpoints are relative to: `https://<host>/api/`

## Common Response Patterns

### Success Response
```json
{
  "errorCode": null,
  "errorMsg": null,
  "data": { ... }
}
```

### Error Response
```json
{
  "errorCode": "ERROR_CODE",
  "errorMsg": "Human-readable error message"
}
```

---

## Plugin Endpoints (`/api/plugin`)

### GET `/api/plugin/autocomplete`

Returns matching hotels and locations for the search term.

**Summary**: Location and hotel autocomplete for search box

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `searchQuery` | string | No | Search term (minimum 3 characters) | `Trikala` |

#### Response

```json
{
  "hotels": [
    {
      "id": "1-VAROSRESID",
      "name": "Varos Village Hotel",
      "originalType": "Hotel",
      "mappedTypes": ["Ξενοδοχείο"],
      "rating": 4,
      "location": {
        "address": "Varos, Limnos",
        "latitude": 39.123,
        "longitude": 25.456
      }
    }
  ],
  "locations": [
    {
      "name": "Trikala",
      "region": "Thessaly",
      "id": "[21.123,39.456,22.123,40.456]-39.789-21.789",
      "countryCode": "GR",
      "type": "location"
    }
  ]
}
```

---

### GET `/api/plugin/allproperties`

Returns all available properties, optionally filtered by type.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `type` | string | No | Filter by hotel type (e.g., "Ξενοδοχείο") |

#### Response

```json
{
  "hotels": [...],
  "filters": [
    {
      "name": "Τύποι Καταλυμμάτων",
      "id": "hotelTypes",
      "values": [
        { "id": "Ξενοδοχείο", "name": "Ξενοδοχείο", "count": 50 }
      ]
    }
  ]
}
```

---

### GET `/api/plugin/submitSearch`

Main search endpoint returning available hotels with pricing and filters.

**Summary**: Search for hotels in a location with availability

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `checkin` | string | Yes | Check-in date (dd/MM/yyyy) | `15/06/2025` |
| `checkOut` | string | Yes | Check-out date (dd/MM/yyyy) | `20/06/2025` |
| `bbox` | string | Yes | Bounding box with center point | `[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348` |
| `searchTerm` | string | Yes | Search term (passthrough for display) | `Crete` |
| `adults` | int | No* | Number of adults (if single room) | `2` |
| `children` | string | No* | Children ages, comma-separated | `5,10` |
| `rooms` | int | No* | Number of rooms (must be 1 if not using party) | `1` |
| `party` | string | No* | Multi-room party JSON | `[{"adults":2,"children":[2,6]},{"adults":3}]` |
| `page` | int | No | Page number (0-indexed, 20 results per page) | `0` |
| `sorting` | string | No | Sort option | `popularity`, `distance`, `price_asc`, `price_desc` |
| `minPrice` | int | No | Minimum price per night filter | `50` |
| `maxPrice` | int | No | Maximum price per night filter | `500` |
| `hotelTypes` | string | No | Hotel types filter (comma-separated) | `Ξενοδοχείο,Διαμερίσματα` |
| `boardTypes` | string | No | Board types filter (comma-separated IDs) | `1,2,14` |
| `rating` | string | No | Star rating filter (comma-separated) | `3,4,5` |

*Either provide `adults` + `children` + `rooms=1`, or provide `party` for multi-room.

#### Response

```json
{
  "searchTerm": "Crete",
  "resultsCount": 150,
  "results": [
    {
      "id": "1-CRETAPAL",
      "name": "Creta Palace",
      "originalType": "Resort",
      "mappedTypes": ["Resort", "Ξενοδοχείο"],
      "rating": 5,
      "minPrice": 450,
      "minPricePerDay": 90,
      "salePrice": 500,
      "photoL": "https://...",
      "boards": [
        { "id": 3, "name": "Ημιδιατροφή" }
      ],
      "location": {
        "address": "Rethymno, Crete",
        "latitude": 35.36,
        "longitude": 24.47
      }
    }
  ],
  "filters": [
    {
      "name": "Ευρος Τιμής",
      "id": "price",
      "type": "range",
      "min": 25,
      "max": 1500,
      "preApplied": true
    },
    {
      "name": "Αστέρια",
      "id": "rating",
      "type": "values",
      "values": [
        { "id": "5", "name": "5 αστέρια", "count": 20, "filteredCount": 15 }
      ]
    },
    {
      "name": "Τύποι Καταλυμμάτων",
      "id": "hotelTypes",
      "type": "values",
      "values": [...]
    },
    {
      "name": "Τύποι Διατροφής",
      "id": "boardTypes",
      "type": "values",
      "values": [
        { "id": "14", "name": "Μόνο Δωμάτιο", "count": 100 },
        { "id": "1", "name": "Πρωινό", "count": 80 }
      ]
    }
  ]
}
```

---

## Hotel Endpoints (`/api/hotel`)

### GET `/api/hotel/hotelInfo`

Returns detailed information about a specific hotel.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `HotelId` | string | Yes | Hotel identifier | `1-VAROSRESID` |

#### Response

```json
{
  "errorCode": null,
  "errorMsg": null,
  "data": {
    "id": "1-VAROSRESID",
    "name": "Varos Village Hotel",
    "description": "...",
    "rating": 4,
    "type": "Hotel",
    "provider": "WebHotelier",
    "operation": {
      "checkinTime": "15:00",
      "checkoutTime": "11:00"
    },
    "location": {
      "address": "Varos, Limnos 81400",
      "latitude": 39.123,
      "longitude": 25.456
    },
    "largePhotos": ["https://..."],
    "amenities": [...]
  }
}
```

---

### GET `/api/hotel/roomInfo`

Returns detailed information about a specific room type.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `HotelId` | string | Yes | Hotel identifier | `1-VAROSRESID` |
| `RoomId` | string | Yes | Room type code | `LVLSTD` |

#### Response

```json
{
  "httpCode": 200,
  "errorCode": null,
  "errorMessage": null,
  "data": {
    "code": "LVLSTD",
    "name": "Standard Room",
    "description": "...",
    "capacity": {
      "maxAdults": 2,
      "maxChildren": 1,
      "maxOccupancy": 3
    },
    "amenities": [...],
    "largePhotos": ["https://..."]
  }
}
```

---

### GET `/api/hotel/hotelRoomAvailability`

Returns room availability and pricing for a specific hotel and dates.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `checkin` | string | Yes | Check-in date (dd/MM/yyyy) | `15/06/2026` |
| `checkOut` | string | Yes | Check-out date (dd/MM/yyyy) | `20/06/2026` |
| `hotelId` | string | Yes | Hotel identifier | `1-VAROSVILL` |
| `adults` | int | No* | Number of adults | `2` |
| `children` | string | No* | Children ages | `5,10` |
| `rooms` | int | No* | Number of rooms | `1` |
| `party` | string | No* | Multi-room party JSON | `[{"adults":2},{"adults":3}]` |

#### Response

```json
{
  "errorCode": null,
  "errorMessage": null,
  "data": {
    "provider": "WebHotelier",
    "rooms": [
      {
        "type": "DBL",
        "roomName": "Double Room",
        "rates": [
          {
            "id": "328000-226-2",
            "totalPrice": 450.00,
            "netPrice": 380.00,
            "remainingRooms": 5,
            "boardType": 1,
            "searchParty": { "adults": 2, "children": [], "party": "[{\"adults\":2}]" },
            "rateProperties": {
              "board": "Πρωινό",
              "hasBoard": true,
              "cancellationName": "Δωρεάν ακύρωση",
              "hasCancellation": true,
              "cancellationExpiry": "2026-06-10",
              "payments": [
                { "amount": 135, "dueDate": "2025-12-15" },
                { "amount": 315, "dueDate": "2026-06-10" }
              ]
            }
          }
        ]
      }
    ],
    "alternatives": []
  }
}
```

---

### GET `/api/hotel/HotelFullInfo`

Returns combined hotel information and availability in a single call.

#### Query Parameters

Same as `/hotelRoomAvailability`

#### Response

Combines `hotelInfo` response with availability data.

---

## Reservation Endpoints (`/api/reservation`)

### GET `/api/reservation/checkout`

Returns checkout page data with selected rooms and pricing breakdown.

#### Query Parameters

| Parameter | Type | Required | Description | Example |
|-----------|------|----------|-------------|---------|
| `checkin` | string | Yes | Check-in date | `17/03/2026` |
| `checkOut` | string | Yes | Check-out date | `20/03/2026` |
| `hotelId` | string | Yes | Hotel identifier | `1-VAROSVILL` |
| `selectedRates` | string | Yes | Selected rates JSON | `[{"rateId":"328000-226-2","count":1}]` |
| `couponCode` | string | No | Coupon code to apply | `SUMMER20` |

#### Response

```json
{
  "hotelData": {
    "id": "1-VAROSVILL",
    "name": "Varos Village Hotel",
    "image": "https://...",
    "rating": 4,
    "operation": {
      "checkinTime": "15:00",
      "checkoutTime": "11:00"
    }
  },
  "checkIn": "17/03/2026",
  "checkOut": "20/03/2026",
  "nights": 3,
  "selectedPeople": "2 ενήλικες, 1 δωμάτιο",
  "totalPrice": 450.00,
  "couponUsed": "SUMMER20",
  "couponValid": true,
  "couponDiscount": "10%",
  "rooms": [
    {
      "type": "DBL",
      "roomName": "Double Room",
      "rateId": "328000-226-2",
      "selectedQuantity": 1,
      "totalPrice": 450.00,
      "netPrice": 380.00,
      "rateProperties": {
        "board": "Πρωινό",
        "boardId": 1,
        "hasBoard": true,
        "cancellationName": "Δωρεάν ακύρωση",
        "hasCancellation": true
      }
    }
  ],
  "partialPayment": {
    "prepayAmount": 135.00,
    "nextPayments": [
      { "amount": 315.00, "dueDate": "2026-03-14" }
    ]
  }
}
```

---

### POST `/api/reservation/preparePayment`

Creates a reservation record and initiates payment with Viva Wallet.

#### Request Body

```json
{
  "hotelId": "1-VAROSVILL",
  "checkIn": "17/03/2026",
  "checkOut": "20/03/2026",
  "rooms": 1,
  "children": "0",
  "adults": 2,
  "party": "[{\"adults\":2}]",
  "selectedRates": "[{\"rateId\":\"328000-226-2\",\"count\":1}]",
  "totalPrice": 450.00,
  "prepayAmount": 135.00,
  "couponCode": "SUMMER20",
  "customerInfo": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+306971234567",
    "requests": "Late check-in please"
  }
}
```

#### Response

```json
{
  "orderCode": "4918784106772600"
}
```

The `orderCode` is used to redirect the user to Viva Wallet's payment page.

---

### POST `/api/reservation/paymentSucceed`

Called after successful Viva Wallet payment to confirm the booking.

#### Request Body

```json
{
  "tid": "dc90abcc-0350-4383-a624-5821811aedb9",
  "orderCode": "7224745916872609"
}
```

#### Response (Success)

```json
{
  "successfulPayment": true,
  "data": {
    "checkIn": "17/03/2026",
    "checkOut": "20/03/2026",
    "hotelName": "Varos Village Hotel",
    "reservationId": 12345
  }
}
```

#### Response (Error)

```json
{
  "successfulPayment": false,
  "error": "Υπήρξε πρόβλημα με την πληρωμή...",
  "errorCode": "RES_ERROR"
}
```

---

### POST `/api/reservation/paymentFailed`

Called when payment fails to retrieve order data for retry.

#### Request Body

```json
{
  "orderCode": "4918784106772600"
}
```

#### Response

Same as `/checkout` response with:
```json
{
  "errorCode": "PAY_FAILED",
  "labelErrorMessage": "Η πληρωμή απέτυχε. Παρακαλώ δοκιμάστε ξανά."
}
```

---

### POST `/api/reservation/cancelBooking`

Cancels an existing booking.

#### Query Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `bookingNumber` | string | Yes | The order code of the booking |

---

### POST `/api/reservation/applyCoupon`

Applies a coupon code and returns updated pricing.

#### Request Body

```json
{
  "couponCode": "SUMMER20",
  "reservationDetails": {
    "hotelId": "1-VAROSVILL",
    "checkIn": "17/03/2026",
    "checkOut": "20/03/2026",
    "children": "",
    "adults": "2",
    "party": "[{\"adults\":2}]",
    "selectedRates": "[{\"rateId\":\"328000-226-2\",\"count\":1}]",
    "totalPrice": 450.00
  },
  "formData": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+306971234567",
    "requests": ""
  }
}
```

#### Response

Same as `/checkout` response with updated prices and coupon validation.

---

## Party Format

The `party` parameter defines room occupancy for searches:

### Single Room
```json
[{"adults":2}]                        // 2 adults
[{"adults":2,"children":[5,10]}]      // 2 adults, 2 children (ages 5 and 10)
```

### Multiple Rooms
```json
[
  {"adults":2,"children":[5]},        // Room 1: 2 adults, 1 child (age 5)
  {"adults":2},                        // Room 2: 2 adults
  {"adults":1,"children":[3,8]}       // Room 3: 1 adult, 2 children
]
```

---

## Rate ID Format

Rate IDs encode the rate and party configuration:

```
{rateNumber}-{partyEncoding}

Examples:
328000-226-2          → Rate 328000-226 for 2 adults
328000-226-2_5_10     → Rate 328000-226 for 2 adults, children ages 5 and 10
```

---

## Error Codes

| Code | Description |
|------|-------------|
| `PAY_FAILED` | Payment failed at Viva Wallet |
| `RES_ERROR` | Error creating reservation in provider |
| `NO_RES` | Reservation not found |
| `Error` | Generic error (check errorMessage) |

---

## HTTP Status Codes

| Code | Description |
|------|-------------|
| `200` | Success |
| `400` | Bad request (invalid parameters) |
| `500` | Internal server error |
