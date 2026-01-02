namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for alternative dates.
/// </summary>
public class WHAlternative
{
    public DateTime CheckIn { get; set; }
    public DateTime Checkout { get; set; }
    public decimal MinPrice { get; set; }
    [JsonIgnore]
    public decimal NetPrice { get; set; }
    public int Nights { get; set; }
}
