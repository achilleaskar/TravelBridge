using System.Text.Json.Serialization;
using TravelBridge.API.Models;

namespace TravelBridge.API.Contracts
{
    public class HotelInfoResponse
    {
        [JsonPropertyName("error_code")]
        public string ErrorCode { get; set; }

        [JsonPropertyName("error_msg")]
        public string ErrorMsg { get; set; }


        [JsonPropertyName("data")]
        public HotelData Data { get; set; }
    }

    public class HotelData
    {
        private decimal salePrice;

        [JsonPropertyName("code")]
        public string Code { internal get; set; }

        [JsonIgnore]
        public Provider Provider { get; set; }

        public decimal MinPrice { get; set; }


        public decimal SalePrice
        {
            get => salePrice;
            set
            {
                if (value > MinPrice)
                    salePrice = value;
                else
                    salePrice = 0;
            }
        }

        public string CustomInfo { get; set; }

        [JsonPropertyName("boardNames")]
        public Dictionary<int, string> BoardNames { get; set; }

        [JsonPropertyName("mappedTypes")]
        public HashSet<string> MappedTypes { get; set; }
        public decimal MinPricePerNight { get; set; }
        public string Id => $"{(int)Provider}-{Code}";

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("rating")]
        public int Rating { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("general_terms")]
        public string GeneralTerms { get; set; }

        [JsonPropertyName("directions")]
        public string Directions { get; set; }

        [JsonPropertyName("location")]
        public LocationInfo Location { get; set; }

        [JsonPropertyName("children")]
        public ChildrenPolicy Children { get; set; }

        [JsonPropertyName("operation")]
        public HotelOperation Operation { get; set; }

        //[JsonPropertyName("rooms")]
        //public List<RoomInfo> Rooms { get; set; }

        [JsonPropertyName("facilities")]
        public IEnumerable<string> Facilities { get; set; }

        [JsonPropertyName("photos")]
        public IEnumerable<PhotoInfo> PhotosItems { get; set; }

        public IEnumerable<string> LargePhotos { get; set; }

        [JsonPropertyName("logourl")]
        public string LogoUrl { get; set; }
    }

    //public class ContactInfo
    //{
    //    [JsonPropertyName("tel")]
    //    public string Tel { get; set; }

    //    [JsonPropertyName("fax")]
    //    public string Fax { get; set; }

    //    [JsonPropertyName("email")]
    //    public string Email { get; set; }

    //    [JsonPropertyName("skype")]
    //    public string Skype { get; set; }
    //}

    public class LocationInfo
    {
        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lon")]
        public double Longitude { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }

        [JsonPropertyName("zip")]
        public string Zip { get; set; }

        [JsonPropertyName("country")]
        public string Country { get; set; }
    }

    public class ChildrenPolicy
    {
        [JsonPropertyName("allowed")]
        public byte Allowed { get; set; }

        [JsonPropertyName("age_from")]
        public int AgeFrom { get; set; }

        [JsonPropertyName("age_to")]
        public int AgeTo { get; set; }

        [JsonPropertyName("policy")]
        public string Policy { get; set; }
    }

    public class HotelOperation
    {
        [JsonPropertyName("checkout_time")]
        public string CheckoutTime { get; set; }

        [JsonPropertyName("checkin_time")]
        public string CheckinTime { get; set; }

    }

    //public class RoomInfo
    //{
    //    [JsonPropertyName("code")]
    //    public string Code { get; set; }

    //    [JsonPropertyName("name")]
    //    public string Name { get; set; }

    //}

    public class PhotoInfo
    {
        [JsonPropertyName("medium")]
        public string Medium { get; set; }

        [JsonPropertyName("large")]
        public string Large { get; set; }
    }

}