using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using TravelBridge.API.Helpers;

namespace TravelBridge.API.Models.WebHotelier
{
    public class PartyItem
    {
        public PartyItem()
        {

        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int adults { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int[] children { get; set; }

        [JsonIgnore]
        public int RoomsCount { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] 
        public string? party { get; set; }

        public bool Equals(PartyItem other)
        {
            if (other == null)
                return false;

            return adults == other.adults &&
                   ((children == null && other.children == null) ||
                   (children != null && other.children != null && children.SequenceEqual(other.children)));
        }

        public override bool Equals(object obj) => Equals(obj as PartyItem);

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
