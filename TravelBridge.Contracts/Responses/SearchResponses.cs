namespace TravelBridge.Contracts.Responses
{
    /// <summary>
    /// Response for hotel search (multi-availability).
    /// </summary>
    public class SearchResponse
    {
        public IReadOnlyList<SearchHotelResult> Results { get; init; } = [];
        public int ResultsCount { get; init; }
        public IReadOnlyList<SearchFilter> Filters { get; init; } = [];
        public string? SearchTerm { get; init; }
    }

    /// <summary>
    /// Hotel result in search.
    /// </summary>
    public class SearchHotelResult
    {
        public required string Id { get; init; }
        public required string Code { get; init; }
        public required string Name { get; init; }
        public string? Type { get; init; }
        public int? Rating { get; init; }
        public string? PhotoUrl { get; init; }
        public SearchLocation? Location { get; init; }
        public decimal? MinPrice { get; init; }
        public decimal? MinPricePerDay { get; init; }
        public decimal? SalePrice { get; init; }
        public IReadOnlyList<string> MappedTypes { get; init; } = [];
        public IReadOnlyList<BoardInfo> Boards { get; init; } = [];
        public string? BoardsText { get; init; }
        public bool HasBoards { get; init; }
    }

    /// <summary>
    /// Location info for search results.
    /// </summary>
    public class SearchLocation
    {
        public double? Latitude { get; init; }
        public double? Longitude { get; init; }
        public string? Name { get; init; }
        public string? Country { get; init; }
    }

    /// <summary>
    /// Board/meal plan info.
    /// </summary>
    public class BoardInfo
    {
        public int Id { get; init; }
        public string? Name { get; init; }
    }

    /// <summary>
    /// Search filter.
    /// </summary>
    public class SearchFilter
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public FilterType Type { get; init; }
        public IReadOnlyList<FilterValue>? Values { get; init; }
        public decimal? MinValue { get; init; }
        public decimal? MaxValue { get; init; }
        public bool ShowAll { get; init; }
    }

    /// <summary>
    /// Filter value option.
    /// </summary>
    public class FilterValue
    {
        public required string Id { get; init; }
        public required string Name { get; init; }
        public int Count { get; init; }
        public int FilteredCount { get; init; }
        public bool Selected { get; init; }
    }

    /// <summary>
    /// Filter type.
    /// </summary>
    public enum FilterType
    {
        Range = 1,
        Values = 2
    }
}
