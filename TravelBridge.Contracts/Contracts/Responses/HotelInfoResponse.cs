using System.Text.Json.Serialization;
using TravelBridge.Contracts.Models.Hotels;

namespace TravelBridge.Contracts.Contracts.Responses;

/// <summary>
/// Response wrapper for hotel info endpoint to match production API contract.
/// </summary>
public class HotelInfoResponse
{
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("error_msg")]
    public string? ErrorMsg { get; set; }

    [JsonPropertyName("data")]
    public HotelData? Data { get; set; }
}
