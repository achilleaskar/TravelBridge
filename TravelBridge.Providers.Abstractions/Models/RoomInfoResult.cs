namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Provider-neutral result for room information.
/// </summary>
public sealed record RoomInfoResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Room data if the operation succeeded.
    /// </summary>
    public RoomInfoData? Data { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static RoomInfoResult Success(RoomInfoData data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static RoomInfoResult Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Provider-neutral room information data.
/// </summary>
public sealed record RoomInfoData
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public RoomCapacityData? Capacity { get; init; }
    public IReadOnlyList<string> Amenities { get; init; } = [];
    public IReadOnlyList<string> LargePhotos { get; init; } = [];
    public IReadOnlyList<string> MediumPhotos { get; init; } = [];
}

/// <summary>
/// Provider-neutral room capacity data.
/// </summary>
public sealed record RoomCapacityData
{
    public int MinPersons { get; init; }
    public int MaxPersons { get; init; }
    public int MaxAdults { get; init; }
    public int MaxInfants { get; init; }
    public bool ChildrenAllowed { get; init; }
}
