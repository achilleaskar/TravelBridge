using TravelBridge.Core.Entities;

namespace TravelBridge.Core.Interfaces
{
    /// <summary>
    /// Repository interface for reservation operations.
    /// Implementation in Infrastructure layer.
    /// </summary>
    public interface IReservationRepository
    {
        /// <summary>
        /// Gets a reservation by its ID.
        /// </summary>
        Task<ReservationEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the status of a reservation.
        /// </summary>
        Task<bool> UpdateStatusAsync(int reservationId, BookingStatus newStatus, BookingStatus? expectedCurrentStatus = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a valid coupon by code.
        /// </summary>
        Task<CouponInfo?> GetValidCouponAsync(string couponCode, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Coupon information returned from repository.
    /// </summary>
    public class CouponInfo
    {
        public required string Code { get; init; }
        public required CouponType Type { get; init; }
        public decimal Percentage { get; init; }
        public decimal Amount { get; init; }
        public DateTime Expiration { get; init; }
    }
}
