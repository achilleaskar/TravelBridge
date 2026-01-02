namespace TravelBridge.Application.Models;

/// <summary>
/// Represents guest party composition for a room.
/// Application-layer domain model.
/// </summary>
public class PartyItem
{
    public int Adults { get; set; }
    public int[]? Children { get; set; }
    public int RoomsCount { get; set; }
    public string? Party { get; set; }

    public bool Equals(PartyItem? other)
    {
        if (other == null)
            return false;

        return Adults == other.Adults &&
               ((Children == null && other.Children == null) ||
               (Children != null && other.Children != null && Children.SequenceEqual(other.Children)));
    }

    public override bool Equals(object? obj) => Equals(obj as PartyItem);

    public override int GetHashCode()
    {
        int hash = Adults.GetHashCode();
        if (Children != null)
        {
            foreach (var child in Children)
                hash = hash * 31 + child.GetHashCode();
        }
        return hash;
    }
}
