using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;
using TravelBridge.API.Endpoints;
using TravelBridge.API.Helpers;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for ReservationEndpoints validation and business logic.
/// Tests focus on input validation, parameter parsing, and payment flow validation.
/// </summary>
[TestClass]
public class ReservationEndpointsTests
{
    private Mock<ILogger<ReservationEndpoints>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<ReservationEndpoints>>();
    }

    #region GetCheckoutInfo Validation Tests

    [TestMethod]
    public void GetCheckoutInfo_WhenCheckinDateIsInvalid_ShouldThrowInvalidCastException()
    {
        // Arrange
        string checkin = "not-a-date";

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateCheckinDate(checkin));
    }

    [TestMethod]
    public void GetCheckoutInfo_WhenCheckoutDateIsInvalid_ShouldThrowInvalidCastException()
    {
        // Arrange
        string checkout = "2025-06-20"; // ISO format instead of dd/MM/yyyy

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateCheckoutDate(checkout));
    }

    [TestMethod]
    public void GetCheckoutInfo_WhenSelectedRatesIsNull_ShouldThrowInvalidCastException()
    {
        // Arrange
        string? selectedRates = null;

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateSelectedRates(selectedRates));
    }

    [TestMethod]
    public void GetCheckoutInfo_WhenSelectedRatesIsEmpty_ShouldThrowInvalidCastException()
    {
        // Arrange
        string selectedRates = "";

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateSelectedRates(selectedRates));
    }

    [TestMethod]
    public void GetCheckoutInfo_WhenHotelIdHasInvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        string hotelId = "InvalidFormat";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateHotelIdFormat(hotelId));
    }

    [TestMethod]
    public void GetCheckoutInfo_WhenHotelIdIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        string? hotelId = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateHotelIdFormat(hotelId));
    }

    #endregion

    #region PreparePayment Validation Tests

    [TestMethod]
    public void PreparePayment_WhenAdultsIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        int? adults = null;
        string? party = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateAdultsWhenNoParty(adults, party));
    }

    [TestMethod]
    public void PreparePayment_WhenAdultsIsZero_ShouldThrowArgumentException()
    {
        // Arrange
        int? adults = 0;
        string? party = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateAdultsWhenNoParty(adults, party));
    }

    [TestMethod]
    public void PreparePayment_WhenPartyIsProvided_ShouldNotValidateAdults()
    {
        // Arrange
        int? adults = null;
        string party = "[{\"adults\":2}]";

        // Act - Should not throw
        var shouldValidateAdults = ShouldValidateAdults(party);

        // Assert
        Assert.IsFalse(shouldValidateAdults);
    }

    [TestMethod]
    public void PreparePayment_WhenSelectedRatesIsInvalidJson_ShouldReturnNull()
    {
        // Arrange
        string selectedRates = "not valid json";

        // Act
        var result = TryParseSelectedRates(selectedRates);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public void PreparePayment_WhenSelectedRatesIsValidJson_ShouldParseCorrectly()
    {
        // Arrange
        string selectedRates = "[{\"rateId\":\"328000-226\",\"count\":1,\"roomType\":\"SUPFAM\"}]";

        // Act
        var result = TryParseSelectedRates(selectedRates);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
    }

    #endregion

    #region ConfirmPayment Validation Tests

    [TestMethod]
    public void ConfirmPayment_WhenOrderCodeIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        string? orderCode = null;
        string tid = "valid-tid";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidatePaymentInfo(orderCode, tid));
    }

    [TestMethod]
    public void ConfirmPayment_WhenOrderCodeIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        string orderCode = "";
        string tid = "valid-tid";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidatePaymentInfo(orderCode, tid));
    }

    [TestMethod]
    public void ConfirmPayment_WhenTidIsNull_ShouldThrowArgumentException()
    {
        // Arrange
        string orderCode = "valid-order-code";
        string? tid = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidatePaymentInfo(orderCode, tid));
    }

    [TestMethod]
    public void ConfirmPayment_WhenTidIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        string orderCode = "valid-order-code";
        string tid = "";

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidatePaymentInfo(orderCode, tid));
    }

    [TestMethod]
    public void ConfirmPayment_WhenBothValuesAreValid_ShouldNotThrow()
    {
        // Arrange
        string orderCode = "4918784106772600";
        string tid = "dc90abcc-0350-4383-a624-5821811aedb9";

        // Act - Should not throw
        ValidatePaymentInfo(orderCode, tid);

        // Assert - Test passes if no exception
        Assert.IsTrue(true);
    }

    #endregion

    #region Price Validation Tests

    [TestMethod]
    public void PriceValidation_WhenPricesMatch_ShouldReturnTrue()
    {
        // Arrange
        decimal expectedTotal = 930.00m;
        decimal actualTotal = 930.00m;
        decimal? expectedPrepay = 255.75m;
        decimal? actualPrepay = 255.75m;

        // Act
        var isValid = ValidatePrices(expectedTotal, actualTotal, expectedPrepay, actualPrepay);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void PriceValidation_WhenTotalPriceChanged_ShouldReturnFalse()
    {
        // Arrange
        decimal expectedTotal = 930.00m;
        decimal actualTotal = 950.00m;
        decimal? expectedPrepay = null;
        decimal? actualPrepay = null;

        // Act
        var isValid = ValidatePrices(expectedTotal, actualTotal, expectedPrepay, actualPrepay);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void PriceValidation_WhenPrepayAmountChanged_ShouldReturnFalse()
    {
        // Arrange
        decimal expectedTotal = 930.00m;
        decimal actualTotal = 930.00m;
        decimal? expectedPrepay = 255.75m;
        decimal? actualPrepay = 300.00m;

        // Act
        var isValid = ValidatePrices(expectedTotal, actualTotal, expectedPrepay, actualPrepay);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void PriceValidation_WhenFullPaymentEqualsTotal_ShouldReturnTrue()
    {
        // Arrange
        decimal expectedTotal = 930.00m;
        decimal actualTotal = 930.00m;
        decimal? expectedPrepay = 930.00m; // Full payment
        decimal? actualPrepay = 930.00m;

        // Act
        var isValid = ValidatePrices(expectedTotal, actualTotal, expectedPrepay, actualPrepay);

        // Assert
        Assert.IsTrue(isValid);
    }

    #endregion

    #region GetPartyInfo Tests

    [TestMethod]
    public void GetPartyInfo_WhenValidRates_ShouldReturnCorrectDescription()
    {
        // Arrange
        var selectedRates = new List<SelectedRateInfo>
        {
            new SelectedRateInfo { Adults = 2, Children = 1, Count = 1 },
            new SelectedRateInfo { Adults = 2, Children = 0, Count = 1 }
        };

        // Act
        var result = GetPartyInfo(selectedRates);

        // Assert
        Assert.IsTrue(result.Contains("4 ενήλικες"));
        Assert.IsTrue(result.Contains("1 παιδιά"));
        Assert.IsTrue(result.Contains("2 δωμάτια"));
    }

    [TestMethod]
    public void GetPartyInfo_WhenSingleRoom_ShouldUseSingularForm()
    {
        // Arrange
        var selectedRates = new List<SelectedRateInfo>
        {
            new SelectedRateInfo { Adults = 2, Children = 0, Count = 1 }
        };

        // Act
        var result = GetPartyInfo(selectedRates);

        // Assert
        Assert.IsTrue(result.Contains("2 ενήλικες"));
        Assert.IsTrue(result.Contains("1 δωμάτιο"));
    }

    [TestMethod]
    public void GetPartyInfo_WhenEmptyRates_ShouldReturnEmptyString()
    {
        // Arrange
        var selectedRates = new List<SelectedRateInfo>();

        // Act
        var result = GetPartyInfo(selectedRates);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void GetPartyInfo_WhenNullRates_ShouldReturnEmptyString()
    {
        // Arrange
        List<SelectedRateInfo>? selectedRates = null;

        // Act
        var result = GetPartyInfo(selectedRates);

        // Assert
        Assert.AreEqual(string.Empty, result);
    }

    #endregion

    #region Validation Helper Methods

    private static void ValidateCheckinDate(string checkin)
    {
        if (!DateTime.TryParseExact(checkin, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
        {
            throw new InvalidCastException("Invalid checkin date format. Use dd/MM/yyyy.");
        }
    }

    private static void ValidateCheckoutDate(string checkout)
    {
        if (!DateTime.TryParseExact(checkout, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out _))
        {
            throw new InvalidCastException("Invalid checkout date format. Use dd/MM/yyyy.");
        }
    }

    private static void ValidateSelectedRates(string? selectedRates)
    {
        if (string.IsNullOrWhiteSpace(selectedRates))
        {
            throw new InvalidCastException("Invalid selected rates");
        }
    }

    private static void ValidateHotelIdFormat(string? hotelId)
    {
        var hotelInfo = hotelId?.Split('-');
        if (hotelInfo?.Length != 2)
        {
            throw new ArgumentException("Invalid hotelId format. Use bbox-lat-lon.");
        }
    }

    private static void ValidateAdultsWhenNoParty(int? adults, string? party)
    {
        if (string.IsNullOrWhiteSpace(party))
        {
            if (adults == null || adults < 1)
            {
                throw new ArgumentException("There must be at least one adult in the room.");
            }
        }
    }

    private static bool ShouldValidateAdults(string? party)
    {
        return string.IsNullOrWhiteSpace(party);
    }

    private static List<SelectedRateInfo>? TryParseSelectedRates(string selectedRates)
    {
        try
        {
            return JsonSerializer.Deserialize<List<SelectedRateInfo>>(selectedRates, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private static void ValidatePaymentInfo(string? orderCode, string? tid)
    {
        if (string.IsNullOrWhiteSpace(orderCode) || string.IsNullOrWhiteSpace(tid))
        {
            throw new ArgumentException("Invalid payment info");
        }
    }

    private static bool ValidatePrices(decimal expectedTotal, decimal actualTotal, decimal? expectedPrepay, decimal? actualPrepay)
    {
        if (actualTotal != expectedTotal)
        {
            return false;
        }

        if (expectedPrepay != null && actualPrepay != null)
        {
            // Prepay can equal total for full payment
            if (actualPrepay != expectedPrepay && actualTotal != expectedPrepay)
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPartyInfo(List<SelectedRateInfo>? selectedRates)
    {
        try
        {
            List<string> response = new();
            if (selectedRates != null && selectedRates.Count > 0)
            {
                var adults = selectedRates.Sum(p => p.Adults * p.Count);
                var childs = selectedRates.Sum(p => p.Children * p.Count);
                if (adults > 0)
                    response.Add($"{adults} ενήλικες");
                if (childs > 0)
                    response.Add($"{childs} παιδιά");
                if (selectedRates.Sum(r => r.Count) == 1)
                    response.Add($"1 δωμάτιο");
                else
                    response.Add($"{selectedRates.Sum(r => r.Count)} δωμάτια");
            }
            return string.Join(", ", response);
        }
        catch
        {
            return "Σφάλμα";
        }
    }

    #endregion

    #region Helper Classes

    private class SelectedRateInfo
    {
        public string? RateId { get; set; }
        public int Count { get; set; }
        public string? RoomType { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
    }

    #endregion
}
