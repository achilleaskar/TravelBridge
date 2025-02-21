using TravelBridge.API.Contracts;
using TravelBridge.API.Models;

namespace TravelBridge.API.Helpers.Extensions
{
    public static class MappingExtensions
    {
        public static decimal GetMinPrice(this WebHotel h, out decimal salePrice)
        {
            salePrice = 0;
            decimal minprice = 0;
            var minRate = h.Rates.MinBy(r => r.Retail);
            if (minRate?.Price == null)
            {
                return 0;
            }
            var minMargin = minRate.Price.Value * 10 / 100;
            if (minRate.Margin == null || minRate.Margin < minMargin)
            {
                minprice = minRate.Price.Value + minMargin;
                salePrice = minRate.Retail ?? 0 + minRate.Discount ?? 0;
                return minprice;
            }
            else if (minRate.Retail == null)
            {
                minprice = minRate.Price.Value + minRate.Margin.Value;
                salePrice = minRate.Retail ?? 0 + minRate.Discount ?? 0;
                return minprice;
            }
            return minRate.Retail.Value;
        }

        public static decimal GetMinPrice(this SingleHotelAvailabilityInfo h, out decimal salePrice)
        {
            salePrice = 0;
            var minRate = h.Rooms.SelectMany(a => a.Rates).MinBy(r => r.Retail.TotalPrice);
            if (minRate == null)
            {
                return 0;
            }
            if (minRate.Retail.Discount > 0 && minRate.Retail.TotalPrice > 0)
            {
                salePrice = minRate.Retail.TotalPrice + minRate.Retail.Discount;
            }

            var minMargin = minRate.Pricing.TotalPrice * 10 / 100;
            if (minRate.Pricing.Margin < minMargin || minRate.Retail == null)
            {
                return minRate.Pricing.TotalPrice + minMargin;
            }

            return minRate.Retail.TotalPrice;
        }

        public static SingleAvailabilityResponse? MapToResponse(this SingleAvailabilityData data)
        {
            if (data?.Data == null)
            {
                return null;
            }
            return new SingleAvailabilityResponse
            {
                HttpCode = data.HttpCode,
                ErrorCode = data.ErrorCode,
                ErrorMessage = data.ErrorMessage,
                Data = new SingleHotelAvailabilityInfo
                {
                    Code = data.Data.Code,
                    Name = data.Data.Name,
                    Location = data.Data.Location,
                    Provider = data.Data.Provider,
                    Rooms = data.Data.Rates.GroupBy(r => r.Type).Select(r => new SingleHotelRoom
                    {
                        Type = r.Key,
                        RoomName = r.First().RoomName,
                        RatesCount = r.Count(),
                        Rates = r.Select(rate => new SingleHotelRate
                        {
                            Id = rate.Id,
                            RateName = rate.RateName,
                            RateDescription = rate.RateDescription,
                            PaymentPolicy = rate.PaymentPolicy,
                            BoardType = rate.BoardType,
                            CancellationExpiry = rate.CancellationExpiry,
                            CancellationPenalty = rate.CancellationPenalty,
                            CancellationPolicy = rate.CancellationPolicy,
                            CancellationPolicyId = rate.CancellationPolicyId,
                            Labels = rate.Labels,
                            PaymentPolicyId = rate.PaymentPolicyId,
                            Retail = rate.Retail,
                            Pricing = rate.Pricing,
                            Status = rate.Status,
                            StatusDescription = rate.StatusDescription
                        }).ToList()
                    })
                }
            };
        }

        private static Dictionary<string, List<string>> categoryMapping = new()
        {
                { "Glamping & Unique Stays", new List<string> {"cave", "luxury glamping resort", "traditional windmill" } },
                { "Guesthouse", new List<string> { "guesthouse", "guest house", "pension" } },
                { "Chalet", new List<string> { "chalet", "challet" } },
                { "Bed & Breakfast", new List<string> { "bed & breakfast", "Bed and Breakfast", "bnb" } },
                { "Bungalow", new List<string> { "bungalow" } },
                { "Resort", new List<string> { "resort"} },
                { "Villa", new List<string> { "villa", "vila" } },
                { "Rooms", new List<string> { "room" } },
                { "Apartments & Houses", new List<string> {"apartment", "studio", "αpartment", "aparments", "apartmenst", "appartment" } },
                { "Houses", new List<string> {"house", "home", "residence", "cottage", "vacation Rental", "mansion", "maisonette" } },
                { "Suites", new List<string> { "suite" } },
                { "Hotel", new List<string> { "hotel"} }
            };

        public static HashSet<string> MapToType(this string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new HashSet<string> { "Other" };

            string normalizedType = type.Trim().ToLower();

            // Get all categories that match the type
            var matchedCategories = categoryMapping
                .Where(category => category.Value.Any(keyword => normalizedType.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)))
                .Select(category => category.Key.ToLower())
                .Distinct()
                .ToHashSet();

            return matchedCategories.Count != 0 ? matchedCategories : new HashSet<string> { "Other" };
        }

        private static readonly Dictionary<int, Dictionary<Language, string>> BoardTypes = new()
        {
                    { 0, new Dictionary<Language, string> { {  Language.en, "No board or N/A" }, {  Language.el, "Χωρίς γεύματα ή Μη διαθέσιμο" } } },
                    { 1, new Dictionary<Language, string> { {  Language.en, "All Inclusive" }, {  Language.el, "Όλα συμπεριλαμβάνονται" } } },
                    { 2, new Dictionary<Language, string> { {  Language.en, "American" }, {  Language.el, "Αμερικανικό" } } },
                    { 3, new Dictionary<Language, string> { {  Language.en, "Bed & Breakfast" }, {  Language.el, "Διαμονή & Πρωινό" } } },
                    { 4, new Dictionary<Language, string> { {  Language.en, "Buffet Breakfast" }, {  Language.el, "Πρωινό σε Μπουφέ" } } },
                    { 5, new Dictionary<Language, string> { {  Language.en, "Caribbean Breakfast" }, {  Language.el, "Καραϊβικό Πρωινό" } } },
                    { 6, new Dictionary<Language, string> { {  Language.en, "Continental Breakfast" }, {  Language.el, "Ηπειρωτικό Πρωινό" } } },
                    { 7, new Dictionary<Language, string> { {  Language.en, "English Breakfast" }, {  Language.el, "Αγγλικό Πρωινό" } } },
                    { 8, new Dictionary<Language, string> { {  Language.en, "European Plan" }, {  Language.el, "Ευρωπαϊκό Πρόγραμμα" } } },
                    { 9, new Dictionary<Language, string> { {  Language.en, "Family Plan" }, {  Language.el, "Οικογενειακό Πρόγραμμα" } } },
                    { 10, new Dictionary<Language, string> { {  Language.en, "Full Board" }, {  Language.el, "Πλήρης Διατροφή" } } },
                    { 11, new Dictionary<Language, string> { {  Language.en, "Full Breakfast" }, {  Language.el, "Πλήρες Πρωινό" } } },
                    { 12, new Dictionary<Language, string> { {  Language.en, "Half Board" }, {  Language.el, "Ημιδιατροφή" } } },
                    { 13, new Dictionary<Language, string> { {  Language.en, "As Brochured" }, {  Language.el, "Όπως στο φυλλάδιο" } } },
                    { 14, new Dictionary<Language, string> { {  Language.en, "Room Only" }, {  Language.el, "Μόνο Δωμάτιο" } } },
                    { 15, new Dictionary<Language, string> { {  Language.en, "Self Catering" }, {  Language.el, "Αυτοεξυπηρέτηση" } } },
                    { 16, new Dictionary<Language, string> { {  Language.en, "Bermuda" }, {  Language.el, "Βερμούδες" } } },
                    { 17, new Dictionary<Language, string> { {  Language.en, "Dinner Bed and Breakfast Plan" }, {  Language.el, "Δείπνο, Κρεβάτι και Πρωινό" } } },
                    { 18, new Dictionary<Language, string> { {  Language.en, "Family American" }, {  Language.el, "Οικογενειακό Αμερικανικό" } } },
                    { 19, new Dictionary<Language, string> { {  Language.en, "Breakfast" }, {  Language.el, "Πρωινό" } } },
                    { 20, new Dictionary<Language, string> { {  Language.en, "Modified" }, {  Language.el, "Τροποποιημένο" } } },
                    { 21, new Dictionary<Language, string> { {  Language.en, "Lunch" }, {  Language.el, "Μεσημεριανό" } } },
                    { 22, new Dictionary<Language, string> { {  Language.en, "Dinner" }, {  Language.el, "Δείπνο" } } },
                    { 23, new Dictionary<Language, string> { {  Language.en, "Breakfast & Lunch" }, {  Language.el, "Πρωινό & Μεσημεριανό" } } }
                };

        public static List<Board> MapBoardTypes(this IEnumerable<Rate>? rates, Language lang = Language.el)
        {
            if (rates == null)
                return new List<Board>();

            // Convert rates to a list for multiple iterations
            var rateList = rates.ToList();

            // Check if board ID 14 exists, and if so, remove board ID 0
            if (rateList.Any(rate => rate.Board == 14))
            {
                rateList = rateList.Where(rate => rate.Board != 0).ToList();
            }

            return rateList
                   .Where(rate => BoardTypes.ContainsKey(rate.Board ?? 0)) // Ensure the board exists in the dictionary
                   .GroupBy(rate => rate.Board ?? 0) // Group by board ID to ensure distinct values
                   .Select(group => new Board
                   {
                       Id = group.Key, // Key: Board ID
                       Name = BoardTypes[group.Key].ContainsKey(lang) ? BoardTypes[group.Key][lang] : "Unknown" // Value: Description in the selected language
                   })
                   .ToList();
        }

        public static Dictionary<int, string> MapBoardTypes(this IEnumerable<SingleHotelRate> rates, Language lang = Language.el)
        {
            return rates?
                   .Where(rate => BoardTypes.ContainsKey(rate.BoardType ?? 0)) // Ensure the board exists in the dictionary
                   .GroupBy(rate => rate.BoardType ?? 0) // Group by board ID to ensure distinct values
                   .ToDictionary(
                       group => group.Key, // Key: Board ID
                       group => BoardTypes[group.Key].ContainsKey(lang) ? BoardTypes[group.Key][lang] : "Unknown" // Value: Description in the selected language
                   ) ?? new();
        }
    }
}