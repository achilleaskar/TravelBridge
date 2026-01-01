using TravelBridge.Contracts.Common.Policies;

namespace TravelBridge.Contracts.Models.Hotels
{
    public class HotelData : BoardTextBase
    {
        private decimal salePrice;

        [JsonPropertyName("code")]
        public string Code { internal get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

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

        public string CustomInfo { get; set; }

        [JsonPropertyName("mappedTypes")]
        public HashSet<string> MappedTypes { get; set; }
        public decimal MinPricePerNight { get; set; }
        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("location")]
        public LocationInfo Location { get; set; }

        [JsonPropertyName("children")]
        public ChildrenPolicy Children { get; set; }

        [JsonPropertyName("operation")]
        public HotelOperation Operation { get; set; }

        [JsonPropertyName("facilities")]
        public IEnumerable<string> Facilities { get; set; }

        [JsonPropertyName("photos")]
        public IEnumerable<PhotoInfo> PhotosItems { get; set; }

        public IEnumerable<string> LargePhotos { get; set; }

        public void SetBoardText()
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
}
