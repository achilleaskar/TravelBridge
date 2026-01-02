namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire response for properties list.
/// </summary>
public class WHPropertiesResponse
{
    public WHPropertiesData? data { get; set; }
}

/// <summary>
/// WebHotelier wire model for properties data.
/// </summary>
public class WHPropertiesData
{
    public WHHotel[]? hotels { get; set; }
}

/// <summary>
/// WebHotelier wire model for hotel in properties list.
/// </summary>
public class WHHotel
{
    public string code { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string type { get; set; } = string.Empty;
    public WHHotelLocation? location { get; set; }
}

/// <summary>
/// WebHotelier wire model for hotel location in properties list.
/// </summary>
public class WHHotelLocation
{
    public string name { get; set; } = string.Empty;
    public string country { get; set; } = string.Empty;
}
