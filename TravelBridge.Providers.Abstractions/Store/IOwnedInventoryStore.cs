namespace TravelBridge.Providers.Abstractions.Store;

/// <summary>
/// Data access interface for owned hotel inventory.
/// Implemented in API layer with EF Core.
/// Keeps providers decoupled from database concerns.
/// 
/// DATE RANGE SEMANTICS: All date ranges use [start, end) convention.
/// - endDate is EXCLUSIVE (not included in the range)
/// - For a 3-night stay (check-in July 15, check-out July 18):
///   startDate = July 15, endDate = July 18
///   Inventory consumed: July 15, 16, 17 (NOT July 18)
/// </summary>
public interface IOwnedInventoryStore
{
    // ==================== Hotel Queries ====================
    
    /// <summary>
    /// Get hotel by database ID.
    /// </summary>
    Task<OwnedHotelStoreModel?> GetHotelByIdAsync(int hotelId, CancellationToken ct = default);
    
    /// <summary>
    /// Get hotel by unique code (used in composite IDs like "0-OWNTEST01").
    /// </summary>
    Task<OwnedHotelStoreModel?> GetHotelByCodeAsync(string hotelCode, CancellationToken ct = default);
    
    /// <summary>
    /// Search hotels within a geographic bounding box.
    /// Used for area-based searches (destination search).
    /// </summary>
    Task<List<OwnedHotelStoreModel>> SearchHotelsInBoundingBoxAsync(
        decimal minLat, 
        decimal maxLat, 
        decimal minLon, 
        decimal maxLon, 
        bool activeOnly = true,
        CancellationToken ct = default);
    
    // ==================== Room Type Queries ====================
    
    /// <summary>
    /// Get room type by database ID.
    /// </summary>
    Task<OwnedRoomTypeStoreModel?> GetRoomTypeByIdAsync(int roomTypeId, CancellationToken ct = default);
    
    /// <summary>
    /// Get room type by hotel ID and room code.
    /// </summary>
    Task<OwnedRoomTypeStoreModel?> GetRoomTypeByCodeAsync(int hotelId, string roomCode, CancellationToken ct = default);
    
    /// <summary>
    /// Get all room types for a hotel.
    /// </summary>
    Task<List<OwnedRoomTypeStoreModel>> GetRoomTypesByHotelIdAsync(int hotelId, bool activeOnly = true, CancellationToken ct = default);
    
    // ==================== Inventory Queries ====================
    
    /// <summary>
    /// Get inventory for a single room type across a date range.
    /// Returns one row per night in [startDate, endDate).
    /// endDate is EXCLUSIVE.
    /// </summary>
    Task<List<OwnedInventoryDailyStoreModel>> GetInventoryAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Get inventory for multiple room types across a date range.
    /// Efficient for checking availability across all room types in a hotel.
    /// Returns dictionary keyed by roomTypeId.
    /// Date range: [startDate, endDate) - endDate is EXCLUSIVE.
    /// </summary>
    Task<Dictionary<int, List<OwnedInventoryDailyStoreModel>>> GetInventoryForMultipleRoomTypesAsync(
        List<int> roomTypeIds, 
        DateOnly startDate, 
        DateOnly endDate, 
        CancellationToken ct = default);
    
    // ==================== Admin Operations (Phase 3) ====================
    
    /// <summary>
    /// Update total capacity for a room type across a date range.
    /// Creates inventory rows if they don't exist.
    /// Date range: [startDate, endDateExclusive) - endDateExclusive is NOT included.
    /// </summary>
    Task UpdateInventoryCapacityAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDateExclusive, 
        int totalUnits, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Update closed units (stop-sell) for a room type across a date range.
    /// Creates inventory rows if they don't exist.
    /// Date range: [startDate, endDateExclusive) - endDateExclusive is NOT included.
    /// Validates: 0 <= closedUnits <= TotalUnits
    /// </summary>
    Task UpdateInventoryClosedUnitsAsync(
        int roomTypeId, 
        DateOnly startDate, 
        DateOnly endDateExclusive, 
        int closedUnits, 
        CancellationToken ct = default);
    
    /// <summary>
    /// Ensure inventory rows exist for a room type across a date range.
    /// Creates missing rows with default values (from room type DefaultTotalUnits).
    /// Used by seed service to maintain rolling window.
    /// </summary>
    Task EnsureInventoryExistsAsync(
        int roomTypeId, 
        DateOnly startDate, 
        int days, 
        CancellationToken ct = default);
}
