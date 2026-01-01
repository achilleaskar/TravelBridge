namespace TravelBridge.Contracts.Requests
{
    /// <summary>
    /// Request to search for hotel availability.
    /// </summary>
    public class AvailabilitySearchRequest
    {
        /// <summary>
        /// Check-in date (format: yyyy-MM-dd).
        /// </summary>
        public required string CheckIn { get; init; }

        /// <summary>
        /// Check-out date (format: yyyy-MM-dd).
        /// </summary>
        public required string CheckOut { get; init; }

        /// <summary>
        /// Party configuration as JSON: [{"adults":2, "children":[2,6]},{"adults":3}]
        /// </summary>
        public required string Party { get; init; }

        /// <summary>
        /// Property/hotel ID for single hotel search.
        /// </summary>
        public string? PropertyId { get; init; }

        /// <summary>
        /// Center latitude for area search.
        /// </summary>
        public decimal? Latitude { get; init; }

        /// <summary>
        /// Center longitude for area search.
        /// </summary>
        public decimal? Longitude { get; init; }

        /// <summary>
        /// Bounding box for area search [bottomLeftLat, bottomLeftLon, topRightLat, topRightLon].
        /// </summary>
        public decimal[]? BoundingBox { get; init; }

        /// <summary>
        /// Coupon code to apply.
        /// </summary>
        public string? CouponCode { get; init; }
    }

    /// <summary>
    /// Request to create a booking.
    /// </summary>
    public class BookingRequest
    {
        /// <summary>
        /// Hotel ID.
        /// </summary>
        public required string HotelId { get; init; }

        /// <summary>
        /// Check-in date.
        /// </summary>
        public required DateOnly CheckIn { get; init; }

        /// <summary>
        /// Check-out date.
        /// </summary>
        public required DateOnly CheckOut { get; init; }

        /// <summary>
        /// Selected rates with quantities.
        /// </summary>
        public required IReadOnlyList<SelectedRateRequest> SelectedRates { get; init; }

        /// <summary>
        /// Customer information.
        /// </summary>
        public required CustomerInfoRequest Customer { get; init; }

        /// <summary>
        /// Coupon code if applicable.
        /// </summary>
        public string? CouponCode { get; init; }
    }

    /// <summary>
    /// Selected rate in a booking request.
    /// </summary>
    public class SelectedRateRequest
    {
        /// <summary>
        /// Rate ID.
        /// </summary>
        public required string RateId { get; init; }

        /// <summary>
        /// Number of rooms for this rate.
        /// </summary>
        public required int Quantity { get; init; }
    }

    /// <summary>
    /// Customer information for booking.
    /// </summary>
    public class CustomerInfoRequest
    {
        /// <summary>
        /// Customer's first name.
        /// </summary>
        public required string FirstName { get; init; }

        /// <summary>
        /// Customer's last name.
        /// </summary>
        public required string LastName { get; init; }

        /// <summary>
        /// Customer's email address.
        /// </summary>
        public required string Email { get; init; }

        /// <summary>
        /// Customer's phone number.
        /// </summary>
        public string? Phone { get; init; }

        /// <summary>
        /// Special requests or notes.
        /// </summary>
        public string? Notes { get; init; }
    }
}
