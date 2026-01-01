namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Represents a selected rate for booking.
    /// Provider-agnostic model.
    /// </summary>
    public class SelectedRate
    {
        public string RateId { get; set; } = "";
        public string? RoomId { get; set; }
        public string? RoomType { get; set; }
        public int Count { get; set; }
        public PartyInfo Party { get; set; } = new();

        /// <summary>
        /// Creates from rate ID with party info encoded in the ID.
        /// Format: "rateId-adults_child1_child2"
        /// </summary>
        public static SelectedRate FromEncodedId(string encodedRateId, int count)
        {
            var result = new SelectedRate
            {
                RateId = encodedRateId,
                Count = count
            };

            var parts = encodedRateId.Split('-');
            if (parts.Length >= 2)
            {
                var partySegment = parts.Last();
                if (!string.IsNullOrEmpty(partySegment) && char.IsDigit(partySegment[0]))
                {
                    int adults = partySegment[0] - '0';
                    var segments = partySegment.Split('_');
                    var childrenAges = segments.Length > 1
                        ? segments.Skip(1).Select(int.Parse).ToArray()
                        : [];

                    result.Party = PartyInfo.Create(adults, childrenAges);
                }
            }

            return result;
        }
    }
}
