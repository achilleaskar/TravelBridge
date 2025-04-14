using System.Text.Json.Serialization;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class MultiAvailabilityResponse : BaseWebHotelierResponse
    {
        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("hotels")]
        public IEnumerable<WebHotel> Hotels { get; set; }
    }

    public class WebHotel : BoardTextBase
    {
        [JsonPropertyName("code")]
        public string Code { internal get; set; }

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

    public class Board
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("lat")]
        public decimal? Latitude { get; set; }

        [JsonPropertyName("lon")]
        public decimal? Longitude { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }
    }

    public class MultiRate : BaseRate
    {
        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("retail")]
        public decimal? Retail { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("margin")]
        public decimal? Margin { get; set; }

        [JsonIgnore]
        public PartyItem? SearchParty { get; set; }

        //[JsonPropertyName("id")]
        //public int Id { get; set; }

        //[JsonPropertyName("url")]
        //public string Url { get; set; }

        //[JsonPropertyName("roomurl")]
        //public string RoomUrl { get; set; }

        [JsonPropertyName("remaining")]
        public int? Remaining { get; set; }

        //[JsonPropertyName("rate_desc")]
        //public string RateDescription { get; set; }

        //[JsonPropertyName("photo")]
        //public string Photo { get; set; }

        //[JsonPropertyName("photoM")]
        //public string PhotoM { get; set; }

        //[JsonPropertyName("photoL")]
        //public string PhotoL { get; set; }

        //[JsonPropertyName("stay")]
        //public decimal? Stay { get; set; }

        //[JsonPropertyName("extras")]
        //public decimal? Extras { get; set; }

        //[JsonPropertyName("taxes")]
        //public decimal? Taxes { get; set; }

        //[JsonPropertyName("excluded_charges")]
        //public decimal? ExcludedCharges { get; set; }

        //[JsonPropertyName("cancellation_fees")]
        //public IEnumerable<CancellationFee> CancellationFees { get; set; }
    }

    //public class Payment
    //{
    //    [JsonPropertyName("due")]
    //    public DateTime? Due { get; set; }

    //    [JsonPropertyName("amount")]
    //    public decimal? Amount { get; set; }
    //}


}