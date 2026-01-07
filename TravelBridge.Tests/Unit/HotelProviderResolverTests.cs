using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using TravelBridge.API.Providers;
using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Models;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Unit tests for HotelProviderResolver.
/// </summary>
[TestClass]
public class HotelProviderResolverTests
{
    private Mock<ILogger<HotelProviderResolver>> _mockLogger = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<HotelProviderResolver>>();
    }

    #region GetRequired Tests

    [TestMethod]
    public void GetRequired_RegisteredProvider_ReturnsProvider()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act
        var result = resolver.GetRequired(ProviderIds.WebHotelier);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(ProviderIds.WebHotelier, result.ProviderId);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void GetRequired_UnregisteredProvider_ThrowsNotSupportedException()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act - Should throw for provider ID 0 (Owned, not registered)
        resolver.GetRequired(ProviderIds.Owned);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void GetRequired_UnknownProviderId_ThrowsNotSupportedException()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act - Should throw for unknown provider ID 99
        resolver.GetRequired(99);
    }

    #endregion

    #region TryGet Tests

    [TestMethod]
    public void TryGet_RegisteredProvider_ReturnsTrue()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act
        var result = resolver.TryGet(ProviderIds.WebHotelier, out var provider);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(provider);
        Assert.AreEqual(ProviderIds.WebHotelier, provider.ProviderId);
    }

    [TestMethod]
    public void TryGet_UnregisteredProvider_ReturnsFalse()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act
        var result = resolver.TryGet(ProviderIds.Owned, out var provider);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
    }

    [TestMethod]
    public void TryGet_UnknownProviderId_ReturnsFalse()
    {
        // Arrange
        var mockProvider = CreateMockProvider(ProviderIds.WebHotelier);
        var resolver = new HotelProviderResolver([mockProvider.Object], _mockLogger.Object);

        // Act
        var result = resolver.TryGet(99, out var provider);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
    }

    #endregion

    #region GetAll Tests

    [TestMethod]
    public void GetAll_MultipleProviders_ReturnsAllProviders()
    {
        // Arrange
        var provider1 = CreateMockProvider(ProviderIds.WebHotelier);
        var provider2 = CreateMockProvider(ProviderIds.Owned);
        var resolver = new HotelProviderResolver([provider1.Object, provider2.Object], _mockLogger.Object);

        // Act
        var result = resolver.GetAll().ToList();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(p => p.ProviderId == ProviderIds.WebHotelier));
        Assert.IsTrue(result.Any(p => p.ProviderId == ProviderIds.Owned));
    }

    [TestMethod]
    public void GetAll_NoProviders_ReturnsEmptyCollection()
    {
        // Arrange
        var resolver = new HotelProviderResolver([], _mockLogger.Object);

        // Act
        var result = resolver.GetAll().ToList();

        // Assert
        Assert.AreEqual(0, result.Count);
    }

    #endregion

    #region Duplicate Registration Tests

    [TestMethod]
    public void Constructor_DuplicateProviderId_KeepsFirstRegistration()
    {
        // Arrange
        var provider1 = CreateMockProvider(ProviderIds.WebHotelier);
        var provider2 = CreateMockProvider(ProviderIds.WebHotelier); // Same ID

        // Act
        var resolver = new HotelProviderResolver([provider1.Object, provider2.Object], _mockLogger.Object);
        var result = resolver.GetAll().ToList();

        // Assert - Should only have one provider
        Assert.AreEqual(1, result.Count);
    }

    #endregion

    #region Helper Methods

    private static Mock<IHotelProvider> CreateMockProvider(int providerId)
    {
        var mock = new Mock<IHotelProvider>();
        mock.Setup(p => p.ProviderId).Returns(providerId);
        return mock;
    }

    #endregion
}
