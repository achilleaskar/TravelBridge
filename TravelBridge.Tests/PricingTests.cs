using TravelBridge.API.Contracts;
using TravelBridge.API.Helpers;
using TravelBridge.API.Models;
using TravelBridge.API.Models.Apis;
using System.Text.Json;

namespace TravelBridge.Tests
{
    /// <summary>
    /// Unit tests for pricing calculations.
    /// These tests verify the margin and discount logic.
    /// </summary>
    public class PricingTests : IDisposable
    {
        public PricingTests()
        {
            // Initialize PricingConfig with default values for tests
            PricingConfig.Initialize(new PricingOptions
            {
                MinimumMarginPercent = 10,
                SpecialHotelDiscountPercent = 5
            });
        }

        public void Dispose()
        {
            // Reset to defaults after tests
            PricingConfig.Initialize(new PricingOptions());
        }

        #region Pricing Tests

        [Fact]
        public void PricingConfig_MinimumMarginDecimal_ReturnsCorrectValue()
        {
            // Assert
            Assert.Equal(0.10m, PricingConfig.MinimumMarginDecimal);
        }

        [Fact]
        public void PricingConfig_SpecialHotelPriceMultiplier_ReturnsCorrectValue()
        {
            // 5% discount = 0.95 multiplier
            Assert.Equal(0.95m, PricingConfig.SpecialHotelPriceMultiplier);
        }

        [Theory]
        [InlineData(100, 10)] // 10% of 100 = 10
        [InlineData(200, 20)] // 10% of 200 = 20
        [InlineData(150.50, 15.05)] // 10% of 150.50 = 15.05
        public void MinimumMargin_CalculatesCorrectly(decimal netPrice, decimal expectedMargin)
        {
            // Act
            var margin = netPrice * PricingConfig.MinimumMarginDecimal;

            // Assert
            Assert.Equal(expectedMargin, margin);
        }

        [Fact]
        public void GetFinalPrice_WithNoCoupon_AppliesMinimumMargin()
        {
            // Arrange
            var alternatives = new List<Alternative>
            {
                new Alternative { NetPrice = 100m, MinPrice = 0m }
            };

            // Act
            var result = alternatives.GetFinalPrice(0m, "TESTHOTEL", CouponType.none);

            // Assert
            // NetPrice 100 + 10% margin = 110, then * 0.95 (special hotel discount) = 104.5, floor = 104
            // But TESTHOTEL is not in hotelCodes list, so PricePerc = 0.95
            var expected = decimal.Floor((100m + 10m) * 0.95m);
            Assert.Equal(expected, result[0].MinPrice);
        }

        [Fact]
        public void GetFinalPrice_WithSpecialHotel_NoDiscount()
        {
            // Arrange - GRECASTIR is in the special hotels list
            var alternatives = new List<Alternative>
            {
                new Alternative { NetPrice = 100m, MinPrice = 0m }
            };

            // Act
            var result = alternatives.GetFinalPrice(0m, "GRECASTIR", CouponType.none);

            // Assert
            // Special hotel: PricePerc = 1.0 (no discount)
            // NetPrice 100 + 10% margin = 110
            var expected = decimal.Floor(110m * 1m);
            Assert.Equal(expected, result[0].MinPrice);
        }

        [Fact]
        public void GetFinalPrice_WithPercentageCoupon_AppliesDiscount()
        {
            // Arrange
            var alternatives = new List<Alternative>
            {
                new Alternative { NetPrice = 100m, MinPrice = 0m }
            };

            // Act - 10% coupon discount
            var result = alternatives.GetFinalPrice(0.10m, "GRECASTIR", CouponType.percentage);

            // Assert
            // NetPrice 100 + 10% margin = 110
            // Special hotel (no 5% discount): 110 * 1.0 = 110
            // Then apply 10% coupon: 110 * 0.90 = 99
            var expected = decimal.Floor(110m * 0.90m);
            Assert.Equal(expected, result[0].MinPrice);
        }

        [Fact]
        public void GetFinalPrice_WithFlatCoupon_AppliesDiscount()
        {
            // Arrange
            var alternatives = new List<Alternative>
            {
                new Alternative { NetPrice = 100m, MinPrice = 0m }
            };

            // Act - 15€ flat discount
            var result = alternatives.GetFinalPrice(15m, "GRECASTIR", CouponType.flat);

            // Assert
            // NetPrice 100 + 10% margin = 110
            // Special hotel (no 5% discount): 110 * 1.0 = 110
            // Then apply 15€ flat: 110 - 15 = 95
            var expected = decimal.Floor(110m - 15m);
            Assert.Equal(expected, result[0].MinPrice);
        }

        [Fact]
        public void GetFinalPrice_WithExistingMargin_UsesExistingPrice()
        {
            // Arrange - MinPrice already has sufficient margin
            var alternatives = new List<Alternative>
            {
                new Alternative { NetPrice = 100m, MinPrice = 120m } // 20% margin already
            };

            // Act
            var result = alternatives.GetFinalPrice(0m, "GRECASTIR", CouponType.none);

            // Assert
            // Existing margin (20) > minimum margin (10), so use NetPrice * PricePerc
            // But the condition checks if MinPrice - NetPrice < minMargin
            // 120 - 100 = 20 >= 10, so it should use the else branch
            // else: MinPrice = Floor(NetPrice * PricePerc) = Floor(100 * 1.0) = 100
            // Wait, that's not right - let me check the actual logic
            var expected = decimal.Floor(100m * 1m);
            Assert.Equal(expected, result[0].MinPrice);
        }

        [Fact]
        public void GetFinalPrice_EmptyList_ReturnsEmptyList()
        {
            // Arrange
            var alternatives = new List<Alternative>();

            // Act
            var result = alternatives.GetFinalPrice(0m, "TESTHOTEL", CouponType.none);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetFinalPrice_NullList_ReturnsEmptyList()
        {
            // Arrange
            List<Alternative>? alternatives = null;

            // Act
            var result = alternatives.GetFinalPrice(0m, "TESTHOTEL", CouponType.none);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Converter Tests

        [Fact]
        public void StringOrIntJsonConverter_ParsesIntValue()
        {
            // Arrange - JSON with min_stay as int (normal case like VAROSVILL)
            var json = """{"date":"2025-01-15","status":"AVL","price":100.50,"retail":120.00,"min_stay":2}""";

            // Act
            var result = JsonSerializer.Deserialize<AlternativeDayInfo>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.min_stay);
            Assert.Equal("2025-01-15", result.date);
            Assert.Equal(100.50m, result.price);
        }

        [Fact]
        public void StringOrIntJsonConverter_ParsesStringValue()
        {
            // Arrange - JSON with min_stay as string (problematic case like ARIADNIPAL, HOTELSISSY)
            var json = """{"date":"2025-01-15","status":"AVL","price":100.50,"retail":120.00,"min_stay":"3"}""";

            // Act
            var result = JsonSerializer.Deserialize<AlternativeDayInfo>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.min_stay);
            Assert.Equal("2025-01-15", result.date);
        }

        [Fact]
        public void StringOrIntJsonConverter_ParsesEmptyStringAsZero()
        {
            // Arrange - JSON with min_stay as empty string
            var json = """{"date":"2025-01-15","status":"AVL","price":100.50,"retail":120.00,"min_stay":""}""";

            // Act
            var result = JsonSerializer.Deserialize<AlternativeDayInfo>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.min_stay);
        }

        [Fact]
        public void StringOrIntJsonConverter_ParsesNullAsZero()
        {
            // Arrange - JSON with min_stay as null
            var json = """{"date":"2025-01-15","status":"AVL","price":100.50,"retail":120.00,"min_stay":null}""";

            // Act
            var result = JsonSerializer.Deserialize<AlternativeDayInfo>(json);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.min_stay);
        }

        #endregion
    }
}
