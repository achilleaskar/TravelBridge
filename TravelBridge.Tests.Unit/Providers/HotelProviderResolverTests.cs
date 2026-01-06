using TravelBridge.Providers.Abstractions;
using TravelBridge.Providers.Abstractions.Queries;
using TravelBridge.Providers.Abstractions.Results;

namespace TravelBridge.Tests.Unit.Providers;

public class HotelProviderResolverTests
{
    #region Test Provider Implementation

    /// <summary>
    /// Mock provider for testing purposes.
    /// </summary>
    private class MockHotelProvider : IHotelProvider
    {
        public AvailabilitySource Source { get; }
        public string Name { get; }

        public MockHotelProvider(AvailabilitySource source, string name = "Mock")
        {
            Source = source;
            Name = name;
        }

        public Task<HotelSearchResult> SearchHotelsAsync(HotelSearchQuery query, CancellationToken ct = default)
            => Task.FromResult(HotelSearchResult.Success([]));

        public Task<IEnumerable<HotelSummary>> SearchPropertiesAsync(string searchTerm, CancellationToken ct = default)
            => Task.FromResult<IEnumerable<HotelSummary>>([]);

        public Task<IEnumerable<HotelSummary>> GetAllPropertiesAsync(CancellationToken ct = default)
            => Task.FromResult<IEnumerable<HotelSummary>>([]);

        public Task<HotelAvailabilityResult> GetAvailabilityAsync(AvailabilityQuery query, CancellationToken ct = default)
            => Task.FromResult(HotelAvailabilityResult.Success(query.ProviderHotelId, [], Source));

        public Task<HotelInfoResult> GetHotelInfoAsync(HotelInfoQuery query, CancellationToken ct = default)
            => Task.FromResult(new HotelInfoResult { Source = Source, Code = query.ProviderHotelId });

        public Task<RoomInfoResult> GetRoomInfoAsync(RoomInfoQuery query, CancellationToken ct = default)
            => Task.FromResult(new RoomInfoResult { Source = Source, Code = query.RoomId });
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WhenProvidersNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new HotelProviderResolver(null!));
    }

    [Fact]
    public void Constructor_WhenNoProviders_ThrowsInvalidOperationException()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => 
            new HotelProviderResolver([]));
        
        Assert.Contains("No IHotelProvider implementations registered", exception.Message);
    }

    [Fact]
    public void Constructor_WhenProvidersProvided_CreatesResolver()
    {
        var providers = new IHotelProvider[]
        {
            new MockHotelProvider(AvailabilitySource.WebHotelier)
        };

        var resolver = new HotelProviderResolver(providers);

        Assert.NotNull(resolver);
    }

    [Fact]
    public void Constructor_WhenMultipleProviders_CreatesResolver()
    {
        var providers = new IHotelProvider[]
        {
            new MockHotelProvider(AvailabilitySource.WebHotelier, "WH"),
            new MockHotelProvider(AvailabilitySource.Owned, "Owned")
        };

        var resolver = new HotelProviderResolver(providers);

        Assert.NotNull(resolver);
    }

    #endregion

    #region GetProvider(AvailabilitySource) Tests

    [Fact]
    public void GetProvider_WhenSourceExists_ReturnsProvider()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier, "WH");
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var result = resolver.GetProvider(AvailabilitySource.WebHotelier);

        Assert.Same(webHotelierProvider, result);
    }

    [Fact]
    public void GetProvider_WhenSourceNotRegistered_ThrowsInvalidOperationException()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var exception = Assert.Throws<InvalidOperationException>(() => 
            resolver.GetProvider(AvailabilitySource.Owned));
        
        Assert.Contains("No IHotelProvider registered for source 'Owned'", exception.Message);
        Assert.Contains("Available sources:", exception.Message);
    }

    [Fact]
    public void GetProvider_WhenMultipleProviders_ReturnsCorrectOne()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier, "WH");
        var ownedProvider = new MockHotelProvider(AvailabilitySource.Owned, "Owned");
        var resolver = new HotelProviderResolver([webHotelierProvider, ownedProvider]);

        var whResult = resolver.GetProvider(AvailabilitySource.WebHotelier);
        var ownedResult = resolver.GetProvider(AvailabilitySource.Owned);

        Assert.Same(webHotelierProvider, whResult);
        Assert.Same(ownedProvider, ownedResult);
    }

    #endregion

    #region GetProvider(CompositeHotelId) Tests

    [Fact]
    public void GetProvider_WhenCompositeIdWebHotelier_ReturnsWebHotelierProvider()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);
        var hotelId = CompositeHotelId.ForWebHotelier("VAROSRESID");

        var result = resolver.GetProvider(hotelId);

        Assert.Same(webHotelierProvider, result);
    }

    [Fact]
    public void GetProvider_WhenCompositeIdOwned_ReturnsOwnedProvider()
    {
        var ownedProvider = new MockHotelProvider(AvailabilitySource.Owned);
        var resolver = new HotelProviderResolver([ownedProvider]);
        var hotelId = CompositeHotelId.ForOwned(123);

        var result = resolver.GetProvider(hotelId);

        Assert.Same(ownedProvider, result);
    }

    #endregion

    #region GetProvider(string) Tests

    [Fact]
    public void GetProvider_WhenStringNewFormat_ReturnsCorrectProvider()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var result = resolver.GetProvider("wh:VAROSRESID");

        Assert.Same(webHotelierProvider, result);
    }

    [Fact]
    public void GetProvider_WhenStringLegacyFormat_ReturnsCorrectProvider()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var result = resolver.GetProvider("1-VAROSRESID");

        Assert.Same(webHotelierProvider, result);
    }

    [Fact]
    public void GetProvider_WhenStringInvalid_ThrowsArgumentException()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        Assert.Throws<ArgumentException>(() => resolver.GetProvider("invalid"));
    }

    #endregion

    #region TryGetProvider Tests

    [Fact]
    public void TryGetProvider_WhenSourceExists_ReturnsTrueAndProvider()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var success = resolver.TryGetProvider(AvailabilitySource.WebHotelier, out var provider);

        Assert.True(success);
        Assert.Same(webHotelierProvider, provider);
    }

    [Fact]
    public void TryGetProvider_WhenSourceNotExists_ReturnsFalse()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        var success = resolver.TryGetProvider(AvailabilitySource.Owned, out var provider);

        Assert.False(success);
        Assert.Null(provider);
    }

    #endregion

    #region HasProvider Tests

    [Fact]
    public void HasProvider_WhenSourceExists_ReturnsTrue()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        Assert.True(resolver.HasProvider(AvailabilitySource.WebHotelier));
    }

    [Fact]
    public void HasProvider_WhenSourceNotExists_ReturnsFalse()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var resolver = new HotelProviderResolver([webHotelierProvider]);

        Assert.False(resolver.HasProvider(AvailabilitySource.Owned));
    }

    #endregion

    #region GetAllProviders Tests

    [Fact]
    public void GetAllProviders_ReturnsAllRegisteredProviders()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var ownedProvider = new MockHotelProvider(AvailabilitySource.Owned);
        var resolver = new HotelProviderResolver([webHotelierProvider, ownedProvider]);

        var providers = resolver.GetAllProviders().ToList();

        Assert.Equal(2, providers.Count);
        Assert.Contains(webHotelierProvider, providers);
        Assert.Contains(ownedProvider, providers);
    }

    #endregion

    #region GetRegisteredSources Tests

    [Fact]
    public void GetRegisteredSources_ReturnsAllRegisteredSources()
    {
        var webHotelierProvider = new MockHotelProvider(AvailabilitySource.WebHotelier);
        var ownedProvider = new MockHotelProvider(AvailabilitySource.Owned);
        var resolver = new HotelProviderResolver([webHotelierProvider, ownedProvider]);

        var sources = resolver.GetRegisteredSources().ToList();

        Assert.Equal(2, sources.Count);
        Assert.Contains(AvailabilitySource.WebHotelier, sources);
        Assert.Contains(AvailabilitySource.Owned, sources);
    }

    #endregion
}
