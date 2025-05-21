using System.Runtime.InteropServices;
using System.Text.Json;
using TravelBridge.API.Contracts;
using TravelBridge.API.Models.DB;
using TravelBridge.API.Models.WebHotelier;

namespace TravelBridge.API.Helpers
{
    public static class General
    {
        public static string CreateParty(int adults, string? children)
        {
            // Check if children is null or empty and build JSON accordingly
            if (string.IsNullOrWhiteSpace(children) || children == "0")
            {
                return $"[{{\"adults\":{adults}}}]";
            }
            else
            {
                return $"[{{\"adults\":{adults},\"children\":[{children}]}}]";
            }
        }

        public static int[] NoboardIds = new int[] { 0, 14 };

        public static string BuildMultiRoomJson(string party)
        {
            // Validate and return the party JSON
            try
            {
                // Attempt to parse to ensure the input is valid JSON
                JsonSerializer.Deserialize<List<Dictionary<string, object>>>(party);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid party data format. Ensure it's valid JSON.", ex);
            }

            return party;
        }

        public static List<SelectedRate>? RatesToList(string selectedRates)
        {
            try
            {
                return JsonSerializer.Deserialize<List<SelectedRate>>(selectedRates);
            }
            catch (JsonException ex)
            {
                throw new ArgumentException("Invalid party data format. Ensure it's valid JSON.", ex);
            }
        }

        private static int _offset = -1;


        public static int offset
        {
            get
            {
                if (_offset == -1)
                {
                    string timeZoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "GTB Standard Time" : "Europe/Athens";

                    TimeZoneInfo greekTimeZone;
                    try
                    {
                        greekTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        greekTimeZone = TimeZoneInfo.Local; // Fallback to system time
                    }
                    _offset = (int)greekTimeZone.GetUtcOffset(DateTime.UtcNow).TotalHours;
                }
                return _offset;
            }

        }


        public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source)
        {
            return source == null || !source.Any();
        }
        public static PartialPayment? FillPartialPayment(List<PaymentWH>? payments, DateTime checkIn)
        {
            if (payments.IsNullOrEmpty()
                || payments[0].DueDate?.Date != DateTime.UtcNow.AddHours(offset).Date
                || !payments.Any(p => p.DueDate?.Date > DateTime.UtcNow.AddHours(offset).Date))
            {
                return null;
            }

            return new PartialPayment
            {
                prepayAmount = payments.Where(p => p.DueDate?.Date <= DateTime.UtcNow.AddHours(offset).Date).Sum(a => a.Amount) ?? throw new InvalidOperationException("Payments calculation failure."),
                nextPayments = MergeNextPayments(payments.Where(p => p.DueDate?.Date > DateTime.UtcNow.AddHours(offset).Date).Select(a => new PaymentWH { Amount = a.Amount, DueDate = a.DueDate }).ToList(), checkIn)
            };
        }

        private static List<PaymentWH> MergeNextPayments(List<PaymentWH> payments, DateTime checkIn)
        {
            if (payments.Any(p => p.DueDate == null || p.Amount <= 0))
            {
                throw new InvalidOperationException("Error on calculating payments");
            }

            // Step 1: Merge same-day payments
            var grouped = payments
                .GroupBy(p => p.DueDate!.Value.Date)
                .Select(g => new PaymentWH
                {
                    DueDate = g.Key,
                    Amount = g.Sum(p => p.Amount)
                })
                .OrderBy(p => p.DueDate)
                .ToList();

            // Step 2: Merge based on day difference from 1 to 10
            for (int maxDays = 1; maxDays <= 10 && grouped.Count > 2; maxDays++)
            {
                var merged = new List<PaymentWH>();
                int i = 0;

                while (i < grouped.Count)
                {
                    var current = grouped[i];
                    int j = i + 1;

                    // Try to merge with next if within maxDays
                    while (j < grouped.Count && (grouped[j].DueDate!.Value - current.DueDate!.Value).TotalDays <= maxDays)
                    {
                        current.Amount += grouped[j].Amount;
                        j++;
                    }

                    merged.Add(new PaymentWH { DueDate = current.DueDate, Amount = current.Amount });
                    i = j;
                }

                grouped = merged.OrderBy(p => p.DueDate).ToList();
            }

            // Step 3: If still more than 2 left, merge based on halves
            if (grouped.Count >= 2)
            {
                // Extra merge if check-in is far from today
                if ((checkIn - DateTime.Today).TotalDays > 12)
                {
                    // Try to merge final payments that are <= 3 days apart
                    var temp = new List<PaymentWH>();
                    int i = 0;

                    while (i < grouped.Count)
                    {
                        var current = grouped[i];
                        int j = i + 1;

                        while (j < grouped.Count && (grouped[j].DueDate!.Value - current.DueDate!.Value).TotalDays <= 3)
                        {
                            current.Amount += grouped[j].Amount;
                            j++;
                        }

                        temp.Add(new PaymentWH { DueDate = current.DueDate, Amount = current.Amount });
                        i = j;
                    }

                    grouped = temp.OrderBy(p => p.DueDate).ToList();
                }

                // If still more than 2 after the extra logic, do the fallback merge
                if (grouped.Count > 2)
                {
                    var firstHalf = grouped.Take(grouped.Count / 2).ToList();
                    var secondHalf = grouped.Skip(grouped.Count / 2).ToList();

                    grouped = new List<PaymentWH>
                    {
                        new PaymentWH
                        {
                            DueDate = firstHalf.First().DueDate,
                            Amount = firstHalf.Sum(p => p.Amount)
                        },
                        new PaymentWH
                        {
                            DueDate = secondHalf.First().DueDate,
                            Amount = secondHalf.Sum(p => p.Amount)
                        }
                    };
                }
            }


            return grouped;
        }

        public class SelectedRate
        {
            public string rateId { get; set; }
            public string roomId { get; set; }
            public int count { get; set; }
            public string roomType { get; set; }
            public string searchParty { get; set; }

            internal void FillPartyFromId()
            {
                var parts = rateId.Split('-');
                //rateId = parts[0]; // Keep the first part as the rate ID
                if (parts.Length < 2)
                    throw new ArgumentException("ID does not contain party info suffix.");

                var partySegment = parts.Last();

                if (string.IsNullOrEmpty(partySegment))
                    throw new ArgumentException("Party segment must contain only digits.");

                // First digit is adults
                int adults = partySegment[0] - '0';

                var segments = partySegment.Split('_');
                // Remaining digits are children
                var children = segments.Length > 1
                        ? segments.Skip(1).Select(int.Parse).ToArray()
                        : Array.Empty<int>();

                if (children?.Length > 0)
                    searchParty = $"[{{\"adults\":{adults},\"children\":[{string.Join(',', children)}]}}]";
                else
                    searchParty = $"[{{\"adults\":{adults}}}]";
            }
        }

    }
}
