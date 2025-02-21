
namespace TravelBridge.API.Models.Plugin.Filters
{
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

        public bool IsMultipleAND { get; set; }

        public string Name { get; set; }

        public string Id { get; set; }

        public FilterType Type { get; set; }

        public List<FilterValue> Values { get; set; }

        public decimal? Min { get; set; }
        public decimal? Max { get; set; }
    }
}
