namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Query for getting alternative available dates.
/// </summary>
public sealed record AlternativesQuery
{
    /// <summary>
    /// The hotel ID.
    /// </summary>
    public required string HotelId { get; init; }

    /// <summary>
    /// Original requested check-in date.
    /// </summary>
    public required DateOnly CheckIn { get; init; }

    /// <summary>
    /// Original requested check-out date.
    /// </summary>
    public required DateOnly CheckOut { get; init; }

    /// <summary>
    /// Party configuration (rooms and guests).
    /// </summary>
    public required PartyConfiguration Party { get; init; }

    /// <summary>
    /// Number of days before/after the original dates to search.
    /// Default is 14 days.
    /// </summary>
    public int SearchRangeDays { get; init; } = 14;
}

/// <summary>
/// Result of alternative dates search.
/// </summary>
public sealed record AlternativesResult
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
    /// List of alternative available dates.
    /// </summary>
    public IReadOnlyList<AlternativeDateData> Alternatives { get; init; } = [];

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AlternativesResult Success(IReadOnlyList<AlternativeDateData> alternatives) => new()
    {
        IsSuccess = true,
        Alternatives = alternatives
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AlternativesResult Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
