# Phase 3 — Steps 6–12 Implementation Pack (Owned ProviderId=0)

This is a practical “do this next” guide for continuing Phase 3 after you’ve completed:
1) EF entities, 2) DbContext config, 3) Migration + SQL script, 4) `IOwnedInventoryStore`, 5) `OwnedInventoryRepository`.

---

## Step 6 — Create `TravelBridge.Providers.Owned` project

### Create project
```bash
dotnet new classlib -n TravelBridge.Providers.Owned
dotnet sln add .\TravelBridge.Providers.Owned\TravelBridge.Providers.Owned.csproj
dotnet add .\TravelBridge.Providers.Owned\TravelBridge.Providers.Owned.csproj reference .\TravelBridge.Providers.Abstractions\TravelBridge.Providers.Abstractions.csproj
```

### Add folder structure
- `TravelBridge.Providers.Owned/`
  - `OwnedHotelProvider.cs`
  - `OwnedPartyHelpers.cs` (optional helper)
  - `README.md` (optional)

---

## Step 7 — Implement `OwnedHotelProvider`

### Principles
- Provider references **only** Abstractions.
- Provider calls `IOwnedInventoryStore` (implemented in API via EF).
- Provider returns provider-neutral models (`HotelInfoResult`, `HotelAvailabilityResult`, etc.).
- RateId format: `rt_{roomTypeId}-{adults}[_{childAges...}]` (no RoomsCount in the ID).

### Party helpers (recommended)
Create a helper to compute:
- `requestedRooms` from party
- `partySuffix` for RateId
- adults/children ages used for suffix

**Pseudo** (adjust to your actual Party model shape):
```csharp
public static class PartyHelpers
{
    public static int GetRequestedRooms(PartyConfiguration party)
        => party.Items?.Sum(x => Math.Max(1, x.RoomsCount)) ?? Math.Max(1, party.RoomCount);

    public static int GetAdults(PartyConfiguration party)
        => party.Items?.FirstOrDefault()?.Adults ?? party.Adults;

    public static int[] GetChildrenAges(PartyConfiguration party)
        => party.Items?.FirstOrDefault()?.ChildrenAges ?? party.ChildrenAges ?? Array.Empty<int>();

    public static string GetPartySuffix(PartyConfiguration party)
    {
        var adults = GetAdults(party);
        var kids = GetChildrenAges(party);
        return kids.Length > 0 ? $"{adults}_{string.Join("_", kids)}" : adults.ToString();
    }

    public static string? ToPartyJson(PartyConfiguration party) => party.PartyJson; // if you already have it
}
```

### `OwnedHotelProvider` skeleton (MVP)
```csharp
using Microsoft.Extensions.Logging;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Providers.Owned;

public sealed class OwnedHotelProvider : IHotelProvider
{
    private readonly IOwnedInventoryStore _store;
    private readonly ILogger<OwnedHotelProvider> _logger;

    public OwnedHotelProvider(IOwnedInventoryStore store, ILogger<OwnedHotelProvider> logger)
    {
        _store = store;
        _logger = logger;
    }

    public int ProviderId => ProviderIds.Owned; // 0

    public async Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default)
    {
        var hotelId = long.Parse(query.HotelId);
        var hotel = await _store.GetHotelByIdAsync(hotelId, ct);
        if (hotel is null) return HotelInfoResult.Failure("NOT_FOUND", "Hotel not found.");

        var rooms = await _store.GetRoomTypesByHotelIdAsync(hotelId, ct);

        return HotelInfoResult.Success(new HotelInfoData
        {
            HotelCode = hotel.HotelId.ToString(),
            HotelName = hotel.Name,
            Location = hotel.Location is null ? null : new HotelLocationData
            {
                Latitude = (double)(hotel.Location.Latitude ?? 0),
                Longitude = (double)(hotel.Location.Longitude ?? 0),
                Name = hotel.Location.Name
            },
            Rooms = rooms.Select(r => new HotelRoomSummaryData
            {
                RoomCode = r.Code,
                RoomName = r.Name
            }).ToList()
        });
    }

    public async Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default)
    {
        var hotelId = long.Parse(query.HotelId);
        var room = await _store.GetRoomTypeAsync(hotelId, query.RoomId, ct);
        if (room is null) return RoomInfoResult.Failure("NOT_FOUND", "Room not found.");

        return RoomInfoResult.Success(new RoomInfoData
        {
            HotelCode = query.HotelId,
            RoomCode = room.Code,
            RoomName = room.Name
        });
    }

    public async Task<HotelAvailabilityResult> GetHotelAvailabilityAsync(HotelAvailabilityQuery query, CancellationToken ct = default)
    {
        var hotelId = long.Parse(query.HotelId);

        var hotel = await _store.GetHotelByIdAsync(hotelId, ct);
        if (hotel is null) return HotelAvailabilityResult.Failure("NOT_FOUND", "Hotel not found.");

        var roomTypes = await _store.GetRoomTypesByHotelIdAsync(hotelId, ct);
        if (roomTypes.Count == 0)
        {
            return HotelAvailabilityResult.Success(new HotelAvailabilityData
            {
                HotelCode = hotelId.ToString(),
                HotelName = hotel.Name,
                Location = hotel.Location,
                Rooms = Array.Empty<AvailableRoomData>(),
                Alternatives = Array.Empty<AlternativeDateData>()
            });
        }

        var requestedRooms = PartyHelpers.GetRequestedRooms(query.Party);
        var partySuffix = PartyHelpers.GetPartySuffix(query.Party);

        var nights = (query.CheckOut.ToDateTime(TimeOnly.MinValue) - query.CheckIn.ToDateTime(TimeOnly.MinValue)).Days;
        if (nights <= 0) return HotelAvailabilityResult.Failure("INVALID_DATES", "CheckOut must be after CheckIn.");

        // Bulk inventory read (preferred for performance)
        var inv = await _store.GetInventoryForMultipleRoomTypesAsync(
            roomTypes.Select(r => r.RoomTypeId).ToArray(),
            query.CheckIn, query.CheckOut, ct);

        var rooms = new List<AvailableRoomData>();

        foreach (var rt in roomTypes)
        {
            if (!inv.TryGetValue(rt.RoomTypeId, out var rows)) continue;

            // require coverage for each night
            if (rows.Count != nights) continue;

            var minAvail = rows.Min(x => x.AvailableUnits);
            if (minAvail < requestedRooms) continue;

            var baseRateId = $"rt_{rt.RoomTypeId}";
            var rateId = $"{baseRateId}-{partySuffix}";

            var totalPerNight = rows.Sum(x => x.PricePerNight ?? rt.BasePricePerNight);
            var total = totalPerNight * requestedRooms;
            var retail = total;

            rooms.Add(new AvailableRoomData
            {
                RoomCode = rt.Code,
                RoomName = rt.Name,
                RoomType = rt.Code,
                Rates = new[]
                {
                    new RoomRateData
                    {
                        RoomCode = rt.Code,
                        RateId = rateId,
                        RateName = "Standard",
                        TotalPrice = retail,
                        NetPrice = total,
                        RemainingRooms = minAvail,
                        HasCancellation = false,
                        SearchParty = new RatePartyInfo
                        {
                            Adults = PartyHelpers.GetAdults(query.Party),
                            ChildrenAges = PartyHelpers.GetChildrenAges(query.Party),
                            RoomsCount = requestedRooms,
                            PartyJson = PartyHelpers.ToPartyJson(query.Party)
                        }
                    }
                }
            });
        }

        return HotelAvailabilityResult.Success(new HotelAvailabilityData
        {
            HotelCode = hotelId.ToString(),
            HotelName = hotel.Name,
            Location = hotel.Location,
            Rooms = rooms,
            Alternatives = Array.Empty<AlternativeDateData>()
        });
    }

    public async Task<AlternativesResult> GetAlternativesAsync(AlternativesQuery query, CancellationToken ct = default)
    {
        // Implement scan-window alternatives using inventory availability + pricing.
        return AlternativesResult.Success(Array.Empty<AlternativeDateData>());
    }

    public async Task<SearchAvailabilityResult> SearchAvailabilityAsync(SearchAvailabilityQuery query, CancellationToken ct = default)
    {
        // MVP: bounding box prefilter + optional haversine ordering.
        return SearchAvailabilityResult.Success(Array.Empty<HotelSummaryData>());
    }
}
```

---

## Step 8 — Admin endpoints (capacity / stop-sell / read inventory)

Create a route group:
- `/admin/owned/inventory/*`
- protect with policy: `RequireAuthorization("Admin")`

Endpoints:
- `PUT /admin/owned/inventory/{roomTypeId}/capacity` (set `TotalUnits`)
- `PUT /admin/owned/inventory/{roomTypeId}/closed` (set `ClosedUnits`)
- `GET /admin/owned/inventory/{roomTypeId}?startDate&endDate`

Validation:
- `0 <= closed <= total`
- `total >= closed + held + confirmed`

---

## Step 9 — Inventory seed background service

Hosted service:
- ensures `OwnedInventoryDaily` rows exist for each active room type for:
  - today → +400 days
- inserts missing rows only
- run on startup and daily

---

## Step 10 — DI registration

In API `Program.cs`:
- `services.AddScoped<IOwnedInventoryStore, OwnedInventoryRepository>();`
- `services.AddScoped<IHotelProvider, OwnedHotelProvider>();`

---

## Step 11 — Dev seed data

Add SQL (or a CLI command) to insert:
- 1 hotel
- 2 room types
- inventory rows for next 60 days with totals and prices

---

## Step 12 — Tests

Add tests for:
- range semantics `[checkIn, checkOut)`
- requestedRooms derived from party correctly (RoomsCount)
- RateId format compatibility
- alternatives (once implemented)

---

## Recommended next action

1) Create the Owned provider project and compile it.
2) Implement `GetHotelAvailabilityAsync` end-to-end first.
3) Run API and call `/hotelRoomAvailability` with `0-<id>`.

