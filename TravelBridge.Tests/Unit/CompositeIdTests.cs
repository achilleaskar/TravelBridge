using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.Providers.Abstractions;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for CompositeId parsing and formatting.
/// These tests lock in the ID format behavior to prevent accidental format changes.
/// </summary>
[TestClass]
public class CompositeIdTests
{
    #region TryParse Tests - Valid Cases

    [TestMethod]
    public void TryParse_ValidWebHotelierId_ReturnsTrue()
    {
        // Arrange
        const string input = "1-VAROSRESID";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(ProviderIds.WebHotelier, id.ProviderId);
        Assert.AreEqual("VAROSRESID", id.Value);
    }

    [TestMethod]
    public void TryParse_ValidOwnedId_ReturnsTrue()
    {
        // Arrange
        const string input = "0-123";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(ProviderIds.Owned, id.ProviderId);
        Assert.AreEqual("123", id.Value);
    }

    [TestMethod]
    public void TryParse_ValueContainsDashes_ParsesCorrectly()
    {
        // Arrange - Value contains additional dashes (split on first dash only)
        const string input = "1-A-B-C";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, id.ProviderId);
        Assert.AreEqual("A-B-C", id.Value);
    }

    [TestMethod]
    public void TryParse_FutureProviderId_ReturnsTrue()
    {
        // Arrange - Provider ID 2 (future provider)
        const string input = "2-SOMEHOTEL";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(2, id.ProviderId);
        Assert.AreEqual("SOMEHOTEL", id.Value);
    }

    [TestMethod]
    public void TryParse_LargeProviderId_ReturnsTrue()
    {
        // Arrange
        const string input = "999-HOTEL123";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(999, id.ProviderId);
        Assert.AreEqual("HOTEL123", id.Value);
    }

    [TestMethod]
    public void TryParse_SingleCharacterValue_ReturnsTrue()
    {
        // Arrange
        const string input = "1-X";

        // Act
        var result = CompositeId.TryParse(input, out var id);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(1, id.ProviderId);
        Assert.AreEqual("X", id.Value);
    }

    #endregion

    #region TryParse Tests - Invalid Cases

    [TestMethod]
    public void TryParse_NullInput_ReturnsFalse()
    {
        // Act
        var result = CompositeId.TryParse(null, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_EmptyString_ReturnsFalse()
    {
        // Act
        var result = CompositeId.TryParse("", out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_WhitespaceOnly_ReturnsFalse()
    {
        // Act
        var result = CompositeId.TryParse("   ", out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_MissingDash_ReturnsFalse()
    {
        // Arrange
        const string input = "1VAROSRESID";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_DashAtStart_ReturnsFalse()
    {
        // Arrange - No provider ID before dash
        const string input = "-ABC";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_DashAtEnd_ReturnsFalse()
    {
        // Arrange - No value after dash
        const string input = "1-";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_NonNumericProviderId_ReturnsFalse()
    {
        // Arrange
        const string input = "ABC-123";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_NegativeProviderId_ReturnsFalse()
    {
        // Arrange - Negative provider IDs are not valid (dash is separator)
        // Input "-1-ABC" would have empty string before first dash
        const string input = "-1-ABC";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_OnlyDash_ReturnsFalse()
    {
        // Act
        var result = CompositeId.TryParse("-", out _);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TryParse_ProviderIdWithLetters_ReturnsFalse()
    {
        // Arrange
        const string input = "1a-HOTEL";

        // Act
        var result = CompositeId.TryParse(input, out _);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion

    #region Parse Tests

    [TestMethod]
    public void Parse_ValidInput_ReturnsCompositeId()
    {
        // Arrange
        const string input = "1-VAROSRESID";

        // Act
        var id = CompositeId.Parse(input);

        // Assert
        Assert.AreEqual(1, id.ProviderId);
        Assert.AreEqual("VAROSRESID", id.Value);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Parse_InvalidInput_ThrowsArgumentException()
    {
        // Arrange
        const string input = "invalid";

        // Act
        CompositeId.Parse(input);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Parse_NullInput_ThrowsArgumentException()
    {
        // Act
        CompositeId.Parse(null);
    }

    #endregion

    #region ToString Tests

    [TestMethod]
    public void ToString_ReturnsOriginalFormat()
    {
        // Arrange
        var id = new CompositeId(1, "VAROSRESID");

        // Act
        var result = id.ToString();

        // Assert
        Assert.AreEqual("1-VAROSRESID", result);
    }

    [TestMethod]
    public void ToString_RoundTrip_PreservesFormat()
    {
        // Arrange
        const string original = "1-A-B-C";

        // Act
        var id = CompositeId.Parse(original);
        var result = id.ToString();

        // Assert
        Assert.AreEqual(original, result);
    }

    #endregion

    #region Constructor Tests

    [TestMethod]
    public void Constructor_ValidArgs_CreatesInstance()
    {
        // Act
        var id = new CompositeId(1, "HOTEL123");

        // Assert
        Assert.AreEqual(1, id.ProviderId);
        Assert.AreEqual("HOTEL123", id.Value);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_NullValue_ThrowsArgumentException()
    {
        // Act
        _ = new CompositeId(1, null!);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_EmptyValue_ThrowsArgumentException()
    {
        // Act
        _ = new CompositeId(1, "");
    }

    #endregion

    #region ProviderIds Constants Tests

    [TestMethod]
    public void ProviderIds_OwnedIs0()
    {
        Assert.AreEqual(0, ProviderIds.Owned);
    }

    [TestMethod]
    public void ProviderIds_WebHotelierIs1()
    {
        Assert.AreEqual(1, ProviderIds.WebHotelier);
    }

    #endregion

    #region Equality Tests

    [TestMethod]
    public void Equals_SameValues_ReturnsTrue()
    {
        // Arrange
        var id1 = new CompositeId(1, "HOTEL");
        var id2 = new CompositeId(1, "HOTEL");

        // Assert
        Assert.AreEqual(id1, id2);
    }

    [TestMethod]
    public void Equals_DifferentProviderId_ReturnsFalse()
    {
        // Arrange
        var id1 = new CompositeId(0, "HOTEL");
        var id2 = new CompositeId(1, "HOTEL");

        // Assert
        Assert.AreNotEqual(id1, id2);
    }

    [TestMethod]
    public void Equals_DifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = new CompositeId(1, "HOTEL1");
        var id2 = new CompositeId(1, "HOTEL2");

        // Assert
        Assert.AreNotEqual(id1, id2);
    }

    #endregion
}
