using System.Text.Json.Serialization;
using TravelBridge.API.Helpers.Converters;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Contracts
{
    public class CancellationFee
    {
        #region Properties

        [JsonConverter(typeof(NullableDateTimeConverter))]
        [JsonPropertyName("after")]
        public DateTime? After { get; set; }

        [JsonPropertyName("fee")]
        public decimal? Fee { get; set; }

        #endregion Properties
    }

    public class HotelInfo : BaseHotelInfo
    {
        #region Properties

        [JsonPropertyName("location")]
        public Location Location { get; set; }

        [JsonPropertyName("rates")]
        public List<HotelRate> Rates { get; set; }

        #endregion Properties
    }

    public class HotelRate : BaseRate
    {
        #region Fields

        public decimal totalPrice;

        #endregion Fields

        #region Properties

        [JsonPropertyName("cancellation_fees")]
        public IEnumerable<CancellationFee> CancellationFees { get; set; }

        [JsonPropertyName("payments")]
        public List<PaymentWH>? Payments { get; set; }

        [JsonPropertyName("id")]
        [JsonConverter(typeof(IntToStringJsonConverter))]
        public string Id { get; set; }

        [JsonPropertyName("pricing")]
        public PricingInfo Pricing { get; set; }

        [JsonPropertyName("rate_desc")]
        public string RateDescription { get; set; }
        [JsonPropertyName("remaining")]
        public int? RemainingRooms { get; set; }

        [JsonPropertyName("retail")]
        public PricingInfo Retail { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("status_descr")]
        public string StatusDescription { get; set; }

        #endregion Properties

        #region Methods

        internal decimal GetSalePrice()
        {
            decimal saleprice = Retail.TotalPrice + Retail.Discount;
            if (saleprice > totalPrice + 5)
            {
                return saleprice;
            }
            return 0;
        }

        public decimal ProfitPerc { get; set; }
        public PartyItem SearchParty { get; set; }

        internal decimal GetTotalPrice()
        {
            var minMargin = Pricing.TotalPrice * 10 / 100;
            if (Pricing.Margin < minMargin || (Retail.TotalPrice - Pricing.TotalPrice) < minMargin || Retail == null || Retail.TotalPrice == 0)
            {
                totalPrice = decimal.Floor((Pricing.TotalPrice + minMargin) * 0.9m);
                ProfitPerc = decimal.Round(totalPrice / Pricing.TotalPrice, 6);
                return totalPrice;
            }
            else
            {
                totalPrice = decimal.Floor(Retail.TotalPrice * 0.9m);
                ProfitPerc = decimal.Round(totalPrice / Pricing.TotalPrice, 6);
                return totalPrice;
            }
        }

        #endregion Methods
    }

    public class PricingInfo
    {
        #region Properties

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

        #endregion Properties
    }

    public class PaymentWH
    {
        [JsonPropertyName("due")]
        public DateTime? DueDate { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        public override string ToString()
        {
            return $"DueDate: {DueDate?.ToString("yyyy-MM-dd")}, " +
                   $"Amount: {Amount?.ToString("C", System.Globalization.CultureInfo.CurrentCulture)}";
        }
    }

    public class SingleAvailabilityData : BaseWebHotelierResponse
    {
        #region Properties

        [JsonPropertyName("data")]
        public HotelInfo Data { get; set; }

        internal bool CoversRequest(List<PartyItem>? partyList)
        {
            if (partyList.Sum(a => a.RoomsCount) > Data.Rates.DistinctBy(h => h.Type).Sum(s => s.RemainingRooms))
            {
                return false;
            }
            else
            {
                foreach (var party in partyList)
                {
                    if (party.RoomsCount <= (
                        Data.Rates
                        .Where(r => r.SearchParty?.Equals(party) == true)
                        .GroupBy(r => r.Type)
                        .Select(g => g.First())
                        .Sum(s => s.RemainingRooms) ?? 0
                        )
                    )
                    {
                        continue;
                    }
                    return false;
                }
            }
            return true;
        }

        #endregion Properties
    }
}