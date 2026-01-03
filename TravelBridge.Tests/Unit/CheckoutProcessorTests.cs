using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Services;
using TravelBridge.Contracts.Common.Payments;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for CheckoutProcessor static methods.
/// Tests focus on payment calculation and checkout processing logic.
/// </summary>
[TestClass]
public class CheckoutProcessorTests
{
    #region CalculatePayments Tests

    [TestMethod]
    public void CalculatePayments_WhenValidCheckoutResponse_ShouldCalculateTotalPrice()
    {
        // Arrange
        var response = CreateTestCheckoutResponse();

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(300m, response.TotalPrice); // 100 + 200
    }

    [TestMethod]
    public void CalculatePayments_WhenInvalidCheckinDate_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "invalid-date",
            Rooms = new List<CheckoutRoomInfo>()
        };

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => CheckoutProcessor.CalculatePayments(response));
    }

    [TestMethod]
    public void CalculatePayments_WhenNoRooms_ShouldSetTotalPriceToZero()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "15/06/2025",
            Rooms = new List<CheckoutRoomInfo>()
        };

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(0m, response.TotalPrice);
    }

    [TestMethod]
    public void CalculatePayments_WhenSingleRoom_ShouldCalculateCorrectly()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "15/06/2025",
            Rooms = new List<CheckoutRoomInfo>
            {
                new CheckoutRoomInfo
                {
                    RoomName = "Standard Room",
                    TotalPrice = 150m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Payments = new List<PaymentWH>()
                    }
                }
            }
        };

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(150m, response.TotalPrice);
    }

    [TestMethod]
    public void CalculatePayments_ShouldClearPaymentsAfterProcessing()
    {
        // Arrange
        var response = CreateTestCheckoutResponse();

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.IsNotNull(response.Payments);
        Assert.AreEqual(0, response.Payments.Count);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void CalculatePayments_WhenRoomPriceIsZero_ShouldHandleCorrectly()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "15/06/2025",
            Rooms = new List<CheckoutRoomInfo>
            {
                new CheckoutRoomInfo
                {
                    RoomName = "Free Room",
                    TotalPrice = 0m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Payments = new List<PaymentWH>()
                    }
                }
            }
        };

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(0m, response.TotalPrice);
    }

    [TestMethod]
    public void CalculatePayments_WhenDecimalPrices_ShouldCalculateCorrectly()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "15/06/2025",
            Rooms = new List<CheckoutRoomInfo>
            {
                new CheckoutRoomInfo
                {
                    RoomName = "Room 1",
                    TotalPrice = 99.99m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Payments = new List<PaymentWH>()
                    }
                },
                new CheckoutRoomInfo
                {
                    RoomName = "Room 2",
                    TotalPrice = 150.01m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Payments = new List<PaymentWH>()
                    }
                }
            }
        };

        // Act
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(250m, response.TotalPrice);
    }

    #endregion

    #region Date Format Tests

    [TestMethod]
    public void CalculatePayments_WhenCheckinIsISO8601_ShouldThrow()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "2025-06-15", // ISO format
            Rooms = new List<CheckoutRoomInfo>()
        };

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => CheckoutProcessor.CalculatePayments(response));
    }

    [TestMethod]
    public void CalculatePayments_WhenCheckinIsUSFormat_ShouldThrow()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "06/15/2025", // US format MM/dd/yyyy
            Rooms = new List<CheckoutRoomInfo>()
        };

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => CheckoutProcessor.CalculatePayments(response));
    }

    [TestMethod]
    public void CalculatePayments_WhenCheckinIsCorrectFormat_ShouldNotThrow()
    {
        // Arrange
        var response = new CheckoutResponse
        {
            CheckIn = "15/06/2025", // Correct dd/MM/yyyy format
            Rooms = new List<CheckoutRoomInfo>()
        };

        // Act - Should not throw
        CheckoutProcessor.CalculatePayments(response);

        // Assert
        Assert.AreEqual(0m, response.TotalPrice);
    }

    #endregion

    #region Helper Methods

    private static CheckoutResponse CreateTestCheckoutResponse()
    {
        return new CheckoutResponse
        {
            CheckIn = "15/06/2025",
            CheckOut = "20/06/2025",
            Rooms = new List<CheckoutRoomInfo>
            {
                new CheckoutRoomInfo
                {
                    RoomName = "Standard Room",
                    TotalPrice = 100m,
                    NetPrice = 90m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Board = "Breakfast",
                        Payments = new List<PaymentWH>()
                    }
                },
                new CheckoutRoomInfo
                {
                    RoomName = "Deluxe Room",
                    TotalPrice = 200m,
                    NetPrice = 180m,
                    RateProperties = new CheckoutRateProperties
                    {
                        Board = "Half Board",
                        Payments = new List<PaymentWH>()
                    }
                }
            }
        };
    }

    #endregion
}
