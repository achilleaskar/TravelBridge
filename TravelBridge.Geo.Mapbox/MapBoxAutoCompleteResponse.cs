using System.Text.Json.Serialization;

namespace TravelBridge.Geo.Mapbox;

public class MapBoxAutoCompleteResponse
{
    [JsonPropertyName("features")]
    public List<Feature> Features { get; set; } = [];
}

public class Feature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; } = null!;
}

public class Properties
{
    [JsonPropertyName("feature_type")]
    public string? FeatureType { get; set; }

    [JsonPropertyName("name_preferred")]
    public string NamePreferred { get; set; } = null!;

    [JsonPropertyName("coordinates")]
    public Coordinate Coordinates { get; set; } = null!;

    [JsonPropertyName("bbox")]
    public List<double> Bbox { get; set; } = [];

    [JsonPropertyName("context")]
    public Context Context { get; set; } = null!;
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
    public Region Region { get; set; } = null!;

    [JsonPropertyName("country")]
    public Country Country { get; set; } = null!;

    [JsonPropertyName("place")]
    public Place Place { get; set; } = null!;
}

public class Region
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("translations")]
    public Translations? Translations { get; set; }
}

public class Country
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = null!;

    [JsonPropertyName("translations")]
    public Translations? Translations { get; set; }
}

public class Place
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

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
