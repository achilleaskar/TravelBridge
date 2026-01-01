using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelBridge.Infrastructure.Data.Models;

namespace TravelBridge.Infrastructure.Data.Configuration
{
    /// <summary>
    /// EF Core configuration for CustomerEntity.
    /// </summary>
    public class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
    {
        public void Configure(EntityTypeBuilder<CustomerEntity> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.LastName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.Email)
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(c => c.Tel)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(c => c.CountryCode)
                .HasColumnType("CHAR(2)");

            builder.Property(c => c.Notes)
                .IsRequired();

            builder.Property(c => c.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    /// <summary>
    /// EF Core configuration for ReservationEntity.
    /// </summary>
    public class ReservationConfiguration : IEntityTypeConfiguration<ReservationEntity>
    {
        public void Configure(EntityTypeBuilder<ReservationEntity> builder)
        {
            builder.ToTable("Reservations");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.HotelCode)
                .HasMaxLength(50);

            builder.Property(r => r.HotelName)
                .HasMaxLength(70);

            builder.Property(r => r.TotalAmount)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(r => r.TotalRooms)
                .HasColumnType("TINYINT UNSIGNED");

            builder.Property(r => r.Party)
                .HasMaxLength(150);

            builder.Property(r => r.RemainingAmount)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(r => r.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            builder.HasOne(r => r.Customer)
                .WithMany(c => c.Reservations)
                .HasForeignKey(r => r.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(r => r.Rates)
                .WithOne(rr => rr.Reservation)
                .HasForeignKey(rr => rr.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.Payments)
                .WithOne(p => p.Reservation)
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    /// <summary>
    /// EF Core configuration for PaymentEntity.
    /// </summary>
    public class PaymentConfiguration : IEntityTypeConfiguration<PaymentEntity>
    {
        public void Configure(EntityTypeBuilder<PaymentEntity> builder)
        {
            builder.ToTable("Payments");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(p => p.TransactionId)
                .HasMaxLength(50);

            builder.Property(p => p.OrderCode)
                .HasMaxLength(50);

            builder.Property(p => p.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationships
            builder.HasOne(p => p.Customer)
                .WithMany(c => c.Payments)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    /// <summary>
    /// EF Core configuration for ReservationRateEntity.
    /// </summary>
    public class ReservationRateConfiguration : IEntityTypeConfiguration<ReservationRateEntity>
    {
        public void Configure(EntityTypeBuilder<ReservationRateEntity> builder)
        {
            builder.ToTable("ReservationRate");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.HotelCode)
                .HasMaxLength(50);

            builder.Property(r => r.RateId)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(r => r.Price)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(r => r.NetPrice)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(r => r.Quantity)
                .HasColumnType("TINYINT UNSIGNED");

            builder.Property(r => r.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Relationship with SearchParty
            builder.HasOne(r => r.SearchParty)
                .WithMany()
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// EF Core configuration for PartyItemEntity.
    /// </summary>
    public class PartyItemConfiguration : IEntityTypeConfiguration<PartyItemEntity>
    {
        public void Configure(EntityTypeBuilder<PartyItemEntity> builder)
        {
            builder.ToTable("PartyItemDB");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Children)
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(p => p.Party)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(p => p.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    /// <summary>
    /// EF Core configuration for CouponEntity.
    /// </summary>
    public class CouponConfiguration : IEntityTypeConfiguration<CouponEntity>
    {
        public void Configure(EntityTypeBuilder<CouponEntity> builder)
        {
            builder.ToTable("Coupons");

            builder.HasKey(c => c.Id);

            builder.Property(c => c.Code)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(c => c.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    /// <summary>
    /// EF Core configuration for PartialPaymentEntity.
    /// </summary>
    public class PartialPaymentConfiguration : IEntityTypeConfiguration<PartialPaymentEntity>
    {
        public void Configure(EntityTypeBuilder<PartialPaymentEntity> builder)
        {
            builder.ToTable("PartialPaymentDB");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.PrepayAmount)
                .HasColumnType("DECIMAL(10,2)");

            builder.Property(p => p.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.HasMany(p => p.NextPayments)
                .WithOne(n => n.PartialPayment)
                .HasForeignKey(n => n.PartialPaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// EF Core configuration for NextPaymentEntity.
    /// </summary>
    public class NextPaymentConfiguration : IEntityTypeConfiguration<NextPaymentEntity>
    {
        public void Configure(EntityTypeBuilder<NextPaymentEntity> builder)
        {
            builder.ToTable("NextPaymentDB");

            builder.HasKey(n => n.Id);

            builder.Property(n => n.DateCreated)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}
