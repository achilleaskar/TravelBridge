namespace TravelBridge.API.Models.Apis
{
    public class WebHotelierApiOptions
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public GuaranteeCardOptions GuaranteeCard { get; set; } = new();
    }

    public class GuaranteeCardOptions
    {
        public string Number { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Month { get; set; } = string.Empty;
        public string Year { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
    }
}