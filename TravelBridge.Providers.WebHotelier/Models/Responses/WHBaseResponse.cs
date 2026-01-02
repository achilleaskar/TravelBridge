namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// Base response model for WebHotelier API responses.
/// </summary>
public class WHBaseResponse
{
    [JsonPropertyName("http_code")]
    public int HttpCode { get; set; }

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("error_msg")]
    public string ErrorMessage { get; set; } = string.Empty;
}
