# TravelBridge API Endpoints Reference

## Base URL
- Development: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

---

## Search & Autocomplete (`/api/plugin`)

### GET `/api/plugin/autocomplete`
Returns matching locations and hotels for search term.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| searchQuery | string | No | Search term (min 3 chars) |

**Response:**
```json
{
  "hotels": [
    {
      "id": "1-VAROSVILL",
      "provider": 1,
      "name": "Varos Village Hotel",
      "location": "Limenaria",
      "country": "GR",
      "type": "Hotel",
      "mappedTypes": ["Hotel"]
    }
  ],
  "locations": [
    {
      "name": "Trikala",
      "region": "Thessaly",
      "id": "[21.77,39.55,21.78,39.56]-39.555-21.775",
      "country": "GR",
      "type": "location"
    }
  ]
}
```

### GET `/api/plugin/submitSearch`
Search hotels by location with filters.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| checkin | string | Yes | Check-in date (dd/MM/yyyy) |
| checkOut | string | Yes | Check-out date (dd/MM/yyyy) |
| bbox | string | Yes | Bounding box from autocomplete |
| adults | int | No* | Adults count (if 1 room) |
| children | string | No | Child ages comma-separated |
| rooms | int | No* | Room count (if 1 room) |
| party | string | No* | JSON party array (if multiple rooms) |
| searchTerm | string | Yes | Location name (passthrough) |
| page | int | No | Page number (default 0) |
| sorting | string | No | `popularity`, `distance`, `price_asc`, `price_desc` |
| minPrice | int | No | Min price per night filter |
| maxPrice | int | No | Max price per night filter |
| hotelTypes | string | No | Comma-separated types |
| boardTypes | string | No | Comma-separated board IDs |
| rating | string | No | Comma-separated star ratings |

**Example:**
```
/api/plugin/submitSearch?checkin=15/06/2025&checkOut=20/06/2025&bbox=[23.377,34.730,26.447,35.773]-35.340-25.134&adults=2&rooms=1&searchTerm=Crete
```

**Response:**
```json
{
  "results": [
    {
      "id": "1-CRETAPAL",
      "code": "CRETAPAL",
      "name": "Creta Palace",
      "rating": 5,
      "minPrice": 450,
      "minPricePerDay": 90,
      "salePrice": 500,
      "photoL": "https://...",
      "boards": [{"id": 1, "name": "All Inclusive"}],
      "mappedTypes": ["Resort"]
    }
  ],
  "resultsCount": 25,
  "searchTerm": "Crete",
  "filters": [
    {
      "name": "Ευρος Τιμής",
      "id": "price",
      "type": "range",
      "min": 50,
      "max": 500
    },
    {
      "name": "Τύποι Καταλυμμάτων",
      "id": "hotelTypes",
      "type": "values",
      "values": [
        {"id": "Hotel", "name": "Hotel", "count": 15}
      ]
    }
  ]
}
```

### GET `/api/plugin/allproperties`
Returns all available properties (for specific use cases).

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| type | string | No | Filter by property type |

---

## Hotel Details (`/api/hotel`)

### GET `/api/hotel/hotelInfo`
Get basic hotel information.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| HotelId | string | Yes | Hotel ID (e.g., `1-VAROSVILL`) |

**Response:**
```json
{
  "data": {
    "id": "VAROSVILL",
    "name": "Varos Village Hotel",
    "rating": 4,
    "type": "Hotel",
    "operation": {
      "checkinTime": "14:00",
      "checkoutTime": "11:00"
    },
    "location": { "name": "Limenaria", "country": "GR" },
    "largePhotos": ["https://..."],
    "amenities": ["WiFi", "Pool"],
    "provider": 1
  }
}
```

### GET `/api/hotel/roomInfo`
Get room type details.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| HotelId | string | Yes | Hotel ID |
| RoomId | string | Yes | Room type code (e.g., `LVLSTD`) |

### GET `/api/hotel/hotelRoomAvailability`
Get hotel availability for dates.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| checkin | string | Yes | dd/MM/yyyy |
| checkOut | string | Yes | dd/MM/yyyy |
| adults | int | No* | Adults (if 1 room) |
| children | string | No | Child ages |
| rooms | int | No* | Room count |
| party | string | No* | JSON party array |
| hotelId | string | Yes | Hotel ID |

### GET `/api/hotel/HotelFullInfo`
Get complete hotel info + availability (combined response).

**Parameters:** Same as `hotelRoomAvailability`

**Response:**
```json
{
  "hotelData": {
    "id": "VAROSVILL",
    "name": "Varos Village Hotel",
    "minPrice": 120,
    "minPricePerNight": 40,
    "boards": [{"id": 3, "name": "Bed & Breakfast"}],
    "customInfo": "<html>..."
  },
  "rooms": [
    {
      "type": "DBL",
      "roomName": "Double Room",
      "rates": [
        {
          "id": "328000-226-2",
          "totalPrice": 120,
          "netPrice": 100,
          "rateProperties": {
            "board": "Bed & Breakfast",
            "hasCancellation": true,
            "cancellationExpiry": "10/06/2025 14:00",
            "payments": [...]
          }
        }
      ]
    }
  ],
  "alternatives": [
    {
      "checkIn": "2025-06-17",
      "checkout": "2025-06-20",
      "minPrice": 110,
      "nights": 3
    }
  ]
}
```

---

## Reservations (`/api/reservation`)

### GET `/api/reservation/checkout`
Get checkout summary for selected rates.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| checkin | string | Yes | dd/MM/yyyy |
| checkOut | string | Yes | dd/MM/yyyy |
| couponCode | string | No | Discount code |
| hotelId | string | Yes | Hotel ID |
| selectedRates | string | Yes | JSON array of selected rates |

**selectedRates format:**
```json
[
  {"rateId": "328000-226-2", "count": 1, "roomType": "DBL"},
  {"rateId": "273063-3-2_5", "count": 1, "roomType": "FAM"}
]
```

**Response:**
```json
{
  "hotelData": { "id": "...", "name": "..." },
  "checkIn": "15/06/2025",
  "checkOut": "20/06/2025",
  "nights": 5,
  "totalPrice": 600,
  "selectedPeople": "2 ενήλικες, 1 παιδί, 2 δωμάτια",
  "rooms": [
    {
      "type": "DBL",
      "roomName": "Double Room",
      "rateId": "328000-226-2",
      "selectedQuantity": 1,
      "totalPrice": 300,
      "rateProperties": {
        "board": "Bed & Breakfast",
        "hasCancellation": true,
        "cancellationName": "Δωρεάν ακύρωση",
        "cancellationExpiry": "10/06/2025 14:00"
      }
    }
  ],
  "partialPayment": {
    "prepayAmount": 180,
    "nextPayments": [
      { "amount": 420, "dueDate": "2025-06-10" }
    ]
  },
  "couponValid": true,
  "couponDiscount": "-10 %"
}
```

### POST `/api/reservation/preparePayment`
Create reservation and get Viva payment code.

**Request Body:**
```json
{
  "hotelId": "1-VAROSVILL",
  "checkIn": "15/06/2025",
  "checkOut": "20/06/2025",
  "rooms": 2,
  "adults": 2,
  "children": "5",
  "party": "[{\"adults\":2,\"children\":[5]},{\"adults\":1}]",
  "selectedRates": "[{\"rateId\":\"328000-226-2_5\",\"count\":1}]",
  "totalPrice": 600,
  "prepayAmount": 180,
  "couponCode": "SUMMER10",
  "customerInfo": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+306977771645",
    "requests": "Late check-in please"
  }
}
```

**Response:**
```json
{
  "orderCode": "4918784106772600"
}
```

**Flow:** Redirect user to Viva payment page with orderCode.

### POST `/api/reservation/paymentSucceed`
Called after successful Viva payment.

**Request Body:**
```json
{
  "tid": "dc90abcc-0350-4383-a624-5821811aedb9",
  "orderCode": "4918784106772600"
}
```

**Response:**
```json
{
  "successfullPayment": true,
  "data": {
    "checkIn": "15/06/2025",
    "checkOut": "20/06/2025",
    "hotelName": "Varos Village Hotel",
    "reservationId": 123
  }
}
```

**Error Response:**
```json
{
  "error": "Υπήρξε πρόβλημα με την πληρωμή...",
  "errorCode": "RES_ERROR"
}
```

### POST `/api/reservation/paymentFailed`
Called when Viva payment fails.

**Request Body:**
```json
{
  "orderCode": "4918784106772600"
}
```

**Response:** Returns `CheckoutResponse` with error message for retry.

### POST `/api/reservation/applyCoupon`
Apply/validate coupon code.

**Request Body:**
```json
{
  "couponCode": "SUMMER10",
  "reservationDetails": {
    "hotelId": "1-VAROSVILL",
    "checkIn": "15/06/2025",
    "checkOut": "20/06/2025",
    "selectedRates": "...",
    "totalPrice": 600
  },
  "formData": {
    "firstName": "John",
    "lastName": "Doe",
    "email": "john@example.com",
    "phone": "+306977771645"
  }
}
```

### POST `/api/reservation/cancelBooking`
Cancel a booking.

**Parameters:**
| Name | Type | Required | Description |
|------|------|----------|-------------|
| bookingNumber | string | Yes | Viva orderCode |

---

## Error Responses

All endpoints may return:

**400 Bad Request:**
```json
{
  "errorCode": "Error",
  "errorMessage": "Invalid checkin date format. Use dd/MM/yyyy."
}
```

**500 Internal Server Error:**
```json
{
  "errorCode": "Error",
  "errorMessage": "Internal Error"
}
```

---

## Board Type IDs Reference

| ID | Greek | English |
|----|-------|---------|
| 0 | Χωρίς γεύματα | No board |
| 1 | All Inclusive | All Inclusive |
| 3 | Διαμονή & Πρωινό | Bed & Breakfast |
| 10 | Πλήρης Διατροφή | Full Board |
| 12 | Ημιδιατροφή | Half Board |
| 14 | Χωρίς διατροφή | Room Only |

---

## Filter Types Reference

| ID | Greek Name | Type |
|----|------------|------|
| price | Ευρος Τιμής | range |
| hotelTypes | Τύποι Καταλυμμάτων | values |
| boardTypes | Τύποι Διατροφής | values |
| rating | Αστέρια | values |
