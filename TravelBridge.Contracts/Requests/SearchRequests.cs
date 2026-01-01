namespace TravelBridge.Contracts.Requests
{
    /// <summary>
    /// Multi-hotel search request.
    /// </summary>
    public class SearchRequest
    {
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public required string BBox { get; init; }
        public string? SearchTerm { get; init; }
        public int? Adults { get; init; }
        public string? Children { get; init; }
        public int? Rooms { get; init; }
        public string? Party { get; init; }
        public int? Page { get; init; }
        public string? Sorting { get; init; }
        public int? MinPrice { get; init; }
        public int? MaxPrice { get; init; }
        public string? HotelTypes { get; init; }
        public string? BoardTypes { get; init; }
        public string? Rating { get; init; }
    }

    /// <summary>
    /// Single hotel availability request.
    /// </summary>
    public class HotelAvailabilityRequest
    {
        public required string HotelId { get; init; }
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public int? Adults { get; init; }
        public string? Children { get; init; }
        public int? Rooms { get; init; }
        public string? Party { get; init; }
    }

    /// <summary>
    /// Checkout request.
    /// </summary>
    public class CheckoutRequest
    {
        public required string HotelId { get; init; }
        public required string CheckIn { get; init; }
        public required string CheckOut { get; init; }
        public required string SelectedRates { get; init; }
        public string? CouponCode { get; init; }
    }
}
