namespace TravelBridge.Contracts.Common
{
    public class WebHotel : BoardTextBase
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("rating")]
        public int? Rating { get; set; }

        [JsonPropertyName("minprice")]
        public decimal? MinPrice { get; set; }

        [JsonIgnore]
        public decimal? MinPricePerDay { get; set; }

        [JsonIgnore]
        public PartyItem SearchParty { get; set; }

        [JsonPropertyName("salePrice")]
        public decimal? SalePrice { get; set; }

        [JsonPropertyName("photoM")]
        public string PhotoM { get; set; }

        [JsonPropertyName("photoL")]
        public string PhotoL { get; set; }

        [JsonPropertyName("distance")]
        public decimal? Distance { get; set; }

        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("type")]
        public string OriginalType { get; set; }

        [JsonPropertyName("mappedTypes")]
        public HashSet<string> MappedTypes { get; set; }

        [JsonPropertyName("rates")]
        public List<MultiRate> Rates { get; set; }
    }

    //public class Payment
    //{
    //    [JsonPropertyName("due")]
    //    public DateTime? Due { get; set; }

    //    [JsonPropertyName("amount")]
    //    public decimal? Amount { get; set; }
    //}


}
