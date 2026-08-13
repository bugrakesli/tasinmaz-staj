using System;
using NetTopologySuite.Geometries;
using Xunit;

public class GeodesicAreaCalculatorTests
{
    private readonly GeometryFactory _geometryFactory;

    public GeodesicAreaCalculatorTests()
    {
        // EPSG:4326 for WGS84
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    [Fact]
    public void CalculateArea_KnownSquareNearEquator_ReturnsApproximateArea()
    {
        // Arrange
        // 0.01 degree at the equator is roughly 1113.2 meters
        // A 0.01 x 0.01 degree square is roughly 1,113.2 * 1,113.2 = 1,239,214.24 square meters
        var coordinates = new[]
        {
            new Coordinate(0, 0),
            new Coordinate(0.01, 0),
            new Coordinate(0.01, 0.01),
            new Coordinate(0, 0.01),
            new Coordinate(0, 0)
        };
        var polygon = _geometryFactory.CreatePolygon(coordinates);

        // Act
        double area = GeodesicAreaCalculator.ComputeAreaSquareMeters(polygon);

        // Assert
        // Allow a 1% margin of error
        double expectedArea = 1239214.24;
        Assert.InRange(area, expectedArea * 0.99, expectedArea * 1.01);
    }

    [Fact]
    public void CalculateArea_TurkeyLatitude_ReturnsReasonableArea()
    {
        // Arrange
        // 1 degree of longitude at 40 degrees North is ~85 km, 1 degree of latitude is ~111 km.
        // Let's create a 0.01 x 0.01 degree square at 40 N latitude (near Turkey).
        // Area should be approx 850m * 1110m = 943,500 sq meters.
        var coordinates = new[]
        {
            new Coordinate(32.0, 40.0),
            new Coordinate(32.01, 40.0),
            new Coordinate(32.01, 40.01),
            new Coordinate(32.0, 40.01),
            new Coordinate(32.0, 40.0)
        };
        var polygon = _geometryFactory.CreatePolygon(coordinates);

        // Act
        double area = GeodesicAreaCalculator.ComputeAreaSquareMeters(polygon);

        // Assert
        // Assert it's reasonably smaller than the equator area for the same degree dimensions
        Assert.True(area > 0);
        Assert.True(area < 1200000); // Definitely less than equator area
        Assert.True(area > 900000); // Should be roughly 943k
    }

    [Fact]
    public void CalculateArea_DegeneratePolygon_ReturnsZero()
    {
        // Arrange
        // A line represented as a geometry collection or point
        var lineString = _geometryFactory.CreateLineString(new[]
        {
            new Coordinate(0, 0),
            new Coordinate(1, 1)
        });

        // Act
        double lineArea = GeodesicAreaCalculator.ComputeAreaSquareMeters(lineString);

        // Assert
        Assert.Equal(0, lineArea);
    }
}
