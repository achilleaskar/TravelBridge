namespace TravelBridge.Contracts.Common
{
    public class Alternative
    {
        public DateTime CheckIn { get; set; }
        public DateTime Checkout { get; set; }
        public decimal MinPrice { get; set; }
        [JsonIgnore]
        public decimal NetPrice { get; set; }
        public int Nights { get; set; }
    }
}
