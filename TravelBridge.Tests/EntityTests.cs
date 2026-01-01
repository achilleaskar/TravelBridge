using TravelBridge.Core.Entities;

namespace TravelBridge.Tests
{
    /// <summary>
    /// Unit tests for Core domain entities.
    /// Tests business logic and state transitions.
    /// </summary>
    public class EntityTests
    {
        #region ReservationEntity Tests

        [Fact]
        public void ReservationEntity_Creation_SetsCorrectDefaults()
        {
            // Arrange & Act
            var reservation = new ReservationEntity(
                checkIn: new DateOnly(2025, 6, 15),
                checkOut: new DateOnly(2025, 6, 18),
                hotelCode: "VAROSVILL",
                hotelName: "Varos Village Hotel",
                totalAmount: 300m,
                totalRooms: 1);

            // Assert
            Assert.Equal(BookingStatus.New, reservation.Status);
            Assert.Equal(300m, reservation.RemainingAmount);
            Assert.Equal(3, reservation.Nights);
            Assert.False(reservation.IsFullyPaid);
            Assert.False(reservation.IsTerminal);
        }

        [Fact]
        public void ReservationEntity_Creation_ThrowsOnInvalidDates()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new ReservationEntity(
                checkIn: new DateOnly(2025, 6, 18),
                checkOut: new DateOnly(2025, 6, 15), // Before check-in
                hotelCode: "VAROSVILL",
                hotelName: "Varos Village Hotel",
                totalAmount: 300m,
                totalRooms: 1));
        }

        [Fact]
        public void ReservationEntity_Creation_ThrowsOnNegativeAmount()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new ReservationEntity(
                checkIn: new DateOnly(2025, 6, 15),
                checkOut: new DateOnly(2025, 6, 18),
                hotelCode: "VAROSVILL",
                hotelName: "Varos Village Hotel",
                totalAmount: -100m,
                totalRooms: 1));
        }

        [Fact]
        public void ReservationEntity_Creation_ThrowsOnZeroRooms()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentException>(() => new ReservationEntity(
                checkIn: new DateOnly(2025, 6, 15),
                checkOut: new DateOnly(2025, 6, 18),
                hotelCode: "VAROSVILL",
                hotelName: "Varos Village Hotel",
                totalAmount: 300m,
                totalRooms: 0));
        }

        [Fact]
        public void ReservationEntity_MarkAsPending_TransitionsFromNew()
        {
            // Arrange
            var reservation = CreateTestReservation();

            // Act
            reservation.MarkAsPending();

            // Assert
            Assert.Equal(BookingStatus.Pending, reservation.Status);
        }

        [Fact]
        public void ReservationEntity_MarkAsPending_ThrowsIfNotNew()
        {
            // Arrange
            var reservation = CreateTestReservation();
            reservation.MarkAsPending();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reservation.MarkAsPending());
        }

        [Fact]
        public void ReservationEntity_Confirm_TransitionsFromRunning()
        {
            // Arrange
            var reservation = CreateTestReservation();
            reservation.MarkAsPending();
            reservation.MarkAsRunning();

            // Act
            reservation.Confirm();

            // Assert
            Assert.Equal(BookingStatus.Confirmed, reservation.Status);
            Assert.True(reservation.IsTerminal);
        }

        [Fact]
        public void ReservationEntity_Cancel_WorksWhenPending()
        {
            // Arrange
            var reservation = CreateTestReservation();
            reservation.MarkAsPending();
            Assert.True(reservation.CanBeCancelled);

            // Act
            reservation.Cancel();

            // Assert
            Assert.Equal(BookingStatus.Cancelled, reservation.Status);
            Assert.True(reservation.IsTerminal);
        }

        [Fact]
        public void ReservationEntity_Cancel_ThrowsWhenNew()
        {
            // Arrange
            var reservation = CreateTestReservation();
            Assert.False(reservation.CanBeCancelled); // New status cannot be cancelled

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reservation.Cancel());
        }

        [Fact]
        public void ReservationEntity_RecordPayment_ReducesRemainingAmount()
        {
            // Arrange
            var reservation = CreateTestReservation(); // 300m total

            // Act
            reservation.RecordPayment(100m);

            // Assert
            Assert.Equal(200m, reservation.RemainingAmount);
            Assert.Equal(100m, reservation.PaidAmount);
            Assert.False(reservation.IsFullyPaid);
        }

        [Fact]
        public void ReservationEntity_RecordPayment_FullPayment()
        {
            // Arrange
            var reservation = CreateTestReservation(); // 300m total

            // Act
            reservation.RecordPayment(300m);

            // Assert
            Assert.Equal(0m, reservation.RemainingAmount);
            Assert.Equal(300m, reservation.PaidAmount);
            Assert.True(reservation.IsFullyPaid);
        }

        [Fact]
        public void ReservationEntity_RecordPayment_ThrowsOnOverpayment()
        {
            // Arrange
            var reservation = CreateTestReservation(); // 300m total

            // Act & Assert
            Assert.Throws<ArgumentException>(() => reservation.RecordPayment(400m));
        }

        [Fact]
        public void ReservationEntity_RecordPayment_ThrowsOnNegativeAmount()
        {
            // Arrange
            var reservation = CreateTestReservation();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => reservation.RecordPayment(-50m));
        }

        [Fact]
        public void ReservationEntity_ApplyCoupon_WorksWhenNew()
        {
            // Arrange
            var reservation = CreateTestReservation();

            // Act
            reservation.ApplyCoupon("SUMMER2025");

            // Assert
            Assert.Equal("SUMMER2025", reservation.CouponCode);
        }

        [Fact]
        public void ReservationEntity_ApplyCoupon_ThrowsWhenConfirmed()
        {
            // Arrange
            var reservation = CreateTestReservation();
            reservation.MarkAsPending();
            reservation.MarkAsRunning();
            reservation.Confirm();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => reservation.ApplyCoupon("SUMMER2025"));
        }

        private static ReservationEntity CreateTestReservation()
        {
            return new ReservationEntity(
                checkIn: new DateOnly(2025, 6, 15),
                checkOut: new DateOnly(2025, 6, 18),
                hotelCode: "VAROSVILL",
                hotelName: "Varos Village Hotel",
                totalAmount: 300m,
                totalRooms: 1);
        }

        #endregion
    }
}
