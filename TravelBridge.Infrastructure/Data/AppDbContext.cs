using Microsoft.EntityFrameworkCore;
using TravelBridge.Infrastructure.Data.Configuration;
using TravelBridge.Infrastructure.Data.Models;

namespace TravelBridge.Infrastructure.Data
{
    /// <summary>
    /// Application database context.
    /// </summary>
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<CustomerEntity> Customers { get; set; }
        public DbSet<ReservationEntity> Reservations { get; set; }
        public DbSet<ReservationRateEntity> ReservationRates { get; set; }
        public DbSet<PaymentEntity> Payments { get; set; }
        public DbSet<CouponEntity> Coupons { get; set; }
        public DbSet<PartyItemEntity> PartyItems { get; set; }
        public DbSet<PartialPaymentEntity> PartialPayments { get; set; }
        public DbSet<NextPaymentEntity> NextPayments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set UTF8MB4 Charset for all tables (for Greek & Latin support)
            modelBuilder.UseCollation("utf8mb4_general_ci");

            // Apply configurations
            modelBuilder.ApplyConfiguration(new CustomerConfiguration());
            modelBuilder.ApplyConfiguration(new ReservationConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new ReservationRateConfiguration());
            modelBuilder.ApplyConfiguration(new PartyItemConfiguration());
            modelBuilder.ApplyConfiguration(new CouponConfiguration());
            modelBuilder.ApplyConfiguration(new PartialPaymentConfiguration());
            modelBuilder.ApplyConfiguration(new NextPaymentConfiguration());
        }
    }
}
