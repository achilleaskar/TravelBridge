namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Query for hotel static information.
/// </summary>
/// <param name="ProviderHotelId">Hotel ID in the provider's format</param>
public record HotelInfoQuery(string ProviderHotelId);
