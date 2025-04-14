using Microsoft.EntityFrameworkCore;
using TravelBridge.API.Models.DB;

namespace TravelBridge.API.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set UTF8MB4 Charset for all tables (for Greek & Latin support)
            modelBuilder.UseCollation("utf8mb4_general_ci");

            // Apply default value to DateCreated for all derived entities dynamically
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseModel).IsAssignableFrom(entityType.ClrType)) // ✅ Apply only to BaseModel-derived entities
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("DateCreated")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP"); // ✅ Store UTC time
                }
            }

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
    }

 
}
