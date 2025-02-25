using System.Text.Json.Serialization;
using TravelBridge.API.Helpers.Converters;
using TravelBridge.API.Models;
using TravelBridge.API.Models.WebHotelier;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TravelBridge.API.Contracts
{
    public class SingleAvailabilityData : BaseWebHotelierResponse
    {
        [JsonPropertyName("data")]
        public HotelInfo Data { get; set; }
    }

    public class HotelInfo : BaseHotelInfo
    {
        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("rates")]
        public List<HotelRate> Rates { get; set; }
    }

    public class HotelRate : BaseRate
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rate_desc")]
        public string RateDescription { get; set; }

        [JsonPropertyName("pricing")]
        public PricingInfo Pricing { get; set; }

        [JsonPropertyName("retail")]
        public PricingInfo Retail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_descr")]
        public string StatusDescription { get; set; }

        [JsonPropertyName("remaining")]
        public int? RemainingRooms { get; set; }

        [JsonPropertyName("cancellation_fees")]
        public IEnumerable<CancellationFee> CancellationFees { get; set; }

        public decimal totalPrice;

        internal decimal GetSalePrice()
        {
            decimal saleprice = Retail.TotalPrice + Retail.Discount;
            if (saleprice > totalPrice + 5)
            {
                return saleprice;
            }
            return 0;
        }

        internal decimal GetTotalPrice()
        {
            var minMargin = Pricing.TotalPrice * 10 / 100;
            if (Pricing.Margin < minMargin || (Retail.TotalPrice - Pricing.TotalPrice) < minMargin || Retail == null || Retail.TotalPrice == 0)
            {
                totalPrice = decimal.Floor(Pricing.TotalPrice + minMargin);
                return totalPrice;
            }
            else
            {
                totalPrice = decimal.Floor(Retail.TotalPrice);
                return totalPrice;
            }
        }
        
    }
    public class CancellationFee
    {
        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("after")]
        public DateTime? After { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }
    }

    public class PricingInfo
    {
        [JsonPropertyName("stay")]
        public decimal StayPrice { get; set; }

        [JsonPropertyName("extras")]
        public decimal Extras { get; set; }

        [JsonPropertyName("taxes")]
        public decimal Taxes { get; set; }

        [JsonPropertyName("excluded_charges")]
        public decimal ExcludedCharges { get; set; }

        [JsonPropertyName("price")]
        public decimal TotalPrice { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("margin")]
        public decimal Margin { get; set; }
    }
}