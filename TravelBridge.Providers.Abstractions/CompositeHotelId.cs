namespace TravelBridge.Providers.Abstractions;

/// <summary>
/// Value object representing a composite hotel ID in format "source:providerHotelId".
/// Examples: "wh:VAROSRESID", "owned:123"
/// 
/// This format is safer than "sourceId-hotelId" because:
/// 1. Splits only on first ":" (hotel IDs can contain "-")
/// 2. Uses text prefix instead of numeric ID (more readable)
/// </summary>
public readonly struct CompositeHotelId : IEquatable<CompositeHotelId>
{
    /// <summary>
    /// Prefix for WebHotelier hotels.
    /// </summary>
    public const string WebHotelierPrefix = "wh";

    /// <summary>
    /// Prefix for owned/managed hotels.
    /// </summary>
    public const string OwnedPrefix = "owned";

    /// <summary>
    /// Separator between source and provider hotel ID.
    /// </summary>
    public const char Separator = ':';

    /// <summary>
    /// The availability source (which provider this hotel belongs to).
    /// </summary>
    public AvailabilitySource Source { get; }

    /// <summary>
    /// The hotel ID in the provider's native format.
    /// For WebHotelier: "VAROSRESID"
    /// For Owned: "123" (database ID)
    /// </summary>
    public string ProviderHotelId { get; }

    private CompositeHotelId(AvailabilitySource source, string providerHotelId)
    {
        Source = source;
        ProviderHotelId = providerHotelId;
    }

    /// <summary>
    /// Creates a composite ID for a WebHotelier hotel.
    /// </summary>
    public static CompositeHotelId ForWebHotelier(string providerHotelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerHotelId);
        return new CompositeHotelId(AvailabilitySource.WebHotelier, providerHotelId);
    }

    /// <summary>
    /// Creates a composite ID for an owned hotel.
    /// </summary>
    public static CompositeHotelId ForOwned(string providerHotelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerHotelId);
        return new CompositeHotelId(AvailabilitySource.Owned, providerHotelId);
    }

    /// <summary>
    /// Creates a composite ID for an owned hotel using its database ID.
    /// </summary>
    public static CompositeHotelId ForOwned(int databaseId)
    {
        return new CompositeHotelId(AvailabilitySource.Owned, databaseId.ToString());
    }

    /// <summary>
    /// Parses a composite hotel ID string.
    /// Supported formats:
    /// - "wh:VAROSRESID" (WebHotelier)
    /// - "owned:123" (Owned)
    /// - "1-VAROSRESID" (legacy format, assumes 1=WebHotelier)
    /// </summary>
    /// <param name="compositeId">The composite ID string to parse</param>
    /// <returns>Parsed CompositeHotelId</returns>
    /// <exception cref="ArgumentException">If the format is invalid</exception>
    public static CompositeHotelId Parse(string compositeId)
    {
        if (string.IsNullOrWhiteSpace(compositeId))
            throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(compositeId));

        // Try new format first: "source:providerHotelId"
        var colonIndex = compositeId.IndexOf(Separator);
        if (colonIndex > 0 && colonIndex < compositeId.Length - 1)
        {
            var prefix = compositeId[..colonIndex].ToLowerInvariant();
            var providerHotelId = compositeId[(colonIndex + 1)..];

            return prefix switch
            {
                WebHotelierPrefix => ForWebHotelier(providerHotelId),
                OwnedPrefix => ForOwned(providerHotelId),
                _ => throw new ArgumentException($"Unknown hotel ID prefix: '{prefix}'. Expected '{WebHotelierPrefix}' or '{OwnedPrefix}'.", nameof(compositeId))
            };
        }

        // Try legacy format: "sourceId-providerHotelId" (split on first dash only)
        var dashIndex = compositeId.IndexOf('-');
        if (dashIndex > 0 && dashIndex < compositeId.Length - 1)
        {
            var sourceIdStr = compositeId[..dashIndex];
            var providerHotelId = compositeId[(dashIndex + 1)..];

            if (int.TryParse(sourceIdStr, out var sourceId))
            {
                var source = sourceId switch
                {
                    0 => AvailabilitySource.Owned,
                    1 => AvailabilitySource.WebHotelier,
                    _ => throw new ArgumentException($"Unknown legacy source ID: {sourceId}", nameof(compositeId))
                };

                return new CompositeHotelId(source, providerHotelId);
            }
        }

        throw new ArgumentException(
            $"Invalid hotel ID format: '{compositeId}'. Expected 'wh:HOTELID', 'owned:ID', or legacy '1-HOTELID' format.",
            nameof(compositeId));
    }

    /// <summary>
    /// Tries to parse a composite hotel ID string.
    /// </summary>
    /// <param name="compositeId">The composite ID string to parse</param>
    /// <param name="result">The parsed result if successful</param>
    /// <returns>True if parsing succeeded</returns>
    public static bool TryParse(string? compositeId, out CompositeHotelId result)
    {
        result = default;

        if (string.IsNullOrWhiteSpace(compositeId))
            return false;

        try
        {
            result = Parse(compositeId);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the string representation: "source:providerHotelId".
    /// </summary>
    public override string ToString()
    {
        var prefix = Source switch
        {
            AvailabilitySource.WebHotelier => WebHotelierPrefix,
            AvailabilitySource.Owned => OwnedPrefix,
            _ => throw new InvalidOperationException($"Unknown source: {Source}")
        };

        return $"{prefix}{Separator}{ProviderHotelId}";
    }

    /// <summary>
    /// Returns the legacy format string: "sourceId-providerHotelId".
    /// Used for backward compatibility with existing URLs/data.
    /// </summary>
    public string ToLegacyString()
    {
        var sourceId = Source switch
        {
            AvailabilitySource.Owned => 0,
            AvailabilitySource.WebHotelier => 1,
            _ => throw new InvalidOperationException($"Unknown source: {Source}")
        };

        return $"{sourceId}-{ProviderHotelId}";
    }

    #region Equality

    public bool Equals(CompositeHotelId other)
    {
        return Source == other.Source && ProviderHotelId == other.ProviderHotelId;
    }

    public override bool Equals(object? obj)
    {
        return obj is CompositeHotelId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Source, ProviderHotelId);
    }

    public static bool operator ==(CompositeHotelId left, CompositeHotelId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CompositeHotelId left, CompositeHotelId right)
    {
        return !left.Equals(right);
    }

    #endregion
}
