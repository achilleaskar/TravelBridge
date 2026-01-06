using TravelBridge.Providers.Abstractions;

namespace TravelBridge.Tests.Unit.Providers;

public class CompositeHotelIdTests
{
    #region Parse - New Format Tests

    [Theory]
    [InlineData("wh:VAROSRESID", AvailabilitySource.WebHotelier, "VAROSRESID")]
    [InlineData("wh:HOTEL123", AvailabilitySource.WebHotelier, "HOTEL123")]
    [InlineData("owned:123", AvailabilitySource.Owned, "123")]
    [InlineData("owned:456", AvailabilitySource.Owned, "456")]
    public void Parse_WhenNewFormatValid_ReturnsCorrectSourceAndId(string input, AvailabilitySource expectedSource, string expectedProviderId)
    {
        var result = CompositeHotelId.Parse(input);

        Assert.Equal(expectedSource, result.Source);
        Assert.Equal(expectedProviderId, result.ProviderHotelId);
    }

    [Theory]
    [InlineData("WH:VAROSRESID", AvailabilitySource.WebHotelier, "VAROSRESID")]
    [InlineData("OWNED:123", AvailabilitySource.Owned, "123")]
    [InlineData("Wh:TEST", AvailabilitySource.WebHotelier, "TEST")]
    public void Parse_WhenNewFormatCaseInsensitive_ReturnsCorrectResult(string input, AvailabilitySource expectedSource, string expectedProviderId)
    {
        var result = CompositeHotelId.Parse(input);

        Assert.Equal(expectedSource, result.Source);
        Assert.Equal(expectedProviderId, result.ProviderHotelId);
    }

    [Fact]
    public void Parse_WhenHotelIdContainsDash_ParsesCorrectly()
    {
        var result = CompositeHotelId.Parse("wh:HOTEL-WITH-DASHES");

        Assert.Equal(AvailabilitySource.WebHotelier, result.Source);
        Assert.Equal("HOTEL-WITH-DASHES", result.ProviderHotelId);
    }

    [Fact]
    public void Parse_WhenHotelIdContainsColon_ParsesOnlyFirstColon()
    {
        var result = CompositeHotelId.Parse("wh:HOTEL:WITH:COLONS");

        Assert.Equal(AvailabilitySource.WebHotelier, result.Source);
        Assert.Equal("HOTEL:WITH:COLONS", result.ProviderHotelId);
    }

    #endregion

    #region Parse - Legacy Format Tests

    [Theory]
    [InlineData("1-VAROSRESID", AvailabilitySource.WebHotelier, "VAROSRESID")]
    [InlineData("1-HOTEL123", AvailabilitySource.WebHotelier, "HOTEL123")]
    [InlineData("0-123", AvailabilitySource.Owned, "123")]
    [InlineData("0-456", AvailabilitySource.Owned, "456")]
    public void Parse_WhenLegacyFormatValid_ReturnsCorrectSourceAndId(string input, AvailabilitySource expectedSource, string expectedProviderId)
    {
        var result = CompositeHotelId.Parse(input);

        Assert.Equal(expectedSource, result.Source);
        Assert.Equal(expectedProviderId, result.ProviderHotelId);
    }

    [Fact]
    public void Parse_WhenLegacyHotelIdContainsDash_ParsesOnlyFirstDash()
    {
        var result = CompositeHotelId.Parse("1-HOTEL-WITH-DASHES");

        Assert.Equal(AvailabilitySource.WebHotelier, result.Source);
        Assert.Equal("HOTEL-WITH-DASHES", result.ProviderHotelId);
    }

    #endregion

    #region Parse - Invalid Input Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_WhenInputNullOrEmpty_ThrowsArgumentException(string? input)
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse(input!));
        Assert.Contains("null or empty", exception.Message);
    }

    [Fact]
    public void Parse_WhenUnknownPrefix_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse("unknown:HOTELID"));
        Assert.Contains("Unknown hotel ID prefix", exception.Message);
    }

    [Fact]
    public void Parse_WhenInvalidFormat_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse("invalidformat"));
        Assert.Contains("Invalid hotel ID format", exception.Message);
    }

    [Fact]
    public void Parse_WhenColonOnly_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse("wh:"));
        Assert.Contains("Invalid hotel ID format", exception.Message);
    }

    [Fact]
    public void Parse_WhenDashOnly_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse("1-"));
        Assert.Contains("Invalid hotel ID format", exception.Message);
    }

    [Fact]
    public void Parse_WhenUnknownLegacySourceId_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompositeHotelId.Parse("99-HOTELID"));
        Assert.Contains("Unknown legacy source ID", exception.Message);
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParse_WhenValidInput_ReturnsTrueAndSetsResult()
    {
        var success = CompositeHotelId.TryParse("wh:VAROSRESID", out var result);

        Assert.True(success);
        Assert.Equal(AvailabilitySource.WebHotelier, result.Source);
        Assert.Equal("VAROSRESID", result.ProviderHotelId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("unknown:test")]
    public void TryParse_WhenInvalidInput_ReturnsFalse(string? input)
    {
        var success = CompositeHotelId.TryParse(input, out var result);

        Assert.False(success);
        Assert.Equal(default, result);
    }

    #endregion

    #region Factory Methods Tests

    [Fact]
    public void ForWebHotelier_CreatesCorrectCompositeId()
    {
        var result = CompositeHotelId.ForWebHotelier("VAROSRESID");

        Assert.Equal(AvailabilitySource.WebHotelier, result.Source);
        Assert.Equal("VAROSRESID", result.ProviderHotelId);
    }

    [Fact]
    public void ForOwned_WithStringId_CreatesCorrectCompositeId()
    {
        var result = CompositeHotelId.ForOwned("123");

        Assert.Equal(AvailabilitySource.Owned, result.Source);
        Assert.Equal("123", result.ProviderHotelId);
    }

    [Fact]
    public void ForOwned_WithIntId_CreatesCorrectCompositeId()
    {
        var result = CompositeHotelId.ForOwned(456);

        Assert.Equal(AvailabilitySource.Owned, result.Source);
        Assert.Equal("456", result.ProviderHotelId);
    }

    [Fact]
    public void ForWebHotelier_WhenNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CompositeHotelId.ForWebHotelier(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ForWebHotelier_WhenEmptyOrWhitespace_ThrowsArgumentException(string? input)
    {
        Assert.Throws<ArgumentException>(() => CompositeHotelId.ForWebHotelier(input!));
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_WhenWebHotelier_ReturnsNewFormat()
    {
        var id = CompositeHotelId.ForWebHotelier("VAROSRESID");

        Assert.Equal("wh:VAROSRESID", id.ToString());
    }

    [Fact]
    public void ToString_WhenOwned_ReturnsNewFormat()
    {
        var id = CompositeHotelId.ForOwned(123);

        Assert.Equal("owned:123", id.ToString());
    }

    #endregion

    #region ToLegacyString Tests

    [Fact]
    public void ToLegacyString_WhenWebHotelier_ReturnsLegacyFormat()
    {
        var id = CompositeHotelId.ForWebHotelier("VAROSRESID");

        Assert.Equal("1-VAROSRESID", id.ToLegacyString());
    }

    [Fact]
    public void ToLegacyString_WhenOwned_ReturnsLegacyFormat()
    {
        var id = CompositeHotelId.ForOwned(123);

        Assert.Equal("0-123", id.ToLegacyString());
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WhenSameSourceAndId_ReturnsTrue()
    {
        var id1 = CompositeHotelId.ForWebHotelier("VAROSRESID");
        var id2 = CompositeHotelId.ForWebHotelier("VAROSRESID");

        Assert.True(id1.Equals(id2));
        Assert.True(id1 == id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void Equals_WhenDifferentSource_ReturnsFalse()
    {
        var id1 = CompositeHotelId.ForWebHotelier("123");
        var id2 = CompositeHotelId.ForOwned("123");

        Assert.False(id1.Equals(id2));
        Assert.True(id1 != id2);
    }

    [Fact]
    public void Equals_WhenDifferentProviderId_ReturnsFalse()
    {
        var id1 = CompositeHotelId.ForWebHotelier("HOTEL1");
        var id2 = CompositeHotelId.ForWebHotelier("HOTEL2");

        Assert.False(id1.Equals(id2));
        Assert.True(id1 != id2);
    }

    #endregion

    #region Round-Trip Tests

    [Theory]
    [InlineData("wh:VAROSRESID")]
    [InlineData("owned:123")]
    [InlineData("wh:HOTEL-WITH-DASHES")]
    public void Parse_AndToString_RoundTrips(string input)
    {
        var parsed = CompositeHotelId.Parse(input);
        var output = parsed.ToString();

        Assert.Equal(input, output);
    }

    [Fact]
    public void Parse_Legacy_AndToLegacyString_RoundTrips()
    {
        var parsed = CompositeHotelId.Parse("1-VAROSRESID");
        var output = parsed.ToLegacyString();

        Assert.Equal("1-VAROSRESID", output);
    }

    #endregion
}
