namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Query for room static information.
/// </summary>
/// <param name="ProviderHotelId">Hotel ID in the provider's format</param>
/// <param name="RoomId">Room type ID in the provider's format</param>
public record RoomInfoQuery(string ProviderHotelId, string RoomId);
