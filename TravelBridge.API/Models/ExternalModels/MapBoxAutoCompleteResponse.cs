namespace TravelBridge.API.Models.ExternalModels
{
    public class MapBoxAutoCompleteResponse
    {
        [JsonPropertyName("features")]
        public List<Feature> Features { get; set; }
    }

    public class Feature
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("properties")]
        public Properties Properties { get; set; }
    }

    public class Properties
    {

        [JsonPropertyName("feature_type")]
        public string? FeatureType { get; set; }

        [JsonPropertyName("name_preferred")]
        public string NamePreferred { get; set; }

        [JsonPropertyName("coordinates")]
        public Coordinate Coordinates { get; set; }

        [JsonPropertyName("bbox")]
        public List<double> Bbox { get; set; }

        [JsonPropertyName("context")]
        public Context Context { get; set; }
    }

    public class Coordinate
    {
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }
    }

    public class Context
    {
        [JsonPropertyName("region")]
        public Region Region { get; set; }

        [JsonPropertyName("country")]
        public Country Country { get; set; }

        [JsonPropertyName("place")]
        public Place Place { get; set; }
    }

    public class Region
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("translations")]
        public Translations? Translations { get; set; }
    }

    public class Country
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("country_code")]
        public string CountryCode { get; set; }

        [JsonPropertyName("translations")]
        public Translations? Translations { get; set; }
    }

    public class Place
    {

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("translations")]
        public Translations? Translations { get; set; }
    }

    public class Translations
    {
        [JsonPropertyName("el")]
        public LanguageTranslation? El { get; set; }

        [JsonPropertyName("en")]
        public LanguageTranslation? En { get; set; }
    }

    public class LanguageTranslation
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
