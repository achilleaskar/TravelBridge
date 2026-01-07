using TravelBridge.API.Contracts;
using TravelBridge.Contracts.Plugin.AutoComplete;
using TravelBridge.Contracts.Common.Policies;
using TravelBridge.Contracts.Common.Board;
using TravelBridge.Providers.WebHotelier.Models.Responses;
using TravelBridge.API.Contracts.DTOs;
using TravelBridge.Contracts.Models.Hotels;
using TravelBridge.Contracts.Common;
using TravelBridge.Providers.Abstractions.Models;
using TravelBridge.API.Providers;

namespace TravelBridge.API.Helpers.Extensions
{
    public static class MappingExtensions
    {
        /// <summary>
        /// Maps WebHotelier Hotel array to AutoCompleteHotel collection
        /// </summary>
        public static IEnumerable<AutoCompleteHotel> MapToAutoCompleteHotels(this WHHotel[] hotels)
        {
            return hotels.Select(hotel =>
                new AutoCompleteHotel(
                    hotel.code,
                    Provider.WebHotelier,
                    hotel.name,
                    hotel.location?.name ?? "",
                    hotel.location?.country ?? "",
                    hotel.type));
        }

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

        public static SingleAvailabilityResponse? MapToResponse(this WHSingleAvailabilityData data, DateTime checkin, decimal disc, CouponType couponType)
        {
            if (data?.Data == null)
            {
                return null;
            }

            // Convert WH rates to Contracts HotelRate for processing
            var contractsRates = data.Data.Rates.Select(r => r.ToContracts()).ToList();

            foreach (var rate in contractsRates)
            {
                if (rate.CancellationExpiry?.Date <= DateTime.Now.AddHours(3).Date)
                {
                    rate.CancellationExpiry = null;
                }
            }

            //TODO: use request dateTime to avoid change of date issue
            return new SingleAvailabilityResponse
            {
                HttpCode = data.HttpCode,
                ErrorCode = data.ErrorCode,
                ErrorMessage = data.ErrorMessage,
                CouponDiscount = disc == 0m ? null : (couponType == CouponType.percentage ? ($"-{(int)(disc * 100)} %") : $"-{disc} €"),
                CouponValid = disc != 0m,
                Data = new SingleHotelAvailabilityInfo
                {
                    Code = data.Data.Code,
                    Name = data.Data.Name,
                    Location = data.Data.Location.ToContracts(),
                    Provider = (Provider)(int)data.Data.Provider,
                    Rooms = contractsRates.GroupBy(r => r.Type).Select(r => new SingleHotelRoom
                    {
                        Type = r.Key,
                        RoomName = r.First().RoomName,
                        RatesCount = r.Count(),
                        Rates = r.Select(rate => new SingleHotelRate
                        {
                            TotalPrice = rate.GetTotalPrice(data.Data.Code, disc, couponType),
                            SalePrice = rate.GetSalePrice(),
                            NetPrice = rate.Pricing.TotalPrice,
                            Id = rate.Id.ToString(),
                            SearchParty = rate.SearchParty,
                            RateProperties = new RateProperties
                            {
                                RateName = rate.RateName,
                                Board = rate.BoardType?.MapBoardType(Language.el) ?? "Χωρίς διατροφή",
                                CancellationExpiry = rate.CancellationExpiry?.ToString("dd/MM/yyyy HH:mm"),
                                CancellationName = rate.CancellationExpiry == null ? "Χωρίς δωρεάν ακύρωση" : "Δωρεάν ακύρωση",
                                CancellationPenalty = rate.CancellationPenalty,
                                CancellationPolicy = rate.CancellationPolicy,
                                CancellationFeesOr = rate.CancellationFees,
                                PaymentsOr = rate.Payments?.Select(a => new PaymentWH { Amount = a.Amount, DueDate = a.DueDate }).ToList() ?? new List<PaymentWH>(),
                                CancellationFees = rate.CancellationFees.ToList().MapToList(checkin, rate),
                                Payments = rate.Payments ?? new List<PaymentWH>(),
                                PartialPayment = rate.CancellationExpiry != null ? General.FillPartialPayment(rate.Payments, checkin) : null,
                                HasCancellation = rate.CancellationExpiry != null,
                                HasBoard = !General.NoboardIds.Contains(rate.BoardType ?? 0)
                            },
                            RateDescription = rate.RateDescription,
                            //Labels = rate.Labels,
                            Retail = rate.Retail,
                            Pricing = rate.Pricing,
                            BoardType = rate.BoardType,
                            //Status = rate.Status,
                            //StatusDescription = rate.StatusDescription,
                            RemainingRooms = rate.RemainingRooms
                        }).ToList()
                    }).ToList(),
                    Alternatives = data.Alternatives.ToContracts().GetFinalPrice(disc, data.Data.Code, couponType)
                }
            };
        }

        /// <summary>
        /// Maps HotelAvailabilityResult to SingleAvailabilityResponse using provider-neutral types.
        /// Reuses existing pricing logic (GetTotalPrice, GetSalePrice, etc.).
        /// </summary>
        public static SingleAvailabilityResponse MapToResponse(
            this HotelAvailabilityResult result, 
            DateTime checkin, 
            decimal disc, 
            CouponType couponType,
            int providerId)
        {
            if (!result.IsSuccess || result.Data == null)
            {
                return new SingleAvailabilityResponse
                {
                    ErrorCode = result.ErrorCode ?? "Error",
                    ErrorMessage = result.ErrorMessage ?? "No data available",
                    Data = new SingleHotelAvailabilityInfo { Rooms = [] }
                };
            }

            // Convert provider rates to contract HotelRate for pricing logic
            var contractsRates = ProviderToContractsMapper.ToHotelRates(result);

            // Apply cancellation expiry fix (same as WH mapper)
            foreach (var rate in contractsRates)
            {
                if (rate.CancellationExpiry?.Date <= DateTime.Now.AddHours(3).Date)
                {
                    rate.CancellationExpiry = null;
                }
            }

            var hotelCode = result.Data.HotelCode;

            return new SingleAvailabilityResponse
            {
                HttpCode = 200,
                ErrorCode = null,
                ErrorMessage = null,
                CouponDiscount = disc == 0m ? null : (couponType == CouponType.percentage ? ($"-{(int)(disc * 100)} %") : $"-{disc} €"),
                CouponValid = disc != 0m,
                Data = new SingleHotelAvailabilityInfo
                {
                    Code = result.Data.HotelCode,
                    Name = result.Data.HotelName ?? "",
                    Location = result.Data.Location != null ? new Location
                    {
                        Latitude = result.Data.Location.Latitude,
                        Longitude = result.Data.Location.Longitude,
                        Name = result.Data.Location.Name
                    } : null,
                    Provider = providerId == TravelBridge.Providers.Abstractions.ProviderIds.WebHotelier 
                        ? Provider.WebHotelier 
                        : Provider.WebHotelier, // Only WH supported for now
                    Rooms = contractsRates.GroupBy(r => r.Type).Select(r => new SingleHotelRoom
                    {
                        Type = r.Key,
                        RoomName = r.First().RoomName,
                        RatesCount = r.Count(),
                        Rates = r.Select(rate => new SingleHotelRate
                        {
                            TotalPrice = rate.GetTotalPrice(hotelCode, disc, couponType),
                            SalePrice = rate.GetSalePrice(),
                            NetPrice = rate.Pricing?.TotalPrice ?? 0,
                            Id = rate.Id ?? "",
                            SearchParty = rate.SearchParty,
                            RateProperties = new RateProperties
                            {
                                RateName = rate.RateName,
                                Board = rate.BoardType?.MapBoardType(Language.el) ?? "Χωρίς διατροφή",
                                CancellationExpiry = rate.CancellationExpiry?.ToString("dd/MM/yyyy HH:mm"),
                                CancellationName = rate.CancellationExpiry == null ? "Χωρίς δωρεάν ακύρωση" : "Δωρεάν ακύρωση",
                                CancellationPenalty = rate.CancellationPenalty,
                                CancellationPolicy = rate.CancellationPolicy,
                                CancellationFeesOr = rate.CancellationFees,
                                PaymentsOr = rate.Payments?.Select(a => new PaymentWH { Amount = a.Amount, DueDate = a.DueDate }).ToList() ?? new List<PaymentWH>(),
                                CancellationFees = (rate.CancellationFees?.ToList() ?? []).MapToList(checkin, rate),
                                Payments = rate.Payments ?? new List<PaymentWH>(),
                                PartialPayment = rate.CancellationExpiry != null ? General.FillPartialPayment(rate.Payments, checkin) : null,
                                HasCancellation = rate.CancellationExpiry != null,
                                HasBoard = !General.NoboardIds.Contains(rate.BoardType ?? 0)
                            },
                            RateDescription = rate.RateDescription,
                            Retail = rate.Retail,
                            Pricing = rate.Pricing,
                            BoardType = rate.BoardType,
                            RemainingRooms = rate.RemainingRooms
                        }).ToList()
                    }).ToList(),
                    Alternatives = result.Data.Alternatives.Select(a => new Alternative
                    {
                        CheckIn = a.CheckIn.ToDateTime(TimeOnly.MinValue),
                        Checkout = a.CheckOut.ToDateTime(TimeOnly.MinValue),
                        Nights = a.Nights,
                        MinPrice = a.MinPrice,
                        NetPrice = a.NetPrice
                    }).ToList().GetFinalPrice(disc, hotelCode, couponType)
                }
            };
        }

        public static decimal GetSalePrice(this HotelRate rate)
        {
            decimal saleprice = rate.Retail.TotalPrice + rate.Retail.Discount;
            if (saleprice > rate.totalPrice + 5)
            {
                return saleprice;
            }
            return 0;
        }

        public static decimal GetTotalPrice(this HotelRate rate, string code, decimal disc, TravelBridge.Contracts.Common.CouponType couponType)
        {
            decimal PricePerc = 0.95m;
            decimal extraDiscPer = 1m;
            decimal extraDisc = 0m;

            if (Helpers.General.hotelCodes.Contains(code))
            {
                PricePerc = 1m;
            }

            if (disc != 0m)
            {
                if (couponType == TravelBridge.Contracts.Common.CouponType.percentage)
                    extraDiscPer = 1 - disc;
                else if (couponType == TravelBridge.Contracts.Common.CouponType.flat)
                {
                    extraDisc = disc;
                }
            }

            var minMargin = rate.Pricing.TotalPrice * 10 / 100;
            if (rate.Pricing.Margin < minMargin || (rate.Retail.TotalPrice - rate.Pricing.TotalPrice) < minMargin || rate.Retail == null || rate.Retail.TotalPrice == 0)
            {
                rate.totalPrice = decimal.Floor(((rate.Pricing.TotalPrice + minMargin) * PricePerc * extraDiscPer) - extraDisc);
                rate.ProfitPerc = decimal.Round(rate.totalPrice / rate.Pricing.TotalPrice, 6);
                return rate.totalPrice;
            }
            else
            {
                rate.totalPrice = decimal.Floor((rate.Retail.TotalPrice * PricePerc * extraDiscPer) - extraDisc);
                rate.ProfitPerc = decimal.Round(rate.totalPrice / rate.Pricing.TotalPrice, 6);
                return rate.totalPrice;
            }
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

        public static List<StringAmount> MapToList(this IEnumerable<CancellationFee> cancellationFees, DateTime checkIn, HotelRate rate)
        {
            var result = new List<StringAmount>();
            if (cancellationFees == null || !cancellationFees.Any())
                return result;

            var fees = cancellationFees.OrderBy(c => c.After).ToList();
            CancellationFee? previous = null;

            // Ensure today exists or add an initial entry for the first cancellation date
            if (fees.First().After?.Date > DateTime.UtcNow.Date)
            {
                result.Add(new StringAmount
                {
                    Description = $"έως {fees.First().After:dd/MM/yyyy HH:00}",
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
                        Description = $"έως {fee.After.Value.AddHours(General.offset):dd/MM/yyyy HH:00}",
                        Amount = decimal.Round((previous.Fee ?? 0) * rate.ProfitPerc, 2)
                    });
                }
                previous = fee;
            }

            // Ensure the last entry covers up to check-out if necessary
            var last = fees.Last();
            if (last.After?.Date != checkIn.Date && last.Fee > 0)
            {
                result.Add(new StringAmount
                {
                    Description = $"έως {checkIn.AddHours(General.offset):dd/MM/yyyy}",
                    Amount = decimal.Round((last.Fee ?? 0) * rate.ProfitPerc, 2)
                });
            }
            if (fees.Min(a => a.After) < rate.Payments?.Min(a => a.DueDate))
            {

            }


            CalculatePayments(rate, checkIn, General.offset);

            return result;
        }

        private static void CalculatePayments(HotelRate rate, DateTime checkIn, int offset)
        {
            if (rate.Payments.IsNullOrEmpty())
            {
                throw new InvalidOperationException("No cancellation fees available.");
            }
            if (rate.CancellationExpiry == null && rate.Payments?.Any() == true)
            {
                rate.Payments = new List<PaymentWH> {
                new PaymentWH
                {
                    Amount=rate.Payments.Sum(p=>p.Amount),
                    DueDate = rate.Payments.First().DueDate
                }
                };
            }

            List<PaymentWH> payments = rate.Payments?.Select(a => new PaymentWH { Amount = a.Amount, DueDate = a.DueDate }).ToList() ?? new List<PaymentWH>();
            var first = payments.First();
            if (first.Amount == 0)
            {
                throw new InvalidOperationException("Invalid cancelation fee value.");
            }

            if (payments.Count == 1)
            {
                if (first.DueDate?.Date <= DateTime.UtcNow.Date)
                {
                    rate.Payments = new List<PaymentWH>
                    {
                        new() {
                            DueDate = first.DueDate?.AddHours(offset)??DateTime.UtcNow.AddHours(offset),
                            Amount = rate.totalPrice
                        }
                    };
                    return;
                }
                else
                {
                    rate.Payments = new List<PaymentWH>
                    {
                        new() {
                            DueDate = DateTime.UtcNow.AddHours(offset),
                            Amount = decimal.Round((rate.totalPrice*0.3m), 0)
                        }
                    };

                    rate.Payments.Add(new PaymentWH
                    {
                        DueDate = first.DueDate?.Date,
                        Amount = rate.totalPrice - rate.Payments.Sum(p => p.Amount)
                    });
                    return;
                }
            }

            if (payments.Count > 1)
            {
                if (first.DueDate?.Date <= DateTime.UtcNow.Date)
                {
                    rate.Payments = new List<PaymentWH>
                    {
                        new() {
                            DueDate = first.DueDate?.AddHours(offset)??DateTime.UtcNow.AddHours(offset),
                            Amount = decimal.Round((first.Amount ?? 0) * rate.ProfitPerc, 2)
                        }
                    };

                    for (int i = 1; i < payments.Count - 1; i++)
                    {
                        var pay = payments.ElementAt(i);
                        rate.Payments.Add(new PaymentWH
                        {
                            DueDate = pay.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                            Amount = decimal.Round((pay.Amount ?? 0) * rate.ProfitPerc, 2)
                        });
                    }
                    var last = payments.Last() ?? throw new InvalidOperationException("Invalid cancelation fee value");
                    rate.Payments.Add(new PaymentWH
                    {
                        DueDate = last.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                        Amount = rate.totalPrice - rate.Payments.Sum(p => p.Amount)
                    });

                    return;
                }
                else
                {
                    if (first.Amount <= rate.totalPrice * 0.4m)
                    {
                        rate.Payments = new List<PaymentWH>
                        {
                            new() {
                                DueDate = DateTime.UtcNow.AddHours(offset),
                                Amount = decimal.Round((first.Amount ?? 0) * rate.ProfitPerc, 2)
                            }
                        };

                        for (int i = 1; i < payments.Count - 1; i++)
                        {
                            var pay = payments.ElementAt(i);
                            rate.Payments.Add(new PaymentWH
                            {
                                DueDate = pay.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                                Amount = decimal.Round((pay.Amount ?? 0) * rate.ProfitPerc, 2)
                            });
                        }
                        var last = payments.Last() ?? throw new InvalidOperationException("Invalid cancelation fee value");
                        rate.Payments.Add(new PaymentWH
                        {
                            DueDate = last.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                            Amount = rate.totalPrice - rate.Payments.Sum(p => p.Amount)
                        });

                        return;
                    }
                    else //the first payment is big so we will split it in two. first will be 30% of total
                    {
                        rate.Payments = new List<PaymentWH>
                        {
                            new() {
                                DueDate = DateTime.UtcNow.AddHours(offset),
                                Amount = decimal.Round((rate.totalPrice * 0.3m), 0)
                            }
                        };

                        rate.Payments.Add(new PaymentWH
                        {
                            DueDate = DateTime.UtcNow.AddHours(offset),
                            Amount = decimal.Round(first.Amount!.Value - rate.Payments.Sum(p => p.Amount.Value), 2)
                        });

                        for (int i = 1; i < payments.Count - 1; i++)
                        {
                            var pay = payments.ElementAt(i);
                            rate.Payments.Add(new PaymentWH
                            {
                                DueDate = pay.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                                Amount = decimal.Round((pay.Amount ?? 0) * rate.ProfitPerc, 2)
                            });
                        }
                        var last = payments.Last() ?? throw new InvalidOperationException("Invalid cancelation fee value");
                        rate.Payments.Add(new PaymentWH
                        {
                            DueDate = last.DueDate?.AddHours(offset) ?? throw new InvalidOperationException("Invalid cancelation fee value"),
                            Amount = rate.totalPrice - rate.Payments.Sum(p => p.Amount)
                        });

                        return;
                    }
                }
            }
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
                //b.Boards.FirstOrDefault(b => b.Id == 14).Name += " -  δεν θα φαινεται";
                b.Boards.RemoveAll(b => b.Id == 14);
                return;
            }

            if (b.Boards.Count > 1)
            {
                b.BoardsText = "Επιλογές Διατροφής:";
                b.HasBoards = true;
                return;
            }
        }

        public static void SetBoardsText(this CheckoutRateProperties b, Language lang = Language.el)
        {
            if (b.Board == null || b.BoardId == null)
            {
                b.Board = "";
                b.HasBoard = false;
                return;
            }

            if (b.BoardId == 14)
            {
                b.Board = "Χωρίς διατροφή";
                b.HasBoard = false;
                return;
            }

            if (BoardTypes.ContainsKey(b.BoardId.Value))
            {
                BoardTypes[b.BoardId.Value].TryGetValue(lang, out string? value);
                if (value != null)
                {
                    b.Board = value;
                    b.HasBoard = true;
                }
                else
                {
                    b.Board = "Χωρίς διατροφή";
                    b.HasBoard = false;
                }
            }
        }
    }
}