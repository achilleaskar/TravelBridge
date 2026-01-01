namespace TravelBridge.Contracts.Plugin.Filters
{
    /// <summary>
    /// Used in: SearchPluginEndpoints (FillFilters, GetRatings, GetTypes, GetBoards)
    /// Returned in: PluginSearchResponse.Filters (GET /api/plugin/submitSearch)
    /// Purpose: API response model representing a filter option (price range, hotel types, board types, ratings)
    /// Created by: SearchPluginEndpoints methods that generate filters from search results
    /// </summary>
    public class Filter
    {

        public Filter(string name, string id, decimal? min, decimal? max, bool isMultipleAnd)
        {
            Name = name;
            Id = id;
            Type = FilterType.range;
            Min = min;
            Max = max;
            IsMultipleAND = isMultipleAnd;
        }

        public Filter(string name, string id, List<FilterValue> values, bool isMultipleAnd)
        {
            Name = name;
            Id = id;
            Type = FilterType.values;
            Values = values;
            IsMultipleAND = isMultipleAnd;
        }

        public override string ToString()
        {
            return Name;
        }

        public bool IsMultipleAND { get; set; }

        public string Name { get; set; }

        public string Id { get; set; }

        public FilterType Type { get; set; }

        public List<FilterValue> Values { get; set; }

        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}
