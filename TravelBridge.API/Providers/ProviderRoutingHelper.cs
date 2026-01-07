using TravelBridge.Providers.Abstractions;

namespace TravelBridge.API.Providers;

/// <summary>
/// Helper methods for provider routing in API endpoints.
/// Provides consistent error handling for composite ID parsing and provider resolution.
/// </summary>
public static class ProviderRoutingHelper
{
    /// <summary>
    /// Attempts to parse a composite hotel ID and resolve the provider.
    /// Returns consistent 400 BadRequest errors for invalid format or unsupported providers.
    /// </summary>
    /// <param name="compositeHotelId">The composite hotel ID (e.g., "1-VAROSRESID").</param>
    /// <param name="resolver">The provider resolver.</param>
    /// <param name="id">When successful, contains the parsed CompositeId.</param>
    /// <param name="provider">When successful, contains the resolved provider.</param>
    /// <param name="error">When failed, contains the IResult error to return.</param>
    /// <returns>true if parsing and resolution succeeded; otherwise, false.</returns>
    public static bool TryResolveProvider(
        string? compositeHotelId,
        IHotelProviderResolver resolver,
        out CompositeId id,
        out IHotelProvider? provider,
        out IResult? error)
    {
        id = default;
        provider = null;
        error = null;

        // Validate and parse composite ID
        if (string.IsNullOrWhiteSpace(compositeHotelId))
        {
            error = Results.BadRequest(new { error = "Hotel ID cannot be null or empty." });
            return false;
        }

        if (!CompositeId.TryParse(compositeHotelId, out id))
        {
            error = Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
            return false;
        }

        // Resolve provider
        if (!resolver.TryGet(id.ProviderId, out provider))
        {
            error = Results.BadRequest(new { error = $"Hotel provider '{id.ProviderId}' is not supported." });
            return false;
        }

        return true;
    }

    /// <summary>
    /// Creates a 400 BadRequest result for invalid composite ID format.
    /// </summary>
    public static IResult InvalidHotelIdFormatError()
    {
        return Results.BadRequest(new { error = "Invalid hotel id format. Expected '{providerId}-{hotelId}'." });
    }

    /// <summary>
    /// Creates a 400 BadRequest result for unsupported provider.
    /// </summary>
    public static IResult UnsupportedProviderError(int providerId)
    {
        return Results.BadRequest(new { error = $"Hotel provider '{providerId}' is not supported." });
    }

    /// <summary>
    /// Creates a 400 BadRequest result for null/empty hotel ID.
    /// </summary>
    public static IResult EmptyHotelIdError()
    {
        return Results.BadRequest(new { error = "Hotel ID cannot be null or empty." });
    }
}
