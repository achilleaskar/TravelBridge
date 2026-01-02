namespace TravelBridge.Geo.HereMaps;

internal class HereMapsAutoCompleteResponse
{
    public List<Item> Items { get; set; } = [];
}

internal class Item
{
    public string Title { get; set; } = null!;
    public string ResultType { get; set; } = null!;
    public string LocalityType { get; set; } = null!;
    public Address Address { get; set; } = null!;
}

internal class Address
{
    public string Label { get; set; } = null!;
    public string CountryName { get; set; } = null!;
    public string State { get; set; } = null!;
    public string County { get; set; } = null!;
    public string City { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
}
