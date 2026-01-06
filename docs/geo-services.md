# TravelBridge Geo Services

This document describes the integration with geocoding and location services used for autocomplete functionality.

## Overview

TravelBridge uses location services for:
- Location autocomplete in search
- Converting location names to geographic coordinates
- Providing bounding boxes for area searches

Two providers are implemented:
- **MapBox** (Primary) - Used for production autocomplete
- **HereMaps** (Alternative) - Available as backup

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    TravelBridge.API                              │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              SearchPluginEndpoints                       │    │
│  │  - GetAutocompleteResults()                              │    │
│  └───────────────────────────┬─────────────────────────────┘    │
└──────────────────────────────┼──────────────────────────────────┘
                               │
              ┌────────────────┴────────────────┐
              │                                 │
              ▼                                 ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│  TravelBridge.Geo.Mapbox │     │TravelBridge.Geo.HereMaps│
│  ┌───────────────────┐  │     │  ┌───────────────────┐  │
│  │   MapBoxService   │  │     │  │  HereMapsService  │  │
│  └─────────┬─────────┘  │     │  └─────────┬─────────┘  │
└────────────┼────────────┘     └────────────┼────────────┘
             │                               │
             ▼                               ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│      MapBox API         │     │      HereMaps API       │
│  (geocode.earth/mapbox) │     │   (api.here.com)        │
└─────────────────────────┘     └─────────────────────────┘
```

## MapBox Integration

### Configuration

```json
{
  "MapBoxApi": {
    "BaseUrl": "https://api.mapbox.com",
    "ApiKey": "<mapbox_api_key>"
  }
}
```

### Service Registration

```csharp
// TravelBridge.Geo.Mapbox/ServiceCollectionExtensions.cs
public static IServiceCollection AddMapBox(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.Configure<MapBoxApiOptions>(
        configuration.GetSection("MapBoxApi"));

    services.AddHttpClient("MapBoxApi", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<MapBoxApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    });

    services.AddScoped<MapBoxService>();
    return services;
}
```

### MapBoxService

```csharp
public class MapBoxService
{
    public async Task<IEnumerable<AutoCompleteLocation>> GetLocationsAsync(
        string? param, 
        string? lang)
    {
        if (param is null || param.Length < 3)
            return [];

        var response = await _httpClient.GetAsync(
            $"search/geocode/v6/forward" +
            $"?q={Uri.EscapeDataString(param)}" +
            $"&country=gr,cy" +              // Greece and Cyprus only
            $"&limit={Limit}" +               // Max 10 results
            $"&types=neighborhood,region,country,place,district,locality" +
            $"&language={language}" +
            $"&autocomplete=true" +
            $"&access_token={_apiKey}" +
            $"&permanent=false");

        // Parse and map response
    }
}
```

### API Parameters

| Parameter | Value | Description |
|-----------|-------|-------------|
| `q` | Search term | URL-encoded search query |
| `country` | `gr,cy` | Limit to Greece and Cyprus |
| `limit` | `10` | Maximum results |
| `types` | `neighborhood,region,country,place,district,locality` | Location types to include |
| `language` | `el` | Response language (Greek) |
| `autocomplete` | `true` | Enable partial matching |
| `permanent` | `false` | Temporary/session results |

### Response Mapping

MapBox response is mapped to `AutoCompleteLocation`:

```csharp
private static IEnumerable<AutoCompleteLocation> MapToAutoCompleteLocations(
    List<Feature> features)
{
    return features
        .Where(f => f.Properties != null && 
                    f.Properties.FeatureType != "country")  // Exclude countries
        .Select(f => new AutoCompleteLocation(
            f.Properties.NamePreferred,
            f.Properties.Context.Region?.Name ?? "",
            // ID format: [bbox]-centerLat-centerLon
            $"[{string.Join(",", f.Properties.Bbox)}]-" +
            $"{f.Properties.Coordinates.Latitude}-" +
            $"{f.Properties.Coordinates.Longitude}",
            f.Properties.Context.Country.CountryCode,
            AutoCompleteType.location));
}
```

## HereMaps Integration

### Configuration

```json
{
  "HereMapsApi": {
    "BaseUrl": "https://autocomplete.search.hereapi.com/v1",
    "ApiKey": "<here_api_key>"
  }
}
```

### Service Registration

```csharp
// TravelBridge.Geo.HereMaps/ServiceCollectionExtensions.cs
public static IServiceCollection AddHereMaps(
    this IServiceCollection services, 
    IConfiguration configuration)
{
    services.Configure<HereMapsApiOptions>(
        configuration.GetSection("HereMapsApi"));

    services.AddHttpClient("HereMapsApi", (sp, client) =>
    {
        var options = sp.GetRequiredService<IOptions<HereMapsApiOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
    });

    services.AddScoped<HereMapsService>();
    return services;
}
```

### HereMapsService

```csharp
public class HereMapsService
{
    public async Task<IEnumerable<AutoCompleteLocation>> GetLocationsAsync(
        string? param, 
        string? lang)
    {
        // Similar implementation to MapBox
        // Uses HereMaps autocomplete API
    }
}
```

## AutoComplete Response Format

### AutoCompleteLocation

```csharp
public record AutoCompleteLocation(
    string Name,           // Display name (e.g., "Heraklion")
    string Region,         // Parent region (e.g., "Crete")
    string Id,             // Encoded location ID
    string CountryCode,    // ISO country code (e.g., "GR")
    AutoCompleteType Type  // "location"
);
```

### Location ID Format

The location ID encodes the bounding box and center coordinates:

```
[lon1,lat1,lon2,lat2]-centerLat-centerLon

Example:
[23.377258,34.730628,26.447346,35.773147]-35.340013-25.134348
     │         │         │         │          │         │
     └─────────┴─────────┴─────────┴──────────┼─────────┘
              Bounding Box                Center Point
```

This format allows:
1. Direct use in WebHotelier availability search
2. Map centering on the location
3. Proper geographic filtering

## Bounding Box Parsing

The `SearchPluginEndpoints` parses the bbox for search:

```csharp
private static BBox TryGetBBox(string locationId)
{
    // Remove brackets and split: "[23.377,34.730,26.447,35.773]"
    var parts = locationId.Trim('[', ']').Split(',');
    
    var lon1 = double.Parse(parts[0]);
    var lat1 = double.Parse(parts[1]);
    var lon2 = double.Parse(parts[2]);
    var lat2 = double.Parse(parts[3]);

    return new BBox
    {
        BottomLeftLatitude = Math.Min(lat1, lat2).ToString(),
        TopRightLatitude = Math.Max(lat1, lat2).ToString(),
        BottomLeftLongitude = Math.Min(lon1, lon2).ToString(),
        TopRightLongitude = Math.Max(lon1, lon2).ToString()
    };
}
```

## Autocomplete Flow

```
User types "Herak" 
    │
    ▼
SearchPluginEndpoints.GetAutocompleteResults("Herak")
    │
    ├──► WebHotelierPropertiesService.SearchPropertyFromWebHotelierAsync("Herak")
    │           │
    │           └──► Returns hotels matching "Herak*"
    │
    └──► MapBoxService.GetLocationsAsync("Herak", "el")
                │
                └──► Returns locations matching "Herak*"
    │
    ▼
Combine results
    │
    ▼
AutoCompleteResponse
{
  "hotels": [
    { "id": "1-HERAKLION", "name": "Heraklion Hotel", ... }
  ],
  "locations": [
    { "name": "Ηράκλειο", "region": "Κρήτη", "id": "[25.0,35.2,25.2,35.4]-35.3-25.1", ... }
  ]
}
```

## Country Filtering

Both services are configured to only return results from:
- **Greece (GR)**
- **Cyprus (CY)**

This is appropriate for the target market of the application.

## Feature Types

MapBox location types used:

| Type | Description |
|------|-------------|
| `place` | Cities, towns |
| `region` | States, provinces |
| `district` | City districts |
| `locality` | Neighborhoods |
| `neighborhood` | Sub-localities |

**Excluded**: `country` type to avoid returning just "Greece"

## Error Handling

```csharp
try
{
    var response = await _httpClient.GetAsync(...);
    response.EnsureSuccessStatusCode();
    // Process response
}
catch (HttpRequestException ex)
{
    throw new InvalidOperationException(ex.ToString());
}
```

On error, the autocomplete returns empty results rather than failing the entire request.

## Parallel Execution

Hotels and locations are fetched in parallel:

```csharp
var hotelsTask = webHotelierPropertiesService
    .SearchPropertyFromWebHotelierAsync(searchQuery);
var locationsTask = mapBoxService
    .GetLocationsAsync(searchQuery, "el");

await Task.WhenAll(hotelsTask, locationsTask);

var result = new AutoCompleteResponse
{
    Hotels = hotelsTask.Result.MapToAutoCompleteHotels(),
    Locations = locationsTask.Result
};
```

## Rate Limiting

MapBox and HereMaps have API rate limits. Current implementation does not include:
- Request throttling
- Caching of results
- Rate limit handling

**Recommendation**: Implement caching for common searches to reduce API calls.

## Future Improvements

1. **Caching**: Cache popular location searches
2. **Fallback**: Auto-switch to HereMaps if MapBox fails
3. **Rate Limiting**: Implement request throttling
4. **Result Ranking**: Improve sorting of results by relevance
5. **Internationalization**: Support multiple languages
