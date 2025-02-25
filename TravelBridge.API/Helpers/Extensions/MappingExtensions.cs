using System;
using System.Runtime.InteropServices;
using TravelBridge.API.Contracts;
using TravelBridge.API.Models;
using TravelBridge.API.Models.WebHotelier;

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
                salePrice = (minRate.Retail ?? 0) + (minRate.Discount ?? 0);
                return minprice;
            }
            else if (minRate.Retail == null)
            {
                minprice = minRate.Price.Value + minRate.Margin.Value;
                salePrice = (minRate.Retail ?? 0) + (minRate.Discount ?? 0);
                return minprice;
            }
            salePrice = (minRate.Retail ?? 0) + (minRate.Discount ?? 0);
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

        private static int[] noboardIds = new int[] { 0, 14 };

        public static SingleAvailabilityResponse? MapToResponse(this SingleAvailabilityData data, DateTime checkout)
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
                            RateProperties = new RateProperties
                            {
                                RateName = rate.RateName,
                                Board = rate.BoardType?.MapBoardType(Language.el) ?? "Χωρίς διατροφή",
                                CancellationExpiry = rate.CancellationExpiry,
                                CancellationName = rate.CancellationExpiry == null ? "Χωρίς δωρεάν ακύρωση" : "Δωρεάν ακύρωση",
                                CancellationPenalty = rate.CancellationPenalty, 
                                CancellationPolicy = rate.CancellationPolicy,
                                CancellationFees = rate.CancellationFees.ToList().MapToList(checkout),
                                HasCancellation = rate.CancellationExpiry != null,
                                HasBoard = !noboardIds.Contains(rate.BoardType ?? 0)
                            },
                            RateDescription = rate.RateDescription,
                            Labels = rate.Labels,
                            Retail = rate.Retail,
                            Pricing = rate.Pricing,
                            BoardType = rate.BoardType,
                            Status = rate.Status,
                            StatusDescription = rate.StatusDescription,
                            RemainingRooms = rate.RemainingRooms,
                            TotalPrice = rate.GetTotalPrice(),
                            SalePrice = rate.GetSalePrice()
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

        public static List<StringAmount> MapToList(this IEnumerable<CancellationFee> cancellationFees, DateTime checkOut)
        {
            var result = new List<StringAmount>();
            if (cancellationFees == null || !cancellationFees.Any())
                return result;

            var fees = cancellationFees.OrderBy(c => c.After).ToList();
            CancellationFee? previous = null;

            string timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "GTB Standard Time" : "Europe/Athens";

            // Determine Greek time offset dynamically
            TimeZoneInfo greekTimeZone;
            try
            {
                greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                greekTimeZone = TimeZoneInfo.Local; // Fallback to system time
            }
            int offset = (int)greekTimeZone.GetUtcOffset(DateTime.UtcNow).TotalHours;

            // Ensure today exists or add an initial entry for the first cancellation date
            if (fees.First().After?.Date > DateTime.Today)
            {
                result.Add(new StringAmount
                {
                    Description = $"έως {fees.First().After.Value.AddHours(offset):dd-MM-yyyy HH:00}",
                    Amount = 0 // No charge before first cancellation fee
                });
            }

            // Process cancellation fees
            foreach (var fee in fees)
            {
                if (previous != null)
                {
                    result.Add(new StringAmount
                    {
                        Description = $"έως {fee.After.Value.AddHours(offset):dd-MM-yyyy HH:00}",
                        Amount = previous.Fee ?? 0
                    });
                }
                previous = fee;
            }

            // Ensure the last entry covers up to check-out if necessary
            var last = fees.Last();
            if (last.After?.Date != checkOut.Date && last.Fee > 0)
            {
                result.Add(new StringAmount
                {
                    Description = $"έως {checkOut.AddHours(offset):dd-MM-yyyy}",
                    Amount = last.Fee ?? 0
                });
            }

            return result;
        }

        public static HashSet<string> MapToType(this string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return new HashSet<string> { "Other" };

            string normalizedType = type.Trim().ToLower();

            // Get all categories that match the type
            var matchedCategories = categoryMapping
                .Where(category => category.Value.Any(keyword => normalizedType.Contains(keyword, StringComparison.InvariantCultureIgnoreCase)))
                .Select(category => category.Key)
                .Distinct()
                .ToHashSet();

            return matchedCategories.Count != 0 ? matchedCategories : new HashSet<string> { "Other" };
        }

        private static readonly Dictionary<int, Dictionary<Language, string>> BoardTypes = new()
        {
            { 0, new Dictionary<Language, string> { {  Language.en, "No board or N/A" }, {  Language.el, "Χωρίς γεύματα ή Μη διαθέσιμο" } } },
            { 1, new Dictionary<Language, string> { {  Language.en, "All Inclusive" }, {  Language.el, "All Inclusive" } } },
            { 2, new Dictionary<Language, string> { {  Language.en, "American" }, {  Language.el, "Αμερικανικό Πρωινό" } } },
            { 3, new Dictionary<Language, string> { {  Language.en, "Bed & Breakfast" }, {  Language.el, "Διαμονή & Πρωινό" } } },
            { 4, new Dictionary<Language, string> { {  Language.en, "Buffet Breakfast" }, {  Language.el, "Πρωινό σε Μπουφέ" } } },
            { 5, new Dictionary<Language, string> { {  Language.en, "Caribbean Breakfast" }, {  Language.el, "Πρωινό Καραϊβικής" } } },
            { 6, new Dictionary<Language, string> { {  Language.en, "Continental Breakfast" }, {  Language.el, "Ηπειρωτικό Πρωινό" } } },
            { 7, new Dictionary<Language, string> { {  Language.en, "English Breakfast" }, {  Language.el, "Αγγλικό Πρωινό" } } },
            { 8, new Dictionary<Language, string> { {  Language.en, "European Plan" }, {  Language.el, "Ευρωπαϊκό Πρωινό" } } },
            { 9, new Dictionary<Language, string> { {  Language.en, "Family Plan" }, {  Language.el, "Οικογενειακό Πρωινό" } } },
            { 10, new Dictionary<Language, string> { {  Language.en, "Full Board" }, {  Language.el, "Πλήρης Διατροφή" } } },
            { 11, new Dictionary<Language, string> { {  Language.en, "Full Breakfast" }, {  Language.el, "Πλήρες Πρωινό" } } },
            { 12, new Dictionary<Language, string> { {  Language.en, "Half Board" }, {  Language.el, "Ημιδιατροφή" } } },
            { 13, new Dictionary<Language, string> { {  Language.en, "As Brochured" }, {  Language.el, "Πρωινό βάση φυλλαδίου" } } },
            { 14, new Dictionary<Language, string> { {  Language.en, "Room Only" }, {  Language.el, "Χωρίς διατροφή" } } },
            { 15, new Dictionary<Language, string> { {  Language.en, "Self Catering" }, {  Language.el, "Αυτοεξυπηρέτηση" } } },
            { 16, new Dictionary<Language, string> { {  Language.en, "Bermuda" }, {  Language.el, "Πρωινό Βερμούδων" } } },
            { 17, new Dictionary<Language, string> { {  Language.en, "Dinner Bed and Breakfast Plan" }, {  Language.el, "Δείπνο, Κρεβάτι και Πρωινό" } } },
            { 18, new Dictionary<Language, string> { {  Language.en, "Family American" }, {  Language.el, "Οικογενειακό Αμερικανικό Πρωινό" } } },
            { 19, new Dictionary<Language, string> { {  Language.en, "Breakfast" }, {  Language.el, "Πρωινό" } } },
            { 20, new Dictionary<Language, string> { {  Language.en, "Modified" }, {  Language.el, "Τροποποιημένο" } } },
            { 21, new Dictionary<Language, string> { {  Language.en, "Lunch" }, {  Language.el, "Μεσημεριανό" } } },
            { 22, new Dictionary<Language, string> { {  Language.en, "Dinner" }, {  Language.el, "Δείπνο" } } },
            { 23, new Dictionary<Language, string> { {  Language.en, "Breakfast & Lunch" }, {  Language.el, "Πρωινό & Μεσημεριανό" } } }
        };

        // Mapping rules: If a specific board ID exists, remove the conflicting ones and update values
        private static readonly Dictionary<int?, int> BoardReplacements = new()
        {
            { 0, 14 }, // If 14 exists, replace 0 → 14
            //{ 10, 1 }  // If 10 exists, replace 10 → 1
        };

        public static string? MapBoardType(this int boardId, Language lang = Language.el)
        {
            // Apply board replacement rules
            foreach (var (oldBoard, newBoard) in BoardReplacements)
            {
                if (boardId == oldBoard)
                {
                    boardId = newBoard; // Replace the board if a rule exists
                    break;
                }
            }

            // Ensure the board exists in the dictionary
            if (BoardTypes.ContainsKey(boardId))
            {
                return BoardTypes[boardId].ContainsKey(lang) ? BoardTypes[boardId][lang] : "Χωρίς διατροφή";
            }

            return "Χωρίς διατροφή"; // Return null if board ID is not valid
        }

        public static List<Board> MapBoardTypes(this IEnumerable<BaseBoard>? rates, Language lang = Language.el)
        {
            if (rates == null)
                return new List<Board>();

            // Convert rates to a list for multiple iterations
            var rateList = rates.ToList();

            var boardIds = rateList.Select(rate => rate.BoardType).ToHashSet(); // Store existing board IDs

            // Apply the board replacement rules
            foreach (var (oldBoard, newBoard) in BoardReplacements)
            {
                if (boardIds.Contains(newBoard)) // If the newBoard exists in the list
                {
                    rateList.RemoveAll(rate => rate.BoardType == oldBoard); // Remove conflicting board
                }
                else
                {
                    foreach (var rate in rateList.Where(rate => rate.BoardType == oldBoard))
                    {
                        rate.BoardType = newBoard;
                    }
                }
            }

            return rateList
                   .Where(rate => BoardTypes.ContainsKey(rate.BoardType ?? 0)) // Ensure the board exists in the dictionary
                   .GroupBy(rate => rate.BoardType ?? 14) // Group by board ID to ensure distinct values
                   .Select(group => new Board
                   {
                       Id = group.Key, // Key: Board ID
                       Name = BoardTypes[group.Key].ContainsKey(lang) ? BoardTypes[group.Key][lang] : "Unknown" // Value: Description in the selected language
                   })
                   .ToList();
        }

        public static void SetBoardsText(this BoardTextBase b)
        {
            if (b.Boards == null || b.Boards.Count == 0)
            {
                b.BoardsText = "";
                b.HasBoards = false;
                return;
            }

            if (b.Boards.Any(b => b.Id == 0))
            {
            }

            bool hasRoomOnly = b.Boards.Any(b => b.Id == 14);
            if (hasRoomOnly && b.Boards.Count == 1)
            {
                b.Boards.First().Name = "Χωρίς επιλογές διατροφής";
            }

            if (b.Boards.Count == 1)
            {
                b.BoardsText = "Διατροφή:";
                b.HasBoards = true;
                return;
            }

            if (hasRoomOnly)
            {
                b.BoardsText = "Επιλογές Διατροφής:";
                b.HasBoards = true;
                b.Boards.FirstOrDefault(b => b.Id == 14).Name += " -  δεν θα φαινεται";
                //Boards.RemoveAll(b => b.Id == 14);
                return;
            }

            if (b.Boards.Count > 1)
            {
                b.BoardsText = "Επιλογές Διατροφής:";
                b.HasBoards = true;
                return;
            }
        }
    }
}