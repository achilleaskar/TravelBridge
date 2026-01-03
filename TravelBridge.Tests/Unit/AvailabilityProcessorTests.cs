using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.API.Contracts;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.API.Services;
using TravelBridge.Contracts.Common;
using TravelBridge.Contracts.Models.Hotels;
using static TravelBridge.API.Helpers.General;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for AvailabilityProcessor static methods.
/// Tests focus on availability filtering and sufficiency checking logic.
/// </summary>
[TestClass]
public class AvailabilityProcessorTests
{
    #region FilterHotelsByAvailability Tests

    [TestMethod]
    public void FilterHotelsByAvailability_WhenResponseResultsIsNull_ShouldReturnEmpty()
    {
        // Arrange
        var response = new PluginSearchResponse { Results = null };
        var partyList = new List<PartyItem> { new PartyItem { adults = 2, RoomsCount = 1 } };

        // Act
        var result = AvailabilityProcessor.FilterHotelsByAvailability(response, partyList);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    [TestMethod]
    public void FilterHotelsByAvailability_WhenResponseResultsIsEmpty_ShouldReturnEmpty()
    {
        // Arrange
        var response = new PluginSearchResponse { Results = new List<WebHotel>() };
        var partyList = new List<PartyItem> { new PartyItem { adults = 2, RoomsCount = 1 } };

        // Act
        var result = AvailabilityProcessor.FilterHotelsByAvailability(response, partyList);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count());
    }

    #endregion

    #region HasSufficientAvailability Tests

    [TestMethod]
    public void HasSufficientAvailability_WhenResponseDataIsNull_ShouldReturnFalse()
    {
        // Arrange
        var response = new SingleAvailabilityResponse { Data = null };
        var selectedRates = new List<SelectedRate> { new SelectedRate { rateId = "123", count = 1, searchParty = "[{\"adults\":2}]" } };

        // Act
        var result = AvailabilityProcessor.HasSufficientAvailability(response, selectedRates);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasSufficientAvailability_WhenRoomsIsNull_ShouldReturnFalse()
    {
        // Arrange
        var response = new SingleAvailabilityResponse 
        { 
            Data = new SingleHotelAvailabilityInfo { Rooms = null } 
        };
        var selectedRates = new List<SelectedRate> { new SelectedRate { rateId = "123", count = 1, searchParty = "[{\"adults\":2}]" } };

        // Act
        var result = AvailabilityProcessor.HasSufficientAvailability(response, selectedRates);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void HasSufficientAvailability_WhenRoomsIsEmpty_ShouldReturnFalse()
    {
        // Arrange
        var response = new SingleAvailabilityResponse 
        { 
            Data = new SingleHotelAvailabilityInfo { Rooms = new List<SingleHotelRoom>() } 
        };
        var selectedRates = new List<SelectedRate> { new SelectedRate { rateId = "123", count = 1, searchParty = "[{\"adults\":2}]" } };

        // Act
        var result = AvailabilityProcessor.HasSufficientAvailability(response, selectedRates);

        // Assert
        Assert.IsFalse(result);
    }

    #endregion
}
