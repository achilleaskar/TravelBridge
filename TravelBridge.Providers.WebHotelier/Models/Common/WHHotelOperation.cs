namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for hotel operation times.
/// </summary>
public class WHHotelOperation
{
    [JsonPropertyName("checkout_time")]
    public string CheckoutTime { get; set; } = string.Empty;

    [JsonPropertyName("checkin_time")]
    public string CheckinTime { get; set; } = string.Empty;
}
