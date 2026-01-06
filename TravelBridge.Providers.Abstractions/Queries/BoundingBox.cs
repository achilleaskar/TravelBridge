namespace TravelBridge.Providers.Abstractions.Queries;

/// <summary>
/// Geographic bounding box for location-based searches.
/// Defines a rectangular area using bottom-left and top-right coordinates.
/// </summary>
/// <param name="BottomLeftLatitude">Southern boundary latitude</param>
/// <param name="BottomLeftLongitude">Western boundary longitude</param>
/// <param name="TopRightLatitude">Northern boundary latitude</param>
/// <param name="TopRightLongitude">Eastern boundary longitude</param>
public record BoundingBox(
    string BottomLeftLatitude,
    string BottomLeftLongitude,
    string TopRightLatitude,
    string TopRightLongitude
)
{
    /// <summary>
    /// Parses a bounding box from the format: [lon1,lat1,lon2,lat2]
    /// </summary>
    public static BoundingBox Parse(string bboxString)
    {
        if (string.IsNullOrWhiteSpace(bboxString))
            throw new ArgumentException("Bounding box string cannot be empty.", nameof(bboxString));

        var parts = bboxString.Trim('[', ']').Split(',');
        if (parts.Length != 4)
            throw new ArgumentException("Invalid bounding box format. Expected [lon1,lat1,lon2,lat2]", nameof(bboxString));

        var lon1 = double.Parse(parts[0]);
        var lat1 = double.Parse(parts[1]);
        var lon2 = double.Parse(parts[2]);
        var lat2 = double.Parse(parts[3]);

        return new BoundingBox(
            BottomLeftLatitude: Math.Min(lat1, lat2).ToString(),
            TopRightLatitude: Math.Max(lat1, lat2).ToString(),
            BottomLeftLongitude: Math.Min(lon1, lon2).ToString(),
            TopRightLongitude: Math.Max(lon1, lon2).ToString()
        );
    }
}
