using Microsoft.EntityFrameworkCore;
using TravelBridge.API.DataBase;
using TravelBridge.API.Models.DB;
using TravelBridge.Providers.Abstractions.Store;

namespace TravelBridge.API.Repositories;

/// <summary>
/// EF Core implementation of the owned inventory store.
/// Provides data access for the Owned provider.
/// 
/// DATE RANGE SEMANTICS: All methods use [start, end) convention (end is EXCLUSIVE).
/// </summary>
public class OwnedInventoryRepository : IOwnedInventoryStore
{
    private readonly AppDbContext _context;
    private readonly ILogger<OwnedInventoryRepository> _logger;

    public OwnedInventoryRepository(AppDbContext context, ILogger<OwnedInventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ==================== Hotel Queries ====================

    public async Task<OwnedHotelStoreModel?> GetHotelByIdAsync(int hotelId, CancellationToken ct = default)
    {
        var hotel = await _context.OwnedHotels
            .AsNoTracking()
            .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
            .FirstOrDefaultAsync(h => h.Id == hotelId, ct);

        return hotel == null ? null : MapToStoreModel(hotel);
    }

    public async Task<OwnedHotelStoreModel?> GetHotelByCodeAsync(string hotelCode, CancellationToken ct = default)
    {
        var hotel = await _context.OwnedHotels
            .AsNoTracking()
            .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
            .FirstOrDefaultAsync(h => h.Code == hotelCode, ct);

        return hotel == null ? null : MapToStoreModel(hotel);
    }

    public async Task<List<OwnedHotelStoreModel>> SearchHotelsInBoundingBoxAsync(
        decimal minLat,
        decimal maxLat,
        decimal minLon,
        decimal maxLon,
        bool activeOnly = true,
        CancellationToken ct = default)
    {
        var query = _context.OwnedHotels
            .AsNoTracking()
            .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
            .Where(h => h.Latitude >= minLat && h.Latitude <= maxLat &&
                       h.Longitude >= minLon && h.Longitude <= maxLon);

        if (activeOnly)
        {
            query = query.Where(h => h.IsActive);
        }

        var hotels = await query.ToListAsync(ct);
        return hotels.Select(MapToStoreModel).ToList();
    }

    // ==================== Room Type Queries ====================

    public async Task<OwnedRoomTypeStoreModel?> GetRoomTypeByIdAsync(int roomTypeId, CancellationToken ct = default)
    {
        var roomType = await _context.OwnedRoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == roomTypeId, ct);

        return roomType == null ? null : MapToStoreModel(roomType);
    }

    public async Task<OwnedRoomTypeStoreModel?> GetRoomTypeByCodeAsync(int hotelId, string roomCode, CancellationToken ct = default)
    {
        var roomType = await _context.OwnedRoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.HotelId == hotelId && rt.Code == roomCode, ct);

        return roomType == null ? null : MapToStoreModel(roomType);
    }

    public async Task<List<OwnedRoomTypeStoreModel>> GetRoomTypesByHotelIdAsync(int hotelId, bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.OwnedRoomTypes
            .AsNoTracking()
            .Where(rt => rt.HotelId == hotelId);

        if (activeOnly)
        {
            query = query.Where(rt => rt.IsActive);
        }

        var roomTypes = await query.ToListAsync(ct);
        return roomTypes.Select(MapToStoreModel).ToList();
    }

    // ==================== Inventory Queries ====================

    public async Task<List<OwnedInventoryDailyStoreModel>> GetInventoryAsync(
        int roomTypeId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        // endDate is EXCLUSIVE: query uses < not <=
        var inventory = await _context.OwnedInventoryDaily
            .AsNoTracking()
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDate)
            .OrderBy(inv => inv.Date)
            .ToListAsync(ct);

        return inventory.Select(MapToStoreModel).ToList();
    }

    public async Task<Dictionary<int, List<OwnedInventoryDailyStoreModel>>> GetInventoryForMultipleRoomTypesAsync(
        List<int> roomTypeIds,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        // endDate is EXCLUSIVE: query uses < not <=
        var inventory = await _context.OwnedInventoryDaily
            .AsNoTracking()
            .Where(inv => roomTypeIds.Contains(inv.RoomTypeId) &&
                         inv.Date >= startDate &&
                         inv.Date < endDate)
            .OrderBy(inv => inv.RoomTypeId)
            .ThenBy(inv => inv.Date)
            .ToListAsync(ct);

        return inventory
            .GroupBy(inv => inv.RoomTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(MapToStoreModel).ToList()
            );
    }

    // ==================== Admin Operations ====================

    public async Task UpdateInventoryCapacityAsync(
        int roomTypeId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        int totalUnits,
        CancellationToken ct = default)
    {
        // Validation
        if (totalUnits < 0)
            throw new ArgumentException("TotalUnits cannot be negative", nameof(totalUnits));

        // Ensure inventory rows exist first
        var days = endDateExclusive.DayNumber - startDate.DayNumber;
        await EnsureInventoryExistsAsync(roomTypeId, startDate, days, ct);

        // Validate against existing counters for the affected rows (CRITICAL for capacity decreases)
        var inventoryRows = await _context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDateExclusive)
            .ToListAsync(ct);

        foreach (var row in inventoryRows)
        {
            if (totalUnits < row.ClosedUnits + row.HeldUnits + row.ConfirmedUnits)
            {
                throw new InvalidOperationException(
                    $"Cannot set TotalUnits to {totalUnits} for date {row.Date}: " +
                    $"would violate constraint (ClosedUnits={row.ClosedUnits} + HeldUnits={row.HeldUnits} + " +
                    $"ConfirmedUnits={row.ConfirmedUnits} = {row.ClosedUnits + row.HeldUnits + row.ConfirmedUnits})");
            }
        }

        // Update capacity (endDateExclusive is EXCLUSIVE)
        var rowsAffected = await _context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDateExclusive)
            .ExecuteUpdateAsync(inv => inv
                .SetProperty(i => i.TotalUnits, totalUnits)
                .SetProperty(i => i.LastModifiedUtc, DateTime.UtcNow), ct);

        _logger.LogInformation(
            "Updated capacity for RoomTypeId {RoomTypeId} from {StartDate} to {EndDate} (exclusive): {TotalUnits} units ({RowsAffected} rows)",
            roomTypeId, startDate, endDateExclusive, totalUnits, rowsAffected);
    }

    public async Task UpdateInventoryClosedUnitsAsync(
        int roomTypeId,
        DateOnly startDate,
        DateOnly endDateExclusive,
        int closedUnits,
        CancellationToken ct = default)
    {
        // Validation
        if (closedUnits < 0)
            throw new ArgumentException("ClosedUnits cannot be negative", nameof(closedUnits));

        // Ensure inventory rows exist first
        var days = endDateExclusive.DayNumber - startDate.DayNumber;
        await EnsureInventoryExistsAsync(roomTypeId, startDate, days, ct);

        // Validate against TotalUnits for the affected rows
        var inventoryRows = await _context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDateExclusive)
            .ToListAsync(ct);

        foreach (var row in inventoryRows)
        {
            if (closedUnits > row.TotalUnits)
            {
                throw new InvalidOperationException(
                    $"ClosedUnits ({closedUnits}) cannot exceed TotalUnits ({row.TotalUnits}) for date {row.Date}");
            }
            if (closedUnits + row.HeldUnits + row.ConfirmedUnits > row.TotalUnits)
            {
                throw new InvalidOperationException(
                    $"ClosedUnits ({closedUnits}) + HeldUnits ({row.HeldUnits}) + ConfirmedUnits ({row.ConfirmedUnits}) " +
                    $"exceeds TotalUnits ({row.TotalUnits}) for date {row.Date}");
            }
        }

        // Update closed units (endDateExclusive is EXCLUSIVE)
        var rowsAffected = await _context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDateExclusive)
            .ExecuteUpdateAsync(inv => inv
                .SetProperty(i => i.ClosedUnits, closedUnits)
                .SetProperty(i => i.LastModifiedUtc, DateTime.UtcNow), ct);

        _logger.LogInformation(
            "Updated closed units for RoomTypeId {RoomTypeId} from {StartDate} to {EndDate} (exclusive): {ClosedUnits} units ({RowsAffected} rows)",
            roomTypeId, startDate, endDateExclusive, closedUnits, rowsAffected);
    }

    public async Task EnsureInventoryExistsAsync(
        int roomTypeId,
        DateOnly startDate,
        int days,
        CancellationToken ct = default)
    {
        // Get room type to fetch default values
        var roomType = await _context.OwnedRoomTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.Id == roomTypeId, ct);

        if (roomType == null)
        {
            throw new InvalidOperationException($"Room type {roomTypeId} not found");
        }

        // Get existing inventory dates
        var endDate = startDate.AddDays(days);
        var existingDates = await _context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDate)
            .Select(inv => inv.Date)
            .ToListAsync(ct);

        var existingDatesSet = existingDates.ToHashSet();

        // Create missing rows using DefaultTotalUnits from room type
        var missingRows = new List<OwnedInventoryDaily>();
        for (int i = 0; i < days; i++)
        {
            var date = startDate.AddDays(i);
            if (!existingDatesSet.Contains(date))
            {
                missingRows.Add(new OwnedInventoryDaily
                {
                    RoomTypeId = roomTypeId,
                    Date = date,
                    TotalUnits = roomType.DefaultTotalUnits,
                    ClosedUnits = 0,
                    HeldUnits = 0,
                    ConfirmedUnits = 0,
                    PricePerNight = null, // Use room type base price
                    LastModifiedUtc = DateTime.UtcNow
                });
            }
        }

        if (missingRows.Count > 0)
        {
            await _context.OwnedInventoryDaily.AddRangeAsync(missingRows, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Created {Count} inventory rows for RoomTypeId {RoomTypeId} from {StartDate} using DefaultTotalUnits={DefaultTotal}",
                missingRows.Count, roomTypeId, startDate, roomType.DefaultTotalUnits);
        }
    }

    // ==================== Mapping Helpers ====================

    private static OwnedHotelStoreModel MapToStoreModel(OwnedHotel hotel)
    {
        return new OwnedHotelStoreModel
        {
            Id = hotel.Id,
            Code = hotel.Code,
            Name = hotel.Name,
            Description = hotel.Description,
            Type = hotel.Type,
            Rating = hotel.Rating,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            City = hotel.City,
            Address = hotel.Address,
            Country = hotel.Country,
            PostalCode = hotel.PostalCode,
            CheckInTime = hotel.CheckInTime,
            CheckOutTime = hotel.CheckOutTime,
            RoomTypes = hotel.RoomTypes.Select(MapToStoreModel).ToList()
        };
    }

    private static OwnedRoomTypeStoreModel MapToStoreModel(OwnedRoomType roomType)
    {
        return new OwnedRoomTypeStoreModel
        {
            Id = roomType.Id,
            HotelId = roomType.HotelId,
            Code = roomType.Code,
            Name = roomType.Name,
            Description = roomType.Description,
            MaxAdults = roomType.MaxAdults,
            MaxChildren = roomType.MaxChildren,
            MaxTotalOccupancy = roomType.MaxTotalOccupancy,
            BasePricePerNight = roomType.BasePricePerNight,
            DefaultTotalUnits = roomType.DefaultTotalUnits
        };
    }

    private static OwnedInventoryDailyStoreModel MapToStoreModel(OwnedInventoryDaily inv)
    {
        return new OwnedInventoryDailyStoreModel
        {
            RoomTypeId = inv.RoomTypeId,
            Date = inv.Date,
            TotalUnits = inv.TotalUnits,
            ClosedUnits = inv.ClosedUnits,
            HeldUnits = inv.HeldUnits,
            ConfirmedUnits = inv.ConfirmedUnits,
            PricePerNight = inv.PricePerNight
        };
    }
}
