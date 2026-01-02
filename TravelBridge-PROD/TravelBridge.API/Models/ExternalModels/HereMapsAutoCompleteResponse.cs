namespace TravelBridge.API.Models.ExternalModels
{
    public class HereMapsAutoCompleteResponse
    {
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        public string Title { get; set; }
        public string ResultType { get; set; }
        public string LocalityType { get; set; }
        public Address Address { get; set; }
    }

    public class Address
    {
        public string Label { get; set; }
        public string CountryName { get; set; }
        public string State { get; set; }
        public string County { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
    }
}
