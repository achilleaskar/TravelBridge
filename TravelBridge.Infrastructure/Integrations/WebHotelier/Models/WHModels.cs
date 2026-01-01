using System.Text.Json.Serialization;
using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Models
{
    /// <summary>
    /// Base hotel info from WebHotelier.
    /// </summary>
    public class WHBaseHotelInfo
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = "";

        [JsonIgnore]
        public HotelProvider Provider { get; set; } = HotelProvider.WebHotelier;

        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";
    }

    /// <summary>
    /// WebHotelier board (meal plan) info.
    /// </summary>
    public class WHBoard
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    /// <summary>
    /// Base rate info from WebHotelier.
    /// </summary>
    public class WHBaseRate
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("board")]
        public WHBoard? Board { get; set; }

        [JsonPropertyName("remaining")]
        public int? RemainingRooms { get; set; }

        public WHPartyItem? SearchParty { get; set; }
    }

    /// <summary>
    /// WebHotelier payment info.
    /// </summary>
    public class WHPayment
    {
        [JsonPropertyName("due")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// Converts to Core ScheduledPayment.
        /// </summary>
        public ScheduledPayment ToScheduledPayment()
        {
            return new ScheduledPayment
            {
                DueDate = DueDate,
                Amount = Amount ?? 0
            };
        }
    }

    /// <summary>
    /// WebHotelier cancellation fee.
    /// </summary>
    public class WHCancellationFee
    {
        [JsonPropertyName("after")]
        public DateTime? After { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }
    }

    /// <summary>
    /// WebHotelier pricing info.
    /// </summary>
    public class WHPricingInfo
    {
        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("excluded_charges")]
        public decimal ExcludedCharges { get; set; }

        [JsonPropertyName("extras")]
        public decimal Extras { get; set; }

        [JsonPropertyName("margin")]
        public decimal Margin { get; set; }

        [JsonPropertyName("stay")]
        public decimal StayPrice { get; set; }

        [JsonPropertyName("taxes")]
        public decimal Taxes { get; set; }

        [JsonPropertyName("price")]
        public decimal TotalPrice { get; set; }
    }

    /// <summary>
    /// WebHotelier location info.
    /// </summary>
    public class WHLocation
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("zip")]
        public string? Zip { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }
    }

    /// <summary>
    /// WebHotelier operation (check-in/out times).
    /// </summary>
    public class WHOperation
    {
        [JsonPropertyName("checkout_time")]
        public string? CheckoutTime { get; set; }

        [JsonPropertyName("checkin_time")]
        public string? CheckinTime { get; set; }
    }

    /// <summary>
    /// WebHotelier photo info.
    /// </summary>
    public class WHPhoto
    {
        [JsonPropertyName("medium")]
        public string? Medium { get; set; }

        [JsonPropertyName("large")]
        public string? Large { get; set; }
    }
}
