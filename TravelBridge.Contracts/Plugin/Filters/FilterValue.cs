namespace TravelBridge.Contracts.Plugin.Filters
{
    /// <summary>
    /// Used in: Filter.Values property
    /// Created by: SearchPluginEndpoints methods (GetRatings, GetTypes, GetBoards)
    /// Returned in: Filter object within PluginSearchResponse (GET /api/plugin/submitSearch)
    /// Purpose: API response model representing individual filter option values with counts
    /// </summary>
    public class FilterValue
    {
        public string Id { get; set; }

        public int Count { get; set; }

        public bool Selected { get; set; }

        public int FilteredCount { get; set; }

        public string Name { get; set; }
    }
}
