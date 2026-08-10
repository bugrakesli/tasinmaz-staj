using System;
using NetTopologySuite.Geometries;

/// <summary>
/// SRS 3.2.10: Alan hesaplarını EPSG:3857 (Web Mercator) yerine doğrudan
/// WGS84 (EPSG:4326) lon/lat koordinatları üzerinden, küre yüzeyinde
/// geodesic olarak hesaplar. EPSG:3857 Türkiye enlemlerinde (~36-42°)
/// alanı yaklaşık 1.6-1.8x şişiriyordu.
///
/// Algoritma: Chamberlain & Duquette, "Some Algorithms for Polygons on a
/// Sphere", JPL Publication 07-03, 2007 (spherical excess yöntemi).
/// </summary>
public static class GeodesicAreaCalculator
{
    // Dünya'nın authalic (eşit alan) ortalama yarıçapı (metre).
    private const double EarthRadiusMeters = 6371007.1810;

    public static double ComputeAreaSquareMeters(Geometry geometry)
    {
        if (geometry == null || geometry.IsEmpty)
            return 0;

        switch (geometry)
        {
            case Polygon polygon:
                return ComputePolygonArea(polygon);

            case MultiPolygon multiPolygon:
                {
                    double total = 0;
                    foreach (Polygon p in multiPolygon.Geometries)
                        total += ComputePolygonArea(p);
                    return total;
                }

            case GeometryCollection collection:
                {
                    double total = 0;
                    foreach (var g in collection.Geometries)
                        total += ComputeAreaSquareMeters(g);
                    return total;
                }

            default:
                // Nokta/çizgi gibi alanı olmayan geometriler için 0 döner.
                return 0;
        }
    }

    private static double ComputePolygonArea(Polygon polygon)
    {
        double area = ComputeRingArea(polygon.ExteriorRing.Coordinates);

        foreach (var hole in polygon.InteriorRings)
            area -= ComputeRingArea(hole.Coordinates);

        return Math.Abs(area);
    }

    // Chamberlain & Duquette spherical excess formülü:
    // sum((lon2 - lon1) * (2 + sin(lat1) + sin(lat2))) * R^2 / 2
    private static double ComputeRingArea(Coordinate[] coordinates)
    {
        if (coordinates == null || coordinates.Length < 4)
            return 0;

        double sum = 0;

        for (int i = 0; i < coordinates.Length - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];

            double lon1 = ToRadians(p1.X);
            double lat1 = ToRadians(p1.Y);
            double lon2 = ToRadians(p2.X);
            double lat2 = ToRadians(p2.Y);

            sum += (lon2 - lon1) * (2 + Math.Sin(lat1) + Math.Sin(lat2));
        }

        return sum * EarthRadiusMeters * EarthRadiusMeters / 2.0;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
