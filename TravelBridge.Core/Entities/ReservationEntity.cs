using System.Text;

namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Represents a hotel reservation in the domain.
    /// Contains business logic for reservation operations.
    /// </summary>
    public class ReservationEntity : EntityBase
    {
        public DateOnly CheckIn { get; private set; }
        public DateOnly CheckOut { get; private set; }
        public string? HotelCode { get; private set; }
        public string? HotelName { get; private set; }
        public decimal TotalAmount { get; private set; }
        public int TotalRooms { get; private set; }
        public BookingStatus Status { get; private set; }
        public string? Party { get; private set; }
        public decimal RemainingAmount { get; private set; }
        public string? CheckInTime { get; private set; }
        public string? CheckOutTime { get; private set; }
        public string? CouponCode { get; private set; }

        // For EF Core
        protected ReservationEntity() { }

        public ReservationEntity(
            DateOnly checkIn,
            DateOnly checkOut,
            string hotelCode,
            string hotelName,
            decimal totalAmount,
            int totalRooms)
        {
            if (checkOut <= checkIn)
                throw new ArgumentException("Check-out must be after check-in");
            if (totalAmount < 0)
                throw new ArgumentException("Total amount cannot be negative");
            if (totalRooms <= 0)
                throw new ArgumentException("Must have at least one room");

            CheckIn = checkIn;
            CheckOut = checkOut;
            HotelCode = hotelCode;
            HotelName = hotelName;
            TotalAmount = totalAmount;
            TotalRooms = totalRooms;
            Status = BookingStatus.New;
            RemainingAmount = totalAmount;
        }

        /// <summary>
        /// Number of nights for this reservation.
        /// </summary>
        public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

        /// <summary>
        /// Amount already paid.
        /// </summary>
        public decimal PaidAmount => TotalAmount - RemainingAmount;

        /// <summary>
        /// Whether the reservation is fully paid.
        /// </summary>
        public bool IsFullyPaid => RemainingAmount <= 0;

        /// <summary>
        /// Whether the reservation can be cancelled.
        /// </summary>
        public bool CanBeCancelled => Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;

        /// <summary>
        /// Whether the reservation is in a terminal state.
        /// </summary>
        public bool IsTerminal => Status == BookingStatus.Confirmed || 
                                   Status == BookingStatus.Cancelled || 
                                   Status == BookingStatus.Error;

        /// <summary>
        /// Marks the reservation as pending (payment initiated).
        /// </summary>
        public void MarkAsPending()
        {
            if (Status != BookingStatus.New)
                throw new InvalidOperationException($"Cannot mark as pending from status {Status}");
            Status = BookingStatus.Pending;
        }

        /// <summary>
        /// Marks the reservation as running (booking in progress).
        /// </summary>
        public void MarkAsRunning()
        {
            if (Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot mark as running from status {Status}");
            Status = BookingStatus.Running;
        }

        /// <summary>
        /// Confirms the reservation.
        /// </summary>
        public void Confirm()
        {
            if (Status != BookingStatus.Running && Status != BookingStatus.Pending)
                throw new InvalidOperationException($"Cannot confirm reservation from status {Status}");
            Status = BookingStatus.Confirmed;
        }

        /// <summary>
        /// Cancels the reservation.
        /// </summary>
        public void Cancel()
        {
            if (!CanBeCancelled)
                throw new InvalidOperationException($"Cannot cancel reservation in status {Status}");
            Status = BookingStatus.Cancelled;
        }

        /// <summary>
        /// Marks the reservation as errored.
        /// </summary>
        public void MarkAsError()
        {
            Status = BookingStatus.Error;
        }

        /// <summary>
        /// Records a payment against this reservation.
        /// </summary>
        public void RecordPayment(decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("Payment amount must be positive");
            if (amount > RemainingAmount)
                throw new ArgumentException("Payment exceeds remaining amount");

            RemainingAmount -= amount;
        }

        /// <summary>
        /// Sets the check-in and check-out times.
        /// </summary>
        public void SetOperationTimes(string checkInTime, string checkOutTime)
        {
            CheckInTime = checkInTime;
            CheckOutTime = checkOutTime;
        }

        /// <summary>
        /// Applies a coupon code to this reservation.
        /// </summary>
        public void ApplyCoupon(string couponCode)
        {
            if (Status != BookingStatus.New && Status != BookingStatus.Pending)
                throw new InvalidOperationException("Cannot apply coupon after booking has started");
            CouponCode = couponCode;
        }
    }
}
