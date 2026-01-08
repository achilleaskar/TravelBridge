using Microsoft.EntityFrameworkCore;
using TravelBridge.API.DataBase;

namespace TravelBridge.API.Services;

/// <summary>
/// Background service that maintains a rolling window of inventory for all active owned room types.
/// Ensures inventory exists for today + 400 days.
/// Runs on startup and daily at 2 AM UTC.
/// </summary>
public class InventorySeedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventorySeedService> _logger;
    private readonly TimeSpan _dailyRunTime = new(2, 0, 0); // 2 AM UTC
    private const int InventoryWindowDays = 400;

    public InventorySeedService(IServiceProvider serviceProvider, ILogger<InventorySeedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InventorySeedService starting");

        // Run immediately on startup - with error handling to prevent app crash
        try
        {
            await SeedInventoryAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InventorySeedService: Initial seed failed on startup. Will retry on next scheduled run at 2 AM UTC.");
            // Don't crash the app - continue to daily scheduling
        }

        // Then run daily
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelayUntilNextRun();
            _logger.LogInformation("InventorySeedService: Next run in {Hours} hours", delay.TotalHours);

            try
            {
                await Task.Delay(delay, stoppingToken);
                await SeedInventoryAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("InventorySeedService stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "InventorySeedService encountered an error during scheduled run");
                // Continue running despite errors
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retry
            }
        }
    }

    /// <summary>
    /// Seed inventory for all active owned room types.
    /// </summary>
    private async Task SeedInventoryAsync(CancellationToken ct)
    {
        _logger.LogInformation("InventorySeedService: Starting inventory seed");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Get all active room types
            var roomTypes = await context.OwnedRoomTypes
                .AsNoTracking()
                .Where(rt => rt.IsActive)
                .Select(rt => new { rt.Id, rt.Code, rt.DefaultTotalUnits })
                .ToListAsync(ct);

            if (roomTypes.Count == 0)
            {
                _logger.LogInformation("InventorySeedService: No active room types found, skipping seed");
                return;
            }

            _logger.LogInformation("InventorySeedService: Seeding inventory for {Count} room types", roomTypes.Count);

            var startDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var endDate = startDate.AddDays(InventoryWindowDays);

            int totalCreated = 0;
            int roomTypesProcessed = 0;

            foreach (var roomType in roomTypes)
            {
                try
                {
                    var created = await SeedRoomTypeInventoryAsync(
                        context,
                        roomType.Id,
                        roomType.Code,
                        roomType.DefaultTotalUnits,
                        startDate,
                        endDate,
                        ct);

                    totalCreated += created;
                    roomTypesProcessed++;

                    if (created > 0)
                    {
                        _logger.LogDebug("InventorySeedService: Created {Count} rows for room type {Code}",
                            created, roomType.Code);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "InventorySeedService: Failed to seed room type {RoomTypeId} ({Code})",
                        roomType.Id, roomType.Code);
                    // Continue with other room types
                }
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "InventorySeedService: Completed in {ElapsedMs}ms. Processed {Processed}/{Total} room types, created {Created} inventory rows",
                stopwatch.ElapsedMilliseconds, roomTypesProcessed, roomTypes.Count, totalCreated);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "InventorySeedService: Seed operation failed after {ElapsedMs}ms",
                stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Seed inventory for a single room type.
    /// Creates missing rows with default values.
    /// </summary>
    private async Task<int> SeedRoomTypeInventoryAsync(
        AppDbContext context,
        int roomTypeId,
        string roomTypeCode,
        int defaultTotalUnits,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        // Get existing inventory dates
        var existingDates = await context.OwnedInventoryDaily
            .Where(inv => inv.RoomTypeId == roomTypeId &&
                         inv.Date >= startDate &&
                         inv.Date < endDate)
            .Select(inv => inv.Date)
            .ToListAsync(ct);

        var existingDatesSet = existingDates.ToHashSet();

        // Create missing rows
        var missingRows = new List<Models.DB.OwnedInventoryDaily>();
        var currentDate = startDate;

        while (currentDate < endDate)
        {
            if (!existingDatesSet.Contains(currentDate))
            {
                missingRows.Add(new Models.DB.OwnedInventoryDaily
                {
                    RoomTypeId = roomTypeId,
                    Date = currentDate,
                    TotalUnits = defaultTotalUnits,
                    ClosedUnits = 0,
                    HeldUnits = 0,
                    ConfirmedUnits = 0,
                    PricePerNight = null, // Use room type base price
                    LastModifiedUtc = DateTime.UtcNow
                });
            }

            currentDate = currentDate.AddDays(1);
        }

        if (missingRows.Count > 0)
        {
            await context.OwnedInventoryDaily.AddRangeAsync(missingRows, ct);
            await context.SaveChangesAsync(ct);
        }

        return missingRows.Count;
    }

    /// <summary>
    /// Calculate delay until next scheduled run (2 AM UTC).
    /// </summary>
    private TimeSpan CalculateDelayUntilNextRun()
    {
        var now = DateTime.UtcNow;
        var nextRun = now.Date.Add(_dailyRunTime);

        // If we've already passed 2 AM today, schedule for tomorrow
        if (now.TimeOfDay >= _dailyRunTime)
        {
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - now;

        // Safety check: minimum 1 minute delay
        if (delay < TimeSpan.FromMinutes(1))
        {
            delay = TimeSpan.FromMinutes(1);
        }

        return delay;
    }
}
