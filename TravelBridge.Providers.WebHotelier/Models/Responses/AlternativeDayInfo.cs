using System.Text.Json.Serialization;
using TravelBridge.Providers.WebHotelier.Models.Common;

namespace TravelBridge.Providers.WebHotelier.Models.Responses;

/// <summary>
/// WebHotelier wire model for alternative day info.
/// </summary>
public class WHAlternativeDayInfo
{
    public string date { get; set; } = string.Empty;
    public DateTime dateOnly { get; set; }
    public string status { get; set; } = string.Empty;
    public decimal price { get; set; }
    public decimal retail { get; set; }
    [JsonConverter(typeof(WHStringToIntJsonConverter))]
    public int min_stay { get; set; }
}
