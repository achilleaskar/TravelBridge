using System.Text.Json.Serialization;
using TravelBridge.API.Helpers.Converters;

namespace TravelBridge.API.Contracts
{
    public class MultiAvailabilityResponse
    {
        [JsonPropertyName("http_code")]
        public int HttpCode { get; set; }

        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMsg { get; set; }

        [JsonPropertyName("data")]
        public Data Data { get; set; }
    }

    public class Data
    {
        [JsonPropertyName("hotels")]
        public IEnumerable<WebHotel> Hotels { get; set; }
    }

    public class WebHotel
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

        [JsonPropertyName("salePrice")]
        public decimal SalePrice { get; set; }

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
       
        [JsonPropertyName("hasBoards")]
        public bool HasBoards { get; set; }

        [JsonPropertyName("boardsText")]
        public string BoardsText { get; set; }

        [JsonPropertyName("boards")]
        public List<Board> Boards { get; set; }

        [JsonPropertyName("mappedTypes")]
        public HashSet<string> MappedTypes { get; set; }

        [JsonPropertyName("rates")]
        public List<Rate> Rates { get; set; }

     

        internal void SetBoardsText()
        {
            if (Boards == null || Boards.Count == 0)
            {
                BoardsText = "";
                HasBoards = false;
                return;
            }

            if (Boards.Any(b => b.Id == 0))
            {
            }

            bool hasRoomOnly = Boards.Any(b => b.Id == 14);
            if (hasRoomOnly && Boards.Count == 1)
            {
                BoardsText = "";
                HasBoards = false;
                return;
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
                Boards.FirstOrDefault(b => b.Id == 14).Name+=" -  δεν θα φαινεται";
                //Boards.RemoveAll(b => b.Id == 14);
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

    public class Rate
    {
        //[JsonPropertyName("id")]
        //public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("board")]
        public int? Board { get; set; }

        //[JsonPropertyName("url")]
        //public string Url { get; set; }

        [JsonPropertyName("room")]
        public string Room { get; set; }

        //[JsonPropertyName("roomurl")]
        //public string RoomUrl { get; set; }

        //[JsonPropertyName("remaining")]
        //public int? Remaining { get; set; }

        [JsonPropertyName("rate")]
        public string RateName { get; set; }

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

        [JsonPropertyName("price")]
        public decimal? Price { get; set; }

        [JsonPropertyName("retail")]
        public decimal? Retail { get; set; }

        [JsonPropertyName("discount")]
        public decimal? Discount { get; set; }

        [JsonPropertyName("margin")]
        public decimal? Margin { get; set; }

        [JsonPropertyName("payment_policy")]
        public string PaymentPolicy { get; set; }

        [JsonPropertyName("payment_policy_id")]
        public int? PaymentPolicyId { get; set; }

        [JsonPropertyName("cancellation_policy")]
        public string CancellationPolicy { get; set; }

        [JsonPropertyName("cancellation_policy_id")]
        public int? CancellationPolicyId { get; set; }

        [JsonPropertyName("cancellation_penalty")]
        public string CancellationPenalty { get; set; }

        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("cancellation_expiry")]
        public DateTime? CancellationExpiry { get; set; }

        //[JsonPropertyName("payments")]
        //public IEnumerable<Payment> Payments { get; set; }

        [JsonPropertyName("cancellation_fees")]
        public IEnumerable<CancellationFee> CancellationFees { get; set; }

        [JsonPropertyName("labels")]
        public IEnumerable<Label> Labels { get; set; }
    }

    //public class Payment
    //{
    //    [JsonPropertyName("due")]
    //    public DateTime? Due { get; set; }

    //    [JsonPropertyName("amount")]
    //    public decimal? Amount { get; set; }
    //}

    public class CancellationFee
    {
        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("after")]
        public DateTime? After { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }
    }

    public class Label
    {
        [JsonPropertyName("code")]
        public string Code { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }
    }
}