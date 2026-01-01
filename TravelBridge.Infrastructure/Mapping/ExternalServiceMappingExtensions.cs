using TravelBridge.Contracts.Responses;
using TravelBridge.Infrastructure.Integrations.ExternalServices;

namespace TravelBridge.Infrastructure.Mapping
{
    /// <summary>
    /// Extension methods to map external service models to Contracts DTOs.
    /// </summary>
    public static class ExternalServiceMappingExtensions
    {
        /// <summary>
        /// Maps LocationAutoCompleteResult to Contracts AutoCompleteLocation.
        /// </summary>
        public static AutoCompleteLocation ToContractsLocation(this LocationAutoCompleteResult location)
        {
            return new AutoCompleteLocation
            {
                Id = location.Id,
                Name = location.Name,
                Region = location.Region,
                CountryCode = location.CountryCode
            };
        }

        /// <summary>
        /// Maps a collection of LocationAutoCompleteResult to Contracts AutoCompleteLocation.
        /// </summary>
        public static IEnumerable<AutoCompleteLocation> ToContractsLocations(this IEnumerable<LocationAutoCompleteResult> locations)
        {
            return locations.Select(l => l.ToContractsLocation());
        }
    }
}
