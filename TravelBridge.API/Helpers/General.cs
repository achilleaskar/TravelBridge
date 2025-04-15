using System.Runtime.InteropServices;
using System.Text.Json;
using TravelBridge.API.Contracts;

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
        public static PartialPayment? FillPartialPayment(List<PaymentWH>? payments)
        {
            if (payments.IsNullOrEmpty() || payments[0].DueDate?.Date != DateTime.UtcNow.AddHours(offset).Date || !payments.Any(p => p.DueDate?.Date > DateTime.UtcNow.AddHours(offset).Date))
            {
                return null;
            }

            return new PartialPayment
            {
                prepayAmount = payments.Where(p => p.DueDate?.Date <= DateTime.UtcNow.AddHours(offset).Date).Sum(a => a.Amount) ?? throw new InvalidOperationException("Payments calculation failure."),
                nextPayments = payments.Where(p => p.DueDate?.Date > DateTime.UtcNow.AddHours(offset).Date).Select(a => new PaymentWH { Amount = a.Amount, DueDate = a.DueDate }).ToList()
                //TODO: check total amount
            };
        }
        public class SelectedRate
        {
            public string rateId { get; set; }
            public string roomId { get; set; }
            public int count { get; set; }
            public string roomType { get; set; }
            public string searchParty { get; set; }
        }

    }
}
