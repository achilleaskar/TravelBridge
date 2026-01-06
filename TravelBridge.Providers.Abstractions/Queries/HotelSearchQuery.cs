namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Query for multi-hotel search by location.
/// Provider-neutral - maps from API request at endpoint, maps to provider-specific request in provider.
/// </summary>
/// <param name="CheckIn">Check-in date</param>
/// <param name="CheckOut">Check-out date</param>
/// <param name="Parties">Room configurations (adults + children per room)</param>
/// <param name="Location">Bounding box for geographic search</param>
/// <param name="CenterLatitude">Center point latitude for distance calculations</param>
/// <param name="CenterLongitude">Center point longitude for distance calculations</param>
/// <param name="SortBy">Sort field (e.g., "PRICE", "DISTANCE", "POPULARITY")</param>
/// <param name="SortOrder">Sort direction ("ASC" or "DESC")</param>
public record HotelSearchQuery(
    DateOnly CheckIn,
    DateOnly CheckOut,
    List<PartyConfiguration> Parties,
    BoundingBox Location,
    string CenterLatitude,
    string CenterLongitude,
    string? SortBy = null,
    string? SortOrder = null
);
