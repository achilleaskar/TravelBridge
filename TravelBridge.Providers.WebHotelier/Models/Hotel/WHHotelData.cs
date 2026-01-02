namespace TravelBridge.Providers.WebHotelier.Models.Hotel;

/// <summary>
/// WebHotelier wire model for hotel data (property details).
/// </summary>
public class WHHotelData : WHBoardTextBase
{
    private decimal salePrice;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public WHProvider Provider { get; set; }

    public decimal MinPrice { get; set; }

    public decimal SalePrice
    {
        get => salePrice;
        set
        {
            if (value > MinPrice)
                salePrice = value;
            else
                salePrice = 0;
        }
    }

    public string CustomInfo { get; set; } = string.Empty;

    [JsonPropertyName("mappedTypes")]
    public HashSet<string> MappedTypes { get; set; } = [];

    public decimal MinPricePerNight { get; set; }

    public string Id => $"{(int)Provider}-{Code}";

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("rating")]
    public int Rating { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public WHLocationInfo Location { get; set; } = new();

    [JsonPropertyName("children")]
    public WHChildrenPolicy Children { get; set; } = new();

    [JsonPropertyName("operation")]
    public WHHotelOperation Operation { get; set; } = new();

    [JsonPropertyName("facilities")]
    public IEnumerable<string> Facilities { get; set; } = [];

    [JsonPropertyName("photos")]
    public IEnumerable<WHPhotoInfo> PhotosItems { get; set; } = [];

    public IEnumerable<string> LargePhotos { get; set; } = [];

    public void SetBoardText()
    {
        if (Boards == null || Boards.Count == 0)
        {
            BoardsText = "";
            HasBoards = false;
            return;
        }

        bool hasRoomOnly = Boards.Any(b => b.Id == 14);
        if (hasRoomOnly && Boards.Count == 1)
        {
            Boards.First().Name = "Χωρίς επιλογές διατροφής";
        }

        if (Boards.Count == 1)
        {
            BoardsText = "Διατροφή:";
            HasBoards = true;
            return;
        }

        if (hasRoomOnly)
        {
            BoardsText = "Επιλογές Διατροφής:";
            HasBoards = true;
            Boards.RemoveAll(b => b.Id == 14);
            return;
        }

        if (Boards.Count > 1)
        {
            BoardsText = "Επιλογές Διατροφής:";
            HasBoards = true;
            return;
        }
    }
}
