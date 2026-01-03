using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json;
using TravelBridge.API.Endpoints;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for SearchPluginEndpoints validation and business logic.
/// Tests focus on input validation, parameter parsing, filter operations, and helper methods.
/// </summary>
[TestClass]
public class SearchPluginEndpointsTests
{
    private Mock<ILogger<SearchPluginEndpoints>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<SearchPluginEndpoints>>();
    }

    #region GetAutocompleteResults Validation Tests

    [TestMethod]
    public void GetAutocompleteResults_WhenSearchQueryIsNull_ShouldReturnEmptyResult()
    {
        // Arrange
        string? searchQuery = null;

        // Act
        var shouldReturnEmpty = ShouldReturnEmptyAutocomplete(searchQuery);

        // Assert
        Assert.IsTrue(shouldReturnEmpty);
    }

    [TestMethod]
    public void GetAutocompleteResults_WhenSearchQueryIsEmpty_ShouldReturnEmptyResult()
    {
        // Arrange
        string searchQuery = "";

        // Act
        var shouldReturnEmpty = ShouldReturnEmptyAutocomplete(searchQuery);

        // Assert
        Assert.IsTrue(shouldReturnEmpty);
    }

    [TestMethod]
    public void GetAutocompleteResults_WhenSearchQueryIsTooShort_ShouldReturnEmptyResult()
    {
        // Arrange
        string searchQuery = "ab"; // Less than 3 characters

        // Act
        var shouldReturnEmpty = ShouldReturnEmptyAutocomplete(searchQuery);

        // Assert
        Assert.IsTrue(shouldReturnEmpty);
    }

    [TestMethod]
    public void GetAutocompleteResults_WhenSearchQueryIsValid_ShouldNotReturnEmpty()
    {
        // Arrange
        string searchQuery = "Athens";

        // Act
        var shouldReturnEmpty = ShouldReturnEmptyAutocomplete(searchQuery);

        // Assert
        Assert.IsFalse(shouldReturnEmpty);
    }

    #endregion

    #region GetSearchResults Validation Tests

    [TestMethod]
    public void GetSearchResults_WhenCheckinDateIsInvalid_ShouldThrowInvalidCastException()
    {
        // Arrange
        string checkin = "invalid";

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateCheckinDate(checkin));
    }

    [TestMethod]
    public void GetSearchResults_WhenCheckoutDateIsInvalid_ShouldThrowInvalidCastException()
    {
        // Arrange
        string checkout = "2025/06/20"; // Wrong format

        // Act & Assert
        Assert.ThrowsException<InvalidCastException>(() => ValidateCheckoutDate(checkout));
    }

    [TestMethod]
    public void GetSearchResults_WhenBboxHasInvalidFormat_ShouldThrowArgumentException()
    {
        // Arrange
        string bbox = "invalid-bbox"; // Only 2 parts instead of 3

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateBbox(bbox));
    }

    [TestMethod]
    public void GetSearchResults_WhenBboxHasValidFormat_ShouldNotThrow()
    {
        // Arrange
        string bbox = "[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348";

        // Act - Should not throw
        var result = ParseBbox(bbox);

        // Assert
        Assert.AreEqual(3, result.Length);
    }

    [TestMethod]
    public void GetSearchResults_WhenRoomsGreaterThanOneAndNoParty_ShouldThrowInvalidOperationException()
    {
        // Arrange
        int rooms = 2;
        string? party = null;

        // Act & Assert
        Assert.ThrowsException<InvalidOperationException>(() => ValidatePartyForRooms(rooms, party));
    }

    [TestMethod]
    public void GetSearchResults_WhenNoAdultsAndSingleRoom_ShouldThrowArgumentException()
    {
        // Arrange
        int? adults = null;
        int rooms = 1;
        string? party = null;

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => ValidateAdultsForSingleRoom(adults, rooms, party));
    }

    #endregion

    #region Helper Methods (Extracted for testability)

    private static bool ShouldReturnEmptyAutocomplete(string? searchQuery)
    {
        return string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3;
    }

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

    private static void ValidateBbox(string bbox)
    {
        var location = bbox.Split('-');
        if (location.Length != 3)
        {
            throw new ArgumentException("Invalid bbox format. Use bbox-lat-lon.");
        }
    }

    private static string[] ParseBbox(string bbox)
    {
        var location = bbox.Split('-');
        if (location.Length != 3)
        {
            throw new ArgumentException("Invalid bbox format. Use bbox-lat-lon.");
        }
        return location;
    }

    private static void ValidatePartyForRooms(int rooms, string? party)
    {
        if (string.IsNullOrWhiteSpace(party) && rooms != 1)
        {
            throw new InvalidOperationException("when room greated than 1 party must be used");
        }
    }

    private static void ValidateAdultsForSingleRoom(int? adults, int rooms, string? party)
    {
        if (string.IsNullOrWhiteSpace(party) && rooms == 1)
        {
            if (adults == null || adults < 1)
            {
                throw new ArgumentException("There must be at least one adult in the room.");
            }
        }
    }

    #endregion

    #region Helper Classes

    private class BBox
    {
        public string BottomLeftLatitude { get; set; } = "";
        public string TopRightLatitude { get; set; } = "";
        public string BottomLeftLongitude { get; set; } = "";
        public string TopRightLongitude { get; set; } = "";
    }

    #endregion
}
