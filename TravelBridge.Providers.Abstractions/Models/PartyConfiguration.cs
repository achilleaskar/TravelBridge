namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Represents a room's party configuration (guests).
/// </summary>
public sealed record PartyRoom
{
    /// <summary>
    /// Number of adults in the room.
    /// </summary>
    public required int Adults { get; init; }

    /// <summary>
    /// Ages of children in the room. Empty array if no children.
    /// </summary>
    public int[] ChildrenAges { get; init; } = [];
}

/// <summary>
/// Represents the party configuration for an availability search.
/// </summary>
public sealed record PartyConfiguration
{
    /// <summary>
    /// The list of rooms with their guest configurations.
    /// </summary>
    public required IReadOnlyList<PartyRoom> Rooms { get; init; }

    /// <summary>
    /// Total number of rooms in the party.
    /// </summary>
    public int RoomCount => Rooms.Count;

    /// <summary>
    /// Total number of adults across all rooms.
    /// </summary>
    public int TotalAdults => Rooms.Sum(r => r.Adults);

    /// <summary>
    /// Total number of children across all rooms.
    /// </summary>
    public int TotalChildren => Rooms.Sum(r => r.ChildrenAges.Length);
}
