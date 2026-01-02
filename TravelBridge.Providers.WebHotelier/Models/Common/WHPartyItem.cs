namespace TravelBridge.Providers.WebHotelier.Models.Common;

/// <summary>
/// Internal WebHotelier wire model for party/guest composition.
/// Maps to/from WebHotelier API JSON.
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

    public bool Equals(WHPartyItem? other)
    {
        if (other == null)
            return false;

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
