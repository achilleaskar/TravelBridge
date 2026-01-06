namespace TravelBridge.Providers.Abstractions.Models;

/// <summary>
/// Provider-neutral result for hotel information.
/// This is a data-only model - the API layer maps this to JSON contract types.
/// </summary>
public sealed record HotelInfoResult
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
    /// Hotel data if the operation succeeded.
    /// </summary>
    public HotelInfoData? Data { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static HotelInfoResult Success(HotelInfoData data) => new()
    {
        IsSuccess = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static HotelInfoResult Failure(string errorCode, string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Provider-neutral hotel information data.
/// </summary>
public sealed record HotelInfoData
{
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Type { get; init; }
    public int Rating { get; init; }
    public string? Description { get; init; }
    public HotelLocationData? Location { get; init; }
    public HotelOperationData? Operation { get; init; }
    public ChildrenPolicyData? ChildrenPolicy { get; init; }
    public IReadOnlyList<string> Facilities { get; init; } = [];
    public IReadOnlyList<string> LargePhotos { get; init; } = [];
}

/// <summary>
/// Provider-neutral location data.
/// </summary>
public sealed record HotelLocationData
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Address { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Region { get; init; }
    public string? Country { get; init; }
}

/// <summary>
/// Provider-neutral hotel operation data.
/// </summary>
public sealed record HotelOperationData
{
    public string? CheckinTime { get; init; }
    public string? CheckoutTime { get; init; }
}

/// <summary>
/// Provider-neutral children policy data.
/// </summary>
public sealed record ChildrenPolicyData
{
    public int MaxChildAge { get; init; }
    public int InfantAge { get; init; }
}
