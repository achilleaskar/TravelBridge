namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// WebHotelier wire model for children policy.
/// </summary>
public class WHChildrenPolicy
{
    [JsonPropertyName("allowed")]
    public byte Allowed { get; set; }

    [JsonPropertyName("age_from")]
    public int AgeFrom { get; set; }

    [JsonPropertyName("age_to")]
    public int AgeTo { get; set; }

    [JsonPropertyName("policy")]
    public string Policy { get; set; } = string.Empty;
}
