using System.Text.Json.Serialization;

namespace TravelBridge.Geo.Mapbox;

internal class MapBoxAutoCompleteResponse
{
    [JsonPropertyName("features")]
    public List<Feature> Features { get; set; } = [];
}

internal class Feature
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("properties")]
    public Properties Properties { get; set; } = null!;
}

internal class Properties
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

internal class Coordinate
{
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }

    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
}

internal class Context
{
    [JsonPropertyName("region")]
    public Region Region { get; set; } = null!;

    [JsonPropertyName("country")]
    public Country Country { get; set; } = null!;

    [JsonPropertyName("place")]
    public Place Place { get; set; } = null!;
}

internal class Region
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("translations")]
    public Translations? Translations { get; set; }
}

internal class Country
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = null!;

    [JsonPropertyName("translations")]
    public Translations? Translations { get; set; }
}

internal class Place
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("translations")]
    public Translations? Translations { get; set; }
}

internal class Translations
{
    [JsonPropertyName("el")]
    public LanguageTranslation? El { get; set; }

    [JsonPropertyName("en")]
    public LanguageTranslation? En { get; set; }
}

internal class LanguageTranslation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
