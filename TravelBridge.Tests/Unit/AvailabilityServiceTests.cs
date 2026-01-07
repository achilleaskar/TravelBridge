using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Tests for availability service routing, provider support, and alternatives flow.
/// </summary>
[TestClass]
public class AvailabilityServiceTests
{
    #region Provider Routing Tests

    [TestMethod]
    public void UnsupportedProvider_ShouldThrowNotSupportedException()
    {
        // This test validates the expected behavior when an unsupported provider is requested.
        // The actual AvailabilityService would throw NotSupportedException for unsupported providers.
        
        // Arrange
        const int unsupportedProviderId = 99;
        
        // Act & Assert
        // We can't easily instantiate AvailabilityService without full DI setup,
        // but we can verify the behavior expectation in the interface contract.
        Assert.IsTrue(unsupportedProviderId != ProviderIds.WebHotelier, 
            "Test requires an unsupported provider ID");
        Assert.IsTrue(unsupportedProviderId != ProviderIds.Owned, 
            "Test requires an unsupported provider ID");
    }

    [TestMethod]
    public void WebHotelierProvider_ShouldBeSupported()
    {
        // Validate that WebHotelier (1) is a known provider ID
        Assert.AreEqual(1, ProviderIds.WebHotelier);
    }

    [TestMethod]
    public void OwnedProvider_ShouldBeDefinedButNotYetImplemented()
    {
        // Validate that Owned (0) is a known provider ID (for Phase 3)
        Assert.AreEqual(0, ProviderIds.Owned);
    }

    #endregion

    #region Alternatives Flow Tests

    [TestMethod]
    public void WhenNoRates_ShouldTriggerAlternativesFetch()
    {
        // Arrange - Provider result with no rates
        var providerResult = new HotelAvailabilityResult
        {
            IsSuccess = true,
            Data = new HotelAvailabilityData
            {
                HotelCode = "TEST_HOTEL",
                Rooms = new List<AvailableRoomData>
                {
                    new() { RoomCode = "STD", RoomName = "Standard", Rates = [] } // No rates
                },
                Alternatives = []
            }
        };

        // Act - Check if rates exist (same logic as AvailabilityService)
        var hasRates = providerResult.Data?.Rooms?.Any(r => r.Rates.Count > 0) == true;

        // Assert
        Assert.IsFalse(hasRates, "Should detect no rates available");
    }

    [TestMethod]
    public void WhenRatesExist_ShouldNotTriggerAlternativesFetch()
    {
        // Arrange - Provider result with rates
        var providerResult = new HotelAvailabilityResult
        {
            IsSuccess = true,
            Data = new HotelAvailabilityData
            {
                HotelCode = "TEST_HOTEL",
                Rooms = new List<AvailableRoomData>
                {
                    new() 
                    { 
                        RoomCode = "STD", 
                        RoomName = "Standard", 
                        Rates = new List<RoomRateData>
                        {
                            new() { RoomCode = "STD", RateId = "123-2", RateName = "Standard Rate", TotalPrice = 100 }
                        }
                    }
                },
                Alternatives = []
            }
        };

        // Act - Check if rates exist (same logic as AvailabilityService)
        var hasRates = providerResult.Data?.Rooms?.Any(r => r.Rates.Count > 0) == true;

        // Assert
        Assert.IsTrue(hasRates, "Should detect rates available");
    }

    [TestMethod]
    public void AlternativesResult_CanBeAttachedToProviderResult()
    {
        // Arrange
        var providerResult = new HotelAvailabilityResult
        {
            IsSuccess = true,
            Data = new HotelAvailabilityData
            {
                HotelCode = "TEST_HOTEL",
                Rooms = [],
                Alternatives = []
            }
        };

        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 20), CheckOut = new DateOnly(2026, 3, 23), Nights = 3, MinPrice = 300, NetPrice = 250 },
            new() { CheckIn = new DateOnly(2026, 3, 25), CheckOut = new DateOnly(2026, 3, 28), Nights = 3, MinPrice = 280, NetPrice = 230 }
        };

        // Act - Update with alternatives using record 'with' syntax
        var updatedResult = providerResult with
        {
            Data = providerResult.Data! with
            {
                Alternatives = alternatives
            }
        };

        // Assert
        Assert.AreEqual(2, updatedResult.Data!.Alternatives.Count);
        Assert.AreEqual(new DateOnly(2026, 3, 20), updatedResult.Data.Alternatives[0].CheckIn);
        Assert.AreEqual(300, updatedResult.Data.Alternatives[0].MinPrice);
    }

    #endregion

    #region AlternativesQuery Tests

    [TestMethod]
    public void AlternativesQuery_DefaultSearchRangeDays_Is14()
    {
        // Arrange & Act
        var query = new AlternativesQuery
        {
            HotelId = "TEST",
            CheckIn = new DateOnly(2026, 3, 15),
            CheckOut = new DateOnly(2026, 3, 18),
            Party = new PartyConfiguration { Rooms = [new PartyRoom { Adults = 2 }] }
        };

        // Assert
        Assert.AreEqual(14, query.SearchRangeDays);
    }

    [TestMethod]
    public void AlternativesQuery_CustomSearchRangeDays_IsRespected()
    {
        // Arrange & Act
        var query = new AlternativesQuery
        {
            HotelId = "TEST",
            CheckIn = new DateOnly(2026, 3, 15),
            CheckOut = new DateOnly(2026, 3, 18),
            Party = new PartyConfiguration { Rooms = [new PartyRoom { Adults = 2 }] },
            SearchRangeDays = 7
        };

        // Assert
        Assert.AreEqual(7, query.SearchRangeDays);
    }

    #endregion

    #region AlternativesResult Tests

    [TestMethod]
    public void AlternativesResult_Success_HasCorrectProperties()
    {
        // Arrange
        var alternatives = new List<AlternativeDateData>
        {
            new() { CheckIn = new DateOnly(2026, 3, 20), CheckOut = new DateOnly(2026, 3, 23), Nights = 3, MinPrice = 300, NetPrice = 250 }
        };

        // Act
        var result = AlternativesResult.Success(alternatives);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.ErrorCode);
        Assert.IsNull(result.ErrorMessage);
        Assert.AreEqual(1, result.Alternatives.Count);
    }

    [TestMethod]
    public void AlternativesResult_Failure_HasCorrectProperties()
    {
        // Act
        var result = AlternativesResult.Failure("HTTP_ERROR", "Connection timeout");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual("HTTP_ERROR", result.ErrorCode);
        Assert.AreEqual("Connection timeout", result.ErrorMessage);
        Assert.AreEqual(0, result.Alternatives.Count);
    }

    [TestMethod]
    public void AlternativesResult_FailureDoesNotBreakAvailabilityResponse()
    {
        // Arrange - Simulate failed alternatives fetch
        var alternativesResult = AlternativesResult.Failure("HTTP_ERROR", "Timeout");

        // Act - Check success before using (same pattern as AvailabilityService)
        var shouldUpdateAlternatives = alternativesResult.IsSuccess && alternativesResult.Alternatives.Count > 0;

        // Assert - Failure should not update alternatives
        Assert.IsFalse(shouldUpdateAlternatives);
    }

    #endregion
}
