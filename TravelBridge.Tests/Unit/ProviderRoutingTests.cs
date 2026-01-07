using Microsoft.VisualStudio.TestTools.UnitTesting;
using TravelBridge.API.Providers;
using TravelBridge.Providers.Abstractions;

namespace TravelBridge.Tests.Unit;

/// <summary>
/// Smoke tests for provider routing in endpoints.
/// Tests that provider resolution correctly handles supported and unsupported providers.
/// </summary>
[TestClass]
public class ProviderRoutingTests
{
    #region ProviderRoutingHelper Tests

    [TestMethod]
    public void TryResolveProvider_ValidWebHotelierId_ReturnsTrue()
    {
        // Arrange
        var compositeId = "1-TESTHOTEL";
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier);

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(ProviderIds.WebHotelier, id.ProviderId);
        Assert.AreEqual("TESTHOTEL", id.Value);
        Assert.IsNotNull(provider);
        Assert.IsNull(error);
    }

    [TestMethod]
    public void TryResolveProvider_UnsupportedProviderId_ReturnsFalse()
    {
        // Arrange - Provider 0 (Owned) is not registered
        var compositeId = "0-TESTHOTEL";
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier); // Only WH registered

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void TryResolveProvider_InvalidFormat_ReturnsFalse()
    {
        // Arrange - Invalid format (no dash)
        var compositeId = "INVALIDFORMAT";
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier);

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void TryResolveProvider_EmptyString_ReturnsFalse()
    {
        // Arrange
        var compositeId = "";
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier);

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void TryResolveProvider_NullString_ReturnsFalse()
    {
        // Arrange
        string? compositeId = null;
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier);

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
        Assert.IsNotNull(error);
    }

    [TestMethod]
    public void TryResolveProvider_NonNumericProviderId_ReturnsFalse()
    {
        // Arrange - Invalid format (non-numeric provider ID)
        var compositeId = "abc-TESTHOTEL";
        var mockResolver = new TestProviderResolver(ProviderIds.WebHotelier);

        // Act
        var result = ProviderRoutingHelper.TryResolveProvider(
            compositeId, 
            mockResolver, 
            out var id, 
            out var provider, 
            out var error);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(provider);
        Assert.IsNotNull(error);
    }

    #endregion

    #region Helper Classes

    /// <summary>
    /// Test provider resolver that only supports specified provider IDs.
    /// </summary>
    private class TestProviderResolver : IHotelProviderResolver
    {
        private readonly HashSet<int> _supportedProviderIds;

        public TestProviderResolver(params int[] supportedProviderIds)
        {
            _supportedProviderIds = new HashSet<int>(supportedProviderIds);
        }

        public IHotelProvider GetRequired(int providerId)
        {
            if (!_supportedProviderIds.Contains(providerId))
                throw new NotSupportedException($"Provider {providerId} not supported");
            return new FakeHotelProvider(providerId);
        }

        public bool TryGet(int providerId, out IHotelProvider? provider)
        {
            if (_supportedProviderIds.Contains(providerId))
            {
                provider = new FakeHotelProvider(providerId);
                return true;
            }
            provider = null;
            return false;
        }

        public IEnumerable<IHotelProvider> GetAll()
        {
            return _supportedProviderIds.Select(id => new FakeHotelProvider(id));
        }
    }

    /// <summary>
    /// Fake hotel provider for testing.
    /// </summary>
    private class FakeHotelProvider : IHotelProvider
    {
        public FakeHotelProvider(int providerId)
        {
            ProviderId = providerId;
        }

        public int ProviderId { get; }

        public Task<Providers.Abstractions.Models.HotelInfoResult> GetHotelInfoAsync(
            Providers.Abstractions.Models.HotelInfoQuery query, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Providers.Abstractions.Models.RoomInfoResult> GetRoomInfoAsync(
            Providers.Abstractions.Models.RoomInfoQuery query, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Providers.Abstractions.Models.HotelAvailabilityResult> GetHotelAvailabilityAsync(
            Providers.Abstractions.Models.HotelAvailabilityQuery query, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Providers.Abstractions.Models.SearchAvailabilityResult> SearchAvailabilityAsync(
            Providers.Abstractions.Models.SearchAvailabilityQuery query, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Providers.Abstractions.Models.AlternativesResult> GetAlternativesAsync(
            Providers.Abstractions.Models.AlternativesQuery query, 
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
}
