namespace TravelBridge.Contracts.Responses
{
    /// <summary>
    /// Autocomplete response for search.
    /// </summary>
    public class AutoCompleteResponse
    {
        public IReadOnlyList<AutoCompleteHotel> Hotels { get; init; } = [];
        public IReadOnlyList<AutoCompleteLocation> Locations { get; init; } = [];
    }

    /// <summary>
    /// Hotel in autocomplete results.
    /// </summary>
    public class AutoCompleteHotel
    {
        public required string Id { get; init; }
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Location { get; init; }
        public string? CountryCode { get; init; }
        public string? Type { get; init; }
        public IReadOnlyList<string> MappedTypes { get; init; } = [];
    }

    /// <summary>
    /// Location in autocomplete results.
    /// </summary>
    public class AutoCompleteLocation
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public string? Region { get; init; }
        public string? CountryCode { get; init; }
    }
}
