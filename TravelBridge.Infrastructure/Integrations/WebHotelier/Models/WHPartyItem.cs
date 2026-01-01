using System.Text.Json.Serialization;
using TravelBridge.Core.Entities;

namespace TravelBridge.Infrastructure.Integrations.WebHotelier.Models
{
    /// <summary>
    /// WebHotelier party item - represents room party configuration.
    /// </summary>
    public class WHPartyItem
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int adults { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int[]? children { get; set; }

        [JsonIgnore]
        public int RoomsCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? party { get; set; }

        /// <summary>
        /// Converts to Core PartyInfo.
        /// </summary>
        public PartyInfo ToPartyInfo()
        {
            return PartyInfo.Create(adults, children);
        }

        /// <summary>
        /// Creates from Core PartyInfo.
        /// </summary>
        public static WHPartyItem FromPartyInfo(PartyInfo partyInfo)
        {
            return new WHPartyItem
            {
                adults = partyInfo.Adults,
                children = partyInfo.Children.Length > 0 ? partyInfo.Children : null,
                party = partyInfo.ToJsonString()
            };
        }

        public bool Equals(WHPartyItem? other)
        {
            if (other == null) return false;
            return adults == other.adults &&
                   ((children == null && other.children == null) ||
                   (children != null && other.children != null && children.SequenceEqual(other.children)));
        }

        public override bool Equals(object? obj) => Equals(obj as WHPartyItem);

        public override int GetHashCode()
        {
            int hash = adults.GetHashCode();
            if (children != null)
            {
                foreach (var child in children)
                    hash = hash * 31 + child.GetHashCode();
            }
            return hash;
        }
    }
}
