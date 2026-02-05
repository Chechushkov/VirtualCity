using System;

namespace Excursion_GPT.Application.Common
{
    public static class CoordinateConverter
    {
        private const double EarthRadius = 6378137.0; // meters
        private const double OriginShift = Math.PI * EarthRadius; // 20037508.342789244

        /// <summary>
        /// Convert Web Mercator (EPSG:3857) coordinates to WGS84 (EPSG:4326) latitude/longitude
        /// </summary>
        /// <param name="x">Web Mercator X coordinate in meters</param>
        /// <param name="z">Web Mercator Z coordinate in meters</param>
        /// <returns>Tuple with latitude and longitude in degrees</returns>
        public static (double Latitude, double Longitude) WebMercatorToLatLon(double x, double z)
        {
            // For Ekaterinburg data, x coordinates are negative but should be positive
            // So we invert the sign of x
            double correctedX = -x;

            // Normalize coordinates
            double lon = (correctedX / OriginShift) * 180.0;
            double lat = (z / OriginShift) * 180.0;

            // Convert to latitude
            lat = 180.0 / Math.PI * (2.0 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);

            return (lat, lon);
        }

        /// <summary>
        /// Convert WGS84 (EPSG:4326) latitude/longitude to Web Mercator (EPSG:3857) coordinates
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <returns>Tuple with Web Mercator X and Z coordinates in meters</returns>
        public static (double X, double Z) LatLonToWebMercator(double latitude, double longitude)
        {
            double x = longitude * OriginShift / 180.0;
            double y = Math.Log(Math.Tan((90.0 + latitude) * Math.PI / 360.0)) / (Math.PI / 180.0);
            y = y * OriginShift / 180.0;

            // For Ekaterinburg data, we need to invert x sign
            double correctedX = -x;

            return (correctedX, y);
        }

        /// <summary>
        /// Check if Web Mercator coordinates are within valid Earth bounds
        /// </summary>
        /// <param name="x">Web Mercator X coordinate</param>
        /// <param name="z">Web Mercator Z coordinate</param>
        /// <returns>True if coordinates are valid</returns>
        public static bool IsValidWebMercator(double x, double z)
        {
            const double maxWebMercator = 20037508.34;
            return Math.Abs(x) <= maxWebMercator && Math.Abs(z) <= maxWebMercator;
        }

        /// <summary>
        /// Check if geographic coordinates are within valid Earth bounds
        /// </summary>
        /// <param name="latitude">Latitude in degrees</param>
        /// <param name="longitude">Longitude in degrees</param>
        /// <returns>True if coordinates are valid</returns>
        public static bool IsValidLatLon(double latitude, double longitude)
        {
            return Math.Abs(latitude) <= 90.0 && Math.Abs(longitude) <= 180.0;
        }

        /// <summary>
        /// Calculate distance between two points in Web Mercator coordinates (in meters)
        /// </summary>
        /// <param name="x1">First point X coordinate</param>
        /// <param name="z1">First point Z coordinate</param>
        /// <param name="x2">Second point X coordinate</param>
        /// <param name="z2">Second point Z coordinate</param>
        /// <returns>Distance in meters</returns>
        public static double CalculateDistance(double x1, double z1, double x2, double z2)
        {
            double dx = x2 - x1;
            double dz = z2 - z1;
            return Math.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Convert distance from meters to degrees at a given latitude (approximate)
        /// </summary>
        /// <param name="distanceMeters">Distance in meters</param>
        /// <param name="latitude">Latitude in degrees for scale factor</param>
        /// <returns>Approximate distance in degrees</returns>
        public static double MetersToDegrees(double distanceMeters, double latitude)
        {
            // Earth's circumference at equator: ~40,075,000 meters
            // 1 degree at equator: ~111,319 meters
            // Scale factor for latitude
            double scaleFactor = Math.Cos(latitude * Math.PI / 180.0);
            double metersPerDegree = 111319.0 * scaleFactor;

            return distanceMeters / metersPerDegree;
        }

        /// <summary>
        /// Convert distance from degrees to meters at a given latitude (approximate)
        /// </summary>
        /// <param name="distanceDegrees">Distance in degrees</param>
        /// <param name="latitude">Latitude in degrees for scale factor</param>
        /// <returns>Approximate distance in meters</returns>
        public static double DegreesToMeters(double distanceDegrees, double latitude)
        {
            double scaleFactor = Math.Cos(latitude * Math.PI / 180.0);
            double metersPerDegree = 111319.0 * scaleFactor;

            return distanceDegrees * metersPerDegree;
        }
    }
}
