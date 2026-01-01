namespace TravelBridge.Core.Entities
{
    /// <summary>
    /// Shared party information model - base for all party representations.
    /// Used across providers and database.
    /// </summary>
    public class PartyInfo
    {
        public int Adults { get; set; }
        public int[] Children { get; set; } = [];

        /// <summary>
        /// Total number of guests.
        /// </summary>
        public int TotalGuests => Adults + Children.Length;

        /// <summary>
        /// Number of children.
        /// </summary>
        public int ChildrenCount => Children.Length;

        /// <summary>
        /// Creates a party info from adults count and children ages.
        /// </summary>
        public static PartyInfo Create(int adults, int[]? children = null)
        {
            return new PartyInfo
            {
                Adults = adults,
                Children = children ?? []
            };
        }

        /// <summary>
        /// Creates a party info from adults count and children as comma-separated string.
        /// </summary>
        public static PartyInfo Create(int adults, string? childrenAges)
        {
            int[] children = [];
            if (!string.IsNullOrWhiteSpace(childrenAges))
            {
                children = childrenAges
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray();
            }
            return Create(adults, children);
        }

        /// <summary>
        /// Returns children ages as comma-separated string.
        /// </summary>
        public string ChildrenAsString()
        {
            return Children.Length > 0 ? string.Join(",", Children) : "";
        }

        /// <summary>
        /// Returns party as JSON string for API calls.
        /// </summary>
        public string ToJsonString()
        {
            if (Children.Length == 0)
                return $"[{{\"adults\":{Adults}}}]";
            else
                return $"[{{\"adults\":{Adults},\"children\":[{string.Join(",", Children)}]}}]";
        }

        /// <summary>
        /// Gets a human-readable description (Greek).
        /// </summary>
        public string GetDescription()
        {
            var parts = new List<string>();
            
            if (Adults > 0)
                parts.Add(Adults == 1 ? "1 ενήλικας" : $"{Adults} ενήλικες");
            
            if (Children.Length > 0)
                parts.Add(Children.Length == 1 ? "1 παιδί" : $"{Children.Length} παιδιά");
            
            return string.Join(", ", parts);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not PartyInfo other) return false;
            return Adults == other.Adults && Children.SequenceEqual(other.Children);
        }

        public override int GetHashCode()
        {
            int hash = Adults.GetHashCode();
            foreach (var child in Children)
                hash = hash * 31 + child.GetHashCode();
            return hash;
        }
    }
}
