using Microsoft.EntityFrameworkCore;
using TravelBridge.API.Models.DB;

namespace TravelBridge.API.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Existing DbSets
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<ReservationRate> ReservationRates { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Coupon> Coupons { get; set; }

        // Owned Inventory DbSets (Phase 3)
        public DbSet<OwnedHotel> OwnedHotels { get; set; }
        public DbSet<OwnedRoomType> OwnedRoomTypes { get; set; }
        public DbSet<OwnedInventoryDaily> OwnedInventoryDaily { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set UTF8MB4 Charset for all tables (for Greek & Latin support)
            modelBuilder.UseCollation("utf8mb4_general_ci");

            // Apply default value to DateCreated for all derived entities dynamically
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("DateCreated")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");
                }
            }

            // Existing relationship configurations
            ConfigureExistingRelationships(modelBuilder);

            // Owned Inventory configuration (Phase 3)
            ConfigureOwnedInventory(modelBuilder);
        }

        private void ConfigureExistingRelationships(ModelBuilder modelBuilder)
        {
            // Customer ⇄ Payment (One-to-Many)
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Payments)
                .WithOne(p => p.Customer)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Customer ⇄ Reservation (One-to-Many)
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Reservations)
                .WithOne(r => r.Customer)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation ⇄ Payment (One-to-Many)
            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Payments)
                .WithOne(p => p.Reservation)
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Reservation ⇄ ReservationRate (One-to-Many)
            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Rates)
                .WithOne(rr => rr.Reservation)
                .HasForeignKey(rr => rr.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        private void ConfigureOwnedInventory(ModelBuilder modelBuilder)
        {
            // OwnedHotel configuration
            modelBuilder.Entity<OwnedHotel>(entity =>
            {
                entity.HasKey(h => h.Id);

                // Unique index on Code (composite ID component)
                entity.HasIndex(h => h.Code)
                    .IsUnique()
                    .HasDatabaseName("IX_OwnedHotel_Code");

                // Index for active hotel queries
                entity.HasIndex(h => h.IsActive)
                    .HasDatabaseName("IX_OwnedHotel_IsActive");

                // Composite index for bounding box queries (geo search)
                // Note: This is a B-tree index, not a spatial index. For true geospatial
                // queries, consider adding a POINT column + SPATIAL index in Phase 6+.
                entity.HasIndex(h => new { h.Latitude, h.Longitude })
                    .HasDatabaseName("IX_OwnedHotel_Location");

                // Relationship: Hotel → RoomTypes (one-to-many, cascade delete)
                entity.HasMany(h => h.RoomTypes)
                    .WithOne(rt => rt.Hotel)
                    .HasForeignKey(rt => rt.HotelId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OwnedRoomType configuration
            modelBuilder.Entity<OwnedRoomType>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                // Index for hotel queries
                entity.HasIndex(rt => rt.HotelId)
                    .HasDatabaseName("IX_OwnedRoomType_HotelId");

                // Unique composite index for (HotelId, Code)
                entity.HasIndex(rt => new { rt.HotelId, rt.Code })
                    .IsUnique()
                    .HasDatabaseName("IX_OwnedRoomType_HotelId_Code");

                // Index for active room type queries
                entity.HasIndex(rt => rt.IsActive)
                    .HasDatabaseName("IX_OwnedRoomType_IsActive");

                // Relationship: RoomType → InventoryDays (one-to-many, cascade delete)
                entity.HasMany(rt => rt.InventoryDays)
                    .WithOne(inv => inv.RoomType)
                    .HasForeignKey(inv => inv.RoomTypeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // OwnedInventoryDaily configuration
            modelBuilder.Entity<OwnedInventoryDaily>(entity =>
            {
                // Composite primary key (RoomTypeId, Date)
                entity.HasKey(inv => new { inv.RoomTypeId, inv.Date })
                    .HasName("PK_OwnedInventoryDaily");

                // Explicit column type for DateOnly (MySQL DATE)
                entity.Property(inv => inv.Date)
                    .HasColumnType("DATE");

                // Explicit column type for DateTime (MySQL DATETIME(6) for microsecond precision)
                entity.Property(inv => inv.LastModifiedUtc)
                    .HasColumnType("DATETIME(6)");

                // Index on Date for date range queries
                entity.HasIndex(inv => inv.Date)
                    .HasDatabaseName("IX_OwnedInventoryDaily_Date");

                // Ignore computed property (not stored in DB)
                entity.Ignore(inv => inv.AvailableUnits);

                // Validation: ensure counters don't exceed capacity
                // Note: MySQL 8+ supports CHECK constraints. Older versions may ignore.
                // Always validate in code (repository) as primary defense.
                entity.ToTable(t => t.HasCheckConstraint(
                    "CK_OwnedInventoryDaily_Counters",
                    "ClosedUnits >= 0 AND HeldUnits >= 0 AND ConfirmedUnits >= 0 AND (ClosedUnits + HeldUnits + ConfirmedUnits) <= TotalUnits"));
            });
        }
    }
}
