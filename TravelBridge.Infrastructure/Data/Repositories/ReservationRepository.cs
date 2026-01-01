using Microsoft.EntityFrameworkCore;
using TravelBridge.Core.Entities;
using TravelBridge.Core.Interfaces;
using TravelBridge.Infrastructure.Data.Models;

namespace TravelBridge.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Repository for reservation-related database operations.
    /// </summary>
    public class ReservationRepository : IReservationRepository
    {
        private readonly AppDbContext _db;

        public ReservationRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Core.Entities.ReservationEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _db.Reservations
                .Include(r => r.Customer)
                .Include(r => r.Rates).ThenInclude(r => r.SearchParty)
                .Include(r => r.Payments)
                .Include(r => r.PartialPayment).ThenInclude(p => p!.NextPayments)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (entity == null) return null;

            // Map to Core entity
            return MapToCore(entity);
        }

        public async Task<bool> UpdateStatusAsync(int reservationId, BookingStatus newStatus, BookingStatus? expectedCurrentStatus = null, CancellationToken cancellationToken = default)
        {
            var reservation = await _db.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId, cancellationToken);
            if (reservation == null) return false;

            if (expectedCurrentStatus.HasValue && reservation.BookingStatus != expectedCurrentStatus.Value)
                return false;

            reservation.BookingStatus = newStatus;
            reservation.DateFinalized = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<CouponInfo?> GetValidCouponAsync(string couponCode, CancellationToken cancellationToken = default)
        {
            var coupon = await _db.Coupons
                .FirstOrDefaultAsync(c => c.Code == couponCode && c.Expiration > DateTime.UtcNow, cancellationToken);

            if (coupon == null) return null;

            return new CouponInfo
            {
                Code = coupon.Code,
                Type = coupon.CouponType,
                Percentage = coupon.Percentage,
                Amount = coupon.Amount,
                Expiration = coupon.Expiration
            };
        }

        /// <summary>
        /// Gets a reservation by payment order code.
        /// </summary>
        public async Task<Models.ReservationEntity?> GetByPaymentOrderCodeAsync(string orderCode, CancellationToken cancellationToken = default)
        {
            return await _db.Reservations
                .Where(r => r.Payments.Any(p => p.OrderCode == orderCode))
                .Include(r => r.Customer)
                .Include(r => r.Rates).ThenInclude(r => r.SearchParty)
                .Include(r => r.Payments)
                .Include(r => r.PartialPayment).ThenInclude(p => p!.NextPayments)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Updates payment status to success.
        /// </summary>
        public async Task<bool> UpdatePaymentSuccessAsync(string orderCode, string transactionId, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.OrderCode == orderCode, cancellationToken);
            if (payment?.PaymentStatus != PaymentStatus.Pending)
                return false;

            payment.PaymentStatus = PaymentStatus.Success;
            payment.DateFinalized = DateTime.Now;
            payment.TransactionId = transactionId;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Updates payment status to failed.
        /// </summary>
        public async Task<bool> UpdatePaymentFailedAsync(int paymentId, CancellationToken cancellationToken = default)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
            if (payment?.PaymentStatus != PaymentStatus.Pending)
                return false;

            payment.PaymentStatus = PaymentStatus.Failed;
            payment.DateFinalized = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Updates reservation rate status.
        /// </summary>
        public async Task<bool> UpdateRateStatusAsync(int rateId, BookingStatus newStatus, BookingStatus expectedStatus, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ReservationRates.FirstOrDefaultAsync(r => r.Id == rateId, cancellationToken);
            if (rate?.BookingStatus != expectedStatus)
                return false;

            rate.BookingStatus = newStatus;
            rate.DateFinalized = DateTime.Now;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        /// <summary>
        /// Updates reservation rate status to confirmed with provider reservation ID.
        /// </summary>
        public async Task<bool> UpdateRateConfirmedAsync(int rateId, int providerResId, CancellationToken cancellationToken = default)
        {
            var rate = await _db.ReservationRates.FirstOrDefaultAsync(r => r.Id == rateId, cancellationToken);
            if (rate?.BookingStatus != BookingStatus.Running)
                return false;

            rate.BookingStatus = BookingStatus.Confirmed;
            rate.DateFinalized = DateTime.Now;
            rate.ProviderResId = providerResId;
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static Core.Entities.ReservationEntity MapToCore(Models.ReservationEntity entity)
        {
            // Create a minimal Core entity for interface compatibility
            // Full mapping would be more complex
            return new Core.Entities.ReservationEntity(
                DateOnly.FromDateTime(entity.CheckIn.ToDateTime(TimeOnly.MinValue)),
                DateOnly.FromDateTime(entity.CheckOut.ToDateTime(TimeOnly.MinValue)),
                entity.HotelCode ?? "",
                entity.HotelName ?? "",
                entity.TotalAmount,
                entity.TotalRooms
            );
        }
    }
}
