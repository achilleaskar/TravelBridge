namespace TravelBridge.API.Helpers
{
    /// <summary>
    /// Helper class for parsing composite IDs used throughout the application.
    /// Provides robust parsing that handles IDs containing the separator character.
    /// </summary>
    public static class CompositeIdHelper
    {
        /// <summary>
        /// Parses a hotel ID in the format "{providerId}-{hotelId}".
        /// Uses only the first dash as separator, allowing hotel IDs to contain dashes.
        /// </summary>
        /// <param name="compositeHotelId">The composite hotel ID string</param>
        /// <returns>A tuple containing (providerId, hotelId)</returns>
        /// <exception cref="ArgumentException">Thrown when the ID format is invalid</exception>
        public static (string ProviderId, string HotelId) ParseHotelId(string compositeHotelId)
        {
            if (string.IsNullOrWhiteSpace(compositeHotelId))
            {
                throw new ArgumentException("Hotel ID cannot be null or empty.", nameof(compositeHotelId));
            }

            int firstDashIndex = compositeHotelId.IndexOf('-');
            if (firstDashIndex <= 0 || firstDashIndex >= compositeHotelId.Length - 1)
            {
                throw new ArgumentException(
                    "Invalid hotel ID format. Expected format: {providerId}-{hotelId}",
                    nameof(compositeHotelId));
            }

            string providerId = compositeHotelId.Substring(0, firstDashIndex);
            string hotelId = compositeHotelId.Substring(firstDashIndex + 1);

            return (providerId, hotelId);
        }

        /// <summary>
        /// Parses a location bounding box ID in the format "{bbox}-{latitude}-{longitude}".
        /// Uses the first two dashes as separators, allowing coordinates to contain dashes.
        /// </summary>
        /// <param name="compositeBBoxId">The composite bounding box ID string</param>
        /// <returns>A tuple containing (bbox, latitude, longitude)</returns>
        /// <exception cref="ArgumentException">Thrown when the ID format is invalid</exception>
        public static (string BBox, string Latitude, string Longitude) ParseBBoxId(string compositeBBoxId)
        {
            if (string.IsNullOrWhiteSpace(compositeBBoxId))
            {
                throw new ArgumentException("BBox ID cannot be null or empty.", nameof(compositeBBoxId));
            }

            int firstDashIndex = compositeBBoxId.IndexOf('-');
            if (firstDashIndex <= 0 || firstDashIndex >= compositeBBoxId.Length - 1)
            {
                throw new ArgumentException(
                    "Invalid bbox format. Expected format: {bbox}-{latitude}-{longitude}",
                    nameof(compositeBBoxId));
            }

            string bbox = compositeBBoxId.Substring(0, firstDashIndex);
            string remainder = compositeBBoxId.Substring(firstDashIndex + 1);

            int secondDashIndex = remainder.IndexOf('-');
            if (secondDashIndex <= 0 || secondDashIndex >= remainder.Length - 1)
            {
                throw new ArgumentException(
                    "Invalid bbox format. Expected format: {bbox}-{latitude}-{longitude}",
                    nameof(compositeBBoxId));
            }

            string latitude = remainder.Substring(0, secondDashIndex);
            string longitude = remainder.Substring(secondDashIndex + 1);

            return (bbox, latitude, longitude);
        }
    }
}
