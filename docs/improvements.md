# TravelBridge - Improvement Plan

> **Process**: Each task will be discussed before implementation. Documentation updated after each change.

---

## 📋 Action Queue (In Order)

| # | Task | Priority | Effort | Status |
|---|------|----------|--------|--------|
| 1 | Fix `Task.WaitAll` → `await Task.WhenAll` | High | Low | ✅ Done |
| 2 | Restrict CORS to allowed origins | High | Low | ✅ Done |
| 3 | Move hardcoded card credentials to config | High | Low | ✅ Done |
| 4 | Make 10% margin configurable | Medium | Low | ✅ Done |
| 5 | Add error notification email (payment success + booking fail) | High | Medium | ✅ Done |
| 6 | Improve logging (session/correlation ID) | Medium | Medium | ✅ Done |
| 7 | Upgrade to .NET 10 | Medium | Medium | 🔒 Postponed |
| 8 | 🔮 Future: Replace MapBox with Google + better area logic | Low | High | 🔒 Postponed |
| 9 | Add retry policy for external API calls | Medium | Low | ✅ Done |
| 10 | Add health checks endpoint | Low | Low | ✅ Done |
| 11 | Add response caching for hotel/room info | Low | Medium | ✅ Done |
| 12 | Add rate limiting | Low | Medium | ✅ Done |
| 13 | Add unit tests | Medium | High | ✅ Done |
| 14 | Fix min_stay JSON deserialization bug | High | Low | ✅ Done |

---

## Task Details

### 1. Fix `Task.WaitAll` → `await Task.WhenAll`
**Problem**: Blocking calls hurt scalability
**Files**: `ReservationEndpoints.cs`, `HotelEndpoint.cs`
**Change**: Replace `Task.WaitAll()` with `await Task.WhenAll()`

---

### 2. Restrict CORS Origins
**Problem**: Currently allows all origins (`AllowAnyOrigin()`)
**Solution**: Whitelist only:
- `https://my-diakopes.gr`
- `https://www.my-diakopes.gr`
- `https://travelproject.gr`
- `https://www.travelproject.gr`
- `http://localhost:*` (development)

---

### 3. Move Card Credentials to Config
**Problem**: Hardcoded test card in `WebHotelierPropertiesService.cs`
**Solution**: Move to `appsettings.json` under `WebHotelierApi` section

**About secrets security** (your question):
You're right - even with Azure Key Vault, you need credentials to access it. Options:
1. **Managed Identity** (Azure) - No credentials needed if hosted on Azure
2. **Environment Variables** - Not in files, set on server
3. **appsettings.json** (current) - Fine for now if server is secure
4. **User Secrets** (dev only) - `dotent user-secrets`

**Recommendation**: Keep in `appsettings.json` for now. If you move to Azure hosting later, use Managed Identity. The card data is for WebHotelier's payment guarantee - it's not actually charged.

---

### 4. Make 10% Margin Configurable
**Problem**: Hardcoded `0.10m` (10%) margin in pricing logic
**Files**: `MappingExtensions.cs`, `General.cs`, `SingleAvailabilityData.cs`
**Solution**: Added `Pricing` section to `appsettings.json`:
```json
{
  "Pricing": {
    "MinimumMarginPercent": 10,
    "SpecialHotelDiscountPercent": 5
  }
}
```
**Implementation**: Created `PricingOptions.cs` with static `PricingConfig` holder initialized at startup.

---

### 5. Error Notification Email
**Problem**: If payment succeeds but WebHotelier booking fails, no alert sent
**Solution**: Added `SendBookingErrorNotificationAsync` method in `WebHotelierPropertiesService.cs`
- Sends urgent email to `bookings@my-diakopes.gr` and `achilleaskaragiannis@outlook.com`
- Includes: reservation ID, hotel info, customer details, payment amount, error message
- Returns specific error message to user with reservation ID to reference
- Updated `ConfirmPayment()` in `ReservationEndpoints.cs` to catch booking failures separately

---

### 6. Improve Logging (Session/Correlation ID)
**Problem**: Hard to trace requests across logs, request/response body logging on every request hurts performance
**Solution**: 
- Created `CorrelationIdMiddleware.cs` that:
  - Reads `X-Session-Id` from FE request headers (or generates one if missing)
  - Generates unique `X-Request-Id` per request
  - Returns both in response headers for FE tracking
  - Enriches all log entries with SessionId/RequestId via Serilog
- Updated Serilog format: `[{Timestamp}] [{SessionId}/{RequestId}] {Message}`
- Made request/response body logging conditional (Development only)

---

### 7. Upgrade to .NET 10
**Status**: 🔒 POSTPONED - causes errors
**Changes needed**:
- Update `TravelBridge.API.csproj` TFM to `net10.0`
- Update NuGet packages
- Test all functionality
- Update Docker/deployment if applicable
**Note**: Wait for stable .NET 10 packages and resolve compatibility issues before attempting upgrade.

---

### 8. 🔒 POSTPONED: Google Maps + Better Area Logic
**Goal**: Replace MapBox with Google Places API, improve geographic filtering
**Reason for postpone**: Works fine currently, significant effort
**Notes for later**:
- Google Places Autocomplete
- Get proper region boundaries (polygons vs bbox)
- Smarter hotel-in-area matching

---

### 9. Add Retry Policy for External API Calls
**Problem**: External APIs can have transient failures
**Solution**: Added Polly retry policies to HttpClient registrations:
- **WebHotelier, MapBox, HereMaps**: 3 retries with 100ms, 250ms, 500ms delays
- **Viva (payments)**: 1 retry with 100ms delay (fail fast for payments)
**Package**: `Microsoft.Extensions.Http.Polly`

---

### 10. Add Health Checks Endpoint
**Problem**: No way to monitor if API and dependencies are healthy
**Solution**: Added `/health` endpoint with MySQL database check
**Package**: `AspNetCore.HealthChecks.MySql`

---

### 11. Add Response Caching for Hotel/Room Info
**Problem**: Hotel info rarely changes but fetched every time
**Solution**: Added `IMemoryCache` to `WebHotelierPropertiesService`:
- `GetHotelInfo()` cached for 6 hours (key: `hotel_info_{hotelId}`)
- `GetRoomInfo()` cached for 6 hours (key: `room_info_{hotelId}_{roomcode}`)

---

### 12. Add Rate Limiting
**Problem**: No protection against API abuse
**Solution**: Added rate limiting middleware:
- 100 requests per minute per IP
- Returns HTTP 429 Too Many Requests when limit exceeded
- No queuing (immediate rejection)

---

### 13. Add Unit Tests
**Problem**: No automated tests for critical business logic
**Solution**: Created `TravelBridge.Tests` project with:
- **12 Pricing Unit Tests**: Margin calculations, coupon application, special hotels
- **8 WebHotelier Integration Tests**: Hotel info, room info, property search (real API calls)
**Packages**: xUnit, Moq, Microsoft.NET.Test.Sdk

---

### 14. Fix min_stay JSON Deserialization Bug (Production Issue)
**Problem**: WebHotelier API returns `min_stay` as either string or int. When string, JSON deserialization fails with:
```
JsonException: The JSON value could not be converted to System.Int32. Path: $.data.days[0].min_stay
```
**Impact**: 500 errors in production for hotels without availability (when alternatives are fetched)
**Solution**: Created `StringOrIntJsonConverter` that handles both string and int values
**Files**: 
- `TravelBridge.API/Helpers/Converters/StringOrIntJsonConverter.cs` (new)
- `TravelBridge.API/Contracts/SingleAvailabilityData.cs` (modified - `AlternativeDayInfo.min_stay`)
**Affected Hotels** (from logs): ARIADNIPAL, HOTELSISSY, GALINIWSR, AQUAMARES, and others

---

## ✅ Completed Tasks

| Task | Date | Notes |
|------|------|-------|
| Create documentation | - | Initial docs created |
| Fix `Task.WaitAll` | - | Changed to `await Task.WhenAll` in 4 locations |
| Restrict CORS | - | Production: my-diakopes.gr, travelproject.gr (+ www). Development: localhost |
| Move card credentials to config | - | Added `GuaranteeCard` section to `WebHotelierApi` in appsettings.json |
| Make margin configurable | - | Added `Pricing` section with `MinimumMarginPercent` and `SpecialHotelDiscountPercent` |
| Error notification email | - | Added `SendBookingErrorNotificationAsync` for payment success + booking fail scenario |
| Improve logging | - | Added `CorrelationIdMiddleware` with X-Session-Id/X-Request-Id |
| Add retry policy | - | Polly: 3 retries for WebHotelier/MapBox, 1 retry for Viva |
| Add health checks | - | `/health` endpoint with MySQL check |
| Add caching | - | 6-hour cache for hotel/room info |
| Add rate limiting | - | 100 requests/minute/IP |
| Add unit tests | - | 20 tests (12 pricing + 8 WebHotelier integration) |
| Fix min_stay bug | - | Created StringOrIntJsonConverter for flexible JSON parsing |
