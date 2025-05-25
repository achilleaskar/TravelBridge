using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace TravelBridge.API.OnlineModels;

public partial class TravelBridgeDBContext : DbContext
{
    public TravelBridgeDBContext()
    {
    }

    public TravelBridgeDBContext(DbContextOptions<TravelBridgeDBContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<EfmigrationsHistory> EfmigrationsHistories { get; set; }

    public virtual DbSet<NextPaymentDb> NextPaymentDbs { get; set; }

    public virtual DbSet<PartialPaymentDb> PartialPaymentDbs { get; set; }

    public virtual DbSet<PartyItemDb> PartyItemDbs { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Reservation> Reservations { get; set; }

    public virtual DbSet<ReservationRate> ReservationRates { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=147.93.57.71;database=srv6_initialDB;user=srv6_admin;password=Tr6981001676!", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.11.10-mariadb"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_general_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(2)
                .IsFixedLength();
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.Email).HasMaxLength(80);
            entity.Property(e => e.FirstName).HasMaxLength(50);
            entity.Property(e => e.LastName).HasMaxLength(50);
            entity.Property(e => e.Tel).HasMaxLength(20);
        });

        modelBuilder.Entity<EfmigrationsHistory>(entity =>
        {
            entity.HasKey(e => e.MigrationId).HasName("PRIMARY");

            entity.ToTable("__EFMigrationsHistory");

            entity.Property(e => e.MigrationId).HasMaxLength(150);
            entity.Property(e => e.ProductVersion).HasMaxLength(32);
        });

        modelBuilder.Entity<NextPaymentDb>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("NextPaymentDB");

            entity.HasIndex(e => e.PartialPaymentDbid, "IX_NextPaymentDB_PartialPaymentDBId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DueDate).HasMaxLength(6);
            entity.Property(e => e.PartialPaymentDbid)
                .HasColumnType("int(11)")
                .HasColumnName("PartialPaymentDBId");

            entity.HasOne(d => d.PartialPaymentDb).WithMany(p => p.NextPaymentDbs).HasForeignKey(d => d.PartialPaymentDbid);
        });

        modelBuilder.Entity<PartialPaymentDb>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("PartialPaymentDB");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.PrepayAmount)
                .HasPrecision(10, 2)
                .HasColumnName("prepayAmount");
        });

        modelBuilder.Entity<PartyItemDb>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("PartyItemDB");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Adults).HasColumnType("int(11)");
            entity.Property(e => e.Children).HasMaxLength(20);
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.Party).HasMaxLength(100);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.CustomerId, "IX_Payments_CustomerId");

            entity.HasIndex(e => e.ReservationId, "IX_Payments_ReservationId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.Amount).HasPrecision(10, 2);
            entity.Property(e => e.CustomerId).HasColumnType("int(11)");
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DateFinalized).HasMaxLength(6);
            entity.Property(e => e.OrderCode).HasMaxLength(50);
            entity.Property(e => e.PaymentProvider).HasColumnType("int(11)");
            entity.Property(e => e.PaymentStatus).HasColumnType("int(11)");
            entity.Property(e => e.ReservationId).HasColumnType("int(11)");
            entity.Property(e => e.TransactionId).HasMaxLength(50);

            entity.HasOne(d => d.Customer).WithMany(p => p.Payments).HasForeignKey(d => d.CustomerId);

            entity.HasOne(d => d.Reservation).WithMany(p => p.Payments).HasForeignKey(d => d.ReservationId);
        });

        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.CustomerId, "IX_Reservations_CustomerId");

            entity.HasIndex(e => e.PartialPaymentId, "IX_Reservations_PartialPaymentId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.BookingStatus).HasColumnType("int(11)");
            entity.Property(e => e.CustomerId).HasColumnType("int(11)");
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DateFinalized)
                .HasMaxLength(6)
                .HasDefaultValueSql("'0001-01-01 00:00:00.000000'");
            entity.Property(e => e.HotelCode).HasMaxLength(50);
            entity.Property(e => e.HotelName).HasMaxLength(70);
            entity.Property(e => e.PartialPaymentId).HasColumnType("int(11)");
            entity.Property(e => e.Party).HasMaxLength(150);
            entity.Property(e => e.RemainingAmount).HasPrecision(10, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(10, 2);
            entity.Property(e => e.TotalRooms).HasColumnType("tinyint(3) unsigned");

            entity.HasOne(d => d.Customer).WithMany(p => p.Reservations).HasForeignKey(d => d.CustomerId);

            entity.HasOne(d => d.PartialPayment).WithMany(p => p.Reservations).HasForeignKey(d => d.PartialPaymentId);
        });

        modelBuilder.Entity<ReservationRate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.HasIndex(e => e.ReservationId, "IX_ReservationRates_ReservationId");

            entity.HasIndex(e => e.SearchPartyId, "IX_ReservationRates_SearchPartyId");

            entity.Property(e => e.Id).HasColumnType("int(11)");
            entity.Property(e => e.BookingStatus).HasColumnType("int(11)");
            entity.Property(e => e.DateCreated)
                .HasMaxLength(6)
                .HasDefaultValueSql("current_timestamp(6)");
            entity.Property(e => e.DateFinalized)
                .HasMaxLength(6)
                .HasDefaultValueSql("'0001-01-01 00:00:00.000000'");
            entity.Property(e => e.HotelCode).HasMaxLength(50);
            entity.Property(e => e.NetPrice).HasPrecision(10, 2);
            entity.Property(e => e.Price).HasPrecision(10, 2);
            entity.Property(e => e.Provider).HasColumnType("int(11)");
            entity.Property(e => e.ProviderResId).HasColumnType("int(11)");
            entity.Property(e => e.Quantity).HasColumnType("tinyint(3) unsigned");
            entity.Property(e => e.RateId).HasMaxLength(20);
            entity.Property(e => e.ReservationId).HasColumnType("int(11)");
            entity.Property(e => e.SearchPartyId).HasColumnType("int(11)");

            entity.HasOne(d => d.Reservation).WithMany(p => p.ReservationRates)
                .HasForeignKey(d => d.ReservationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.SearchParty).WithMany(p => p.ReservationRates).HasForeignKey(d => d.SearchPartyId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
