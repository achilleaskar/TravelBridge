using Microsoft.AspNetCore.Mvc;
using TravelBridge.Providers.Abstractions.Store;

namespace TravelBridge.API.Endpoints;

/// <summary>
/// Admin endpoints for managing owned hotel inventory.
/// Provides capacity management and stop-sell functionality.
/// </summary>
public static class OwnedAdminEndpoint
{
    public static void MapEndpoints(IEndpointRouteBuilder app)
    {
        // SECURITY: Admin endpoints are restricted to Development environment only in Phase 3
        // For production use, implement proper authentication (JWT, API Key, etc.)
        var env = app.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
        var logger = app.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        if (!env.IsDevelopment())
        {
            logger.LogWarning("OwnedAdminEndpoint: Admin endpoints NOT registered in {Environment} environment. Development environment required.", env.EnvironmentName);
            return; // Don't register admin endpoints outside of Development
        }

        logger.LogInformation("OwnedAdminEndpoint: Registering admin endpoints (Development environment only)");

        var adminGroup = app.MapGroup("/admin/owned/inventory");
        // Note: No RequireAuthorization() in Phase 3 since we're dev-only
        // Phase 4+ should add proper auth: .RequireAuthorization("AdminPolicy")

        adminGroup.MapPut("/roomtype/{roomTypeId:int}/capacity", SetCapacity)
            .WithName("SetOwnedCapacity")
            .WithSummary("Update total capacity for a room type across a date range");

        adminGroup.MapPut("/roomtype/{roomTypeId:int}/closed", SetClosedUnits)
            .WithName("SetOwnedClosedUnits")
            .WithSummary("Update closed units (stop-sell) for a room type across a date range");

        adminGroup.MapPut("/hotel/{hotelCode}/close", CloseHotel)
            .WithName("CloseOwnedHotel")
            .WithSummary("Close an entire hotel (stop-sell all room types) for a date range");

        adminGroup.MapGet("/roomtype/{roomTypeId:int}", GetInventory)
            .WithName("GetOwnedInventory")
            .WithSummary("Get inventory details for a room type across a date range");

        adminGroup.MapPost("/roomtype/{roomTypeId:int}/seed", SeedInventory)
            .WithName("SeedOwnedInventory")
            .WithSummary("Manually trigger inventory seeding for a room type");
    }

    /// <summary>
    /// Update total capacity for a room type.
    /// </summary>
    private static async Task<IResult> SetCapacity(
        int roomTypeId,
        [FromBody] SetCapacityRequest request,
        IOwnedInventoryStore store,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("SetCapacity started for RoomTypeId: {RoomTypeId}, StartDate: {StartDate}, EndDate: {EndDate}, TotalUnits: {TotalUnits}",
            roomTypeId, request.StartDate, request.EndDateExclusive, request.TotalUnits);

        try
        {
            // Validate input
            if (request.StartDate >= request.EndDateExclusive)
            {
                return Results.BadRequest(new { error = "EndDateExclusive must be after StartDate" });
            }

            if (request.TotalUnits < 0)
            {
                return Results.BadRequest(new { error = "TotalUnits cannot be negative" });
            }

            // Verify room type exists
            var roomType = await store.GetRoomTypeByIdAsync(roomTypeId, ct);
            if (roomType == null)
            {
                return Results.NotFound(new { error = $"Room type {roomTypeId} not found" });
            }

            // Update capacity
            await store.UpdateInventoryCapacityAsync(
                roomTypeId,
                request.StartDate,
                request.EndDateExclusive,
                request.TotalUnits,
                ct);

            logger.LogInformation("SetCapacity completed for RoomTypeId: {RoomTypeId}", roomTypeId);

            return Results.Ok(new
            {
                success = true,
                message = $"Capacity updated to {request.TotalUnits} units for {request.EndDateExclusive.DayNumber - request.StartDate.DayNumber} days"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "SetCapacity failed validation for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SetCapacity failed for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.Problem("An error occurred while updating capacity");
        }
    }

    /// <summary>
    /// Update closed units (stop-sell) for a room type.
    /// </summary>
    private static async Task<IResult> SetClosedUnits(
        int roomTypeId,
        [FromBody] SetClosedUnitsRequest request,
        IOwnedInventoryStore store,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("SetClosedUnits started for RoomTypeId: {RoomTypeId}, StartDate: {StartDate}, EndDate: {EndDate}, ClosedUnits: {ClosedUnits}",
            roomTypeId, request.StartDate, request.EndDateExclusive, request.ClosedUnits);

        try
        {
            // Validate input
            if (request.StartDate >= request.EndDateExclusive)
            {
                return Results.BadRequest(new { error = "EndDateExclusive must be after StartDate" });
            }

            if (request.ClosedUnits < 0)
            {
                return Results.BadRequest(new { error = "ClosedUnits cannot be negative" });
            }

            // Verify room type exists
            var roomType = await store.GetRoomTypeByIdAsync(roomTypeId, ct);
            if (roomType == null)
            {
                return Results.NotFound(new { error = $"Room type {roomTypeId} not found" });
            }

            // Update closed units
            await store.UpdateInventoryClosedUnitsAsync(
                roomTypeId,
                request.StartDate,
                request.EndDateExclusive,
                request.ClosedUnits,
                ct);

            logger.LogInformation("SetClosedUnits completed for RoomTypeId: {RoomTypeId}", roomTypeId);

            return Results.Ok(new
            {
                success = true,
                message = $"Closed units set to {request.ClosedUnits} for {request.EndDateExclusive.DayNumber - request.StartDate.DayNumber} days"
            });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "SetClosedUnits failed validation for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SetClosedUnits failed for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.Problem("An error occurred while updating closed units");
        }
    }

    /// <summary>
    /// Close an entire hotel (stop-sell all room types) for a date range.
    /// </summary>
    private static async Task<IResult> CloseHotel(
        string hotelCode,
        [FromBody] CloseDateRangeRequest request,
        IOwnedInventoryStore store,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("CloseHotel started for HotelCode: {HotelCode}, StartDate: {StartDate}, EndDate: {EndDate}",
            hotelCode, request.StartDate, request.EndDateExclusive);

        try
        {
            // Validate input
            if (request.StartDate >= request.EndDateExclusive)
            {
                return Results.BadRequest(new { error = "EndDateExclusive must be after StartDate" });
            }

            // Get hotel and its room types
            var hotel = await store.GetHotelByCodeAsync(hotelCode, ct);
            if (hotel == null)
            {
                return Results.NotFound(new { error = $"Hotel {hotelCode} not found" });
            }

            var roomTypes = await store.GetRoomTypesByHotelIdAsync(hotel.Id, activeOnly: false, ct);
            if (roomTypes.Count == 0)
            {
                return Results.BadRequest(new { error = "Hotel has no room types" });
            }

            // Close all room types by setting ClosedUnits = TotalUnits
            int updatedCount = 0;
            var errors = new List<string>();

            foreach (var roomType in roomTypes)
            {
                try
                {
                    // Get inventory for this room type to determine max capacity
                    var inventory = await store.GetInventoryAsync(
                        roomType.Id,
                        request.StartDate,
                        request.EndDateExclusive,
                        ct);

                    if (inventory.Count == 0)
                    {
                        // No inventory exists, seed it first
                        await store.EnsureInventoryExistsAsync(
                            roomType.Id,
                            request.StartDate,
                            request.EndDateExclusive.DayNumber - request.StartDate.DayNumber,
                            ct);

                        inventory = await store.GetInventoryAsync(
                            roomType.Id,
                            request.StartDate,
                            request.EndDateExclusive,
                            ct);
                    }

                    // For each date, set ClosedUnits = TotalUnits
                    var maxTotal = inventory.Max(i => i.TotalUnits);
                    
                    await store.UpdateInventoryClosedUnitsAsync(
                        roomType.Id,
                        request.StartDate,
                        request.EndDateExclusive,
                        maxTotal,
                        ct);

                    updatedCount++;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to close room type {RoomTypeId} in hotel {HotelCode}",
                        roomType.Id, hotelCode);
                    errors.Add($"Room type {roomType.Code}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
            {
                return Results.Ok(new
                {
                    success = updatedCount > 0,
                    message = $"Partially closed hotel: {updatedCount}/{roomTypes.Count} room types updated",
                    errors
                });
            }

            logger.LogInformation("CloseHotel completed for HotelCode: {HotelCode}, RoomTypes: {Count}",
                hotelCode, updatedCount);

            return Results.Ok(new
            {
                success = true,
                message = $"Hotel closed: {updatedCount} room types stop-sold for {request.EndDateExclusive.DayNumber - request.StartDate.DayNumber} days"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CloseHotel failed for HotelCode: {HotelCode}", hotelCode);
            return Results.Problem("An error occurred while closing the hotel");
        }
    }

    /// <summary>
    /// Get inventory details for a room type across a date range.
    /// </summary>
    private static async Task<IResult> GetInventory(
        int roomTypeId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        IOwnedInventoryStore store,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("GetInventory started for RoomTypeId: {RoomTypeId}, StartDate: {StartDate}, EndDate: {EndDate}",
            roomTypeId, startDate, endDate);

        try
        {
            // Default to next 30 days if not specified
            var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var end = endDate ?? start.AddDays(30);

            if (start >= end)
            {
                return Results.BadRequest(new { error = "EndDate must be after StartDate" });
            }

            // Verify room type exists
            var roomType = await store.GetRoomTypeByIdAsync(roomTypeId, ct);
            if (roomType == null)
            {
                return Results.NotFound(new { error = $"Room type {roomTypeId} not found" });
            }

            // Get inventory
            var inventory = await store.GetInventoryAsync(roomTypeId, start, end, ct);

            logger.LogInformation("GetInventory completed for RoomTypeId: {RoomTypeId}, Rows: {Count}",
                roomTypeId, inventory.Count);

            return Results.Ok(new
            {
                roomType = new
                {
                    roomType.Id,
                    roomType.Code,
                    roomType.Name,
                    roomType.DefaultTotalUnits,
                    roomType.BasePricePerNight
                },
                startDate = start,
                endDate = end,
                inventory = inventory.Select(inv => new
                {
                    inv.Date,
                    inv.TotalUnits,
                    inv.ClosedUnits,
                    inv.HeldUnits,
                    inv.ConfirmedUnits,
                    inv.AvailableUnits,
                    PricePerNight = inv.PricePerNight ?? roomType.BasePricePerNight
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetInventory failed for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.Problem("An error occurred while retrieving inventory");
        }
    }

    /// <summary>
    /// Manually trigger inventory seeding for a room type.
    /// Useful for testing or one-off data population.
    /// </summary>
    private static async Task<IResult> SeedInventory(
        int roomTypeId,
        [FromBody] SeedInventoryRequest request,
        IOwnedInventoryStore store,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        logger.LogInformation("SeedInventory started for RoomTypeId: {RoomTypeId}, StartDate: {StartDate}, Days: {Days}",
            roomTypeId, request.StartDate, request.Days);

        try
        {
            if (request.Days <= 0 || request.Days > 400)
            {
                return Results.BadRequest(new { error = "Days must be between 1 and 400" });
            }

            // Verify room type exists
            var roomType = await store.GetRoomTypeByIdAsync(roomTypeId, ct);
            if (roomType == null)
            {
                return Results.NotFound(new { error = $"Room type {roomTypeId} not found" });
            }

            // Seed inventory
            await store.EnsureInventoryExistsAsync(roomTypeId, request.StartDate, request.Days, ct);

            logger.LogInformation("SeedInventory completed for RoomTypeId: {RoomTypeId}", roomTypeId);

            return Results.Ok(new
            {
                success = true,
                message = $"Inventory seeded for {request.Days} days starting {request.StartDate}"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SeedInventory failed for RoomTypeId: {RoomTypeId}", roomTypeId);
            return Results.Problem("An error occurred while seeding inventory");
        }
    }

    // Request models
    public record SetCapacityRequest(DateOnly StartDate, DateOnly EndDateExclusive, int TotalUnits);
    public record SetClosedUnitsRequest(DateOnly StartDate, DateOnly EndDateExclusive, int ClosedUnits);
    public record CloseDateRangeRequest(DateOnly StartDate, DateOnly EndDateExclusive);
    public record SeedInventoryRequest(DateOnly StartDate, int Days);
}
