namespace TravelBridge.Providers.WebHotelier.Models.Hotel;

/// <summary>
/// WebHotelier wire model for hotel in multi-availability response.
/// </summary>
public class WHWebHotel : WHBoardTextBase
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("minprice")]
    public decimal? MinPrice { get; set; }

    [JsonIgnore]
    public decimal? MinPricePerDay { get; set; }

    [JsonIgnore]
    public WHPartyItem? SearchParty { get; set; }

    [JsonPropertyName("salePrice")]
    public decimal? SalePrice { get; set; }

    [JsonPropertyName("photoM")]
    public string PhotoM { get; set; } = string.Empty;

    [JsonPropertyName("photoL")]
    public string PhotoL { get; set; } = string.Empty;

    [JsonPropertyName("distance")]
    public decimal? Distance { get; set; }

    [JsonPropertyName("location")]
    public WHLocation Location { get; set; } = new();

    [JsonPropertyName("type")]
    public string OriginalType { get; set; } = string.Empty;

    [JsonPropertyName("mappedTypes")]
    public HashSet<string> MappedTypes { get; set; } = new();

    [JsonPropertyName("rates")]
    public List<WHMultiRate> Rates { get; set; } = new();
}
