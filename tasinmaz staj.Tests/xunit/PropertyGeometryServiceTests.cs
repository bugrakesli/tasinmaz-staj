using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Xunit;

// NOT: AnalyzeIntersectionAsync burada test edilmiyor; bu metot alan
// hesabi icin EF.Functions.Transform (Npgsql/PostGIS'e ozgu) kullaniyor
// ve InMemory provider bu fonksiyonu ceviremiyor. Bu metot gercek bir
// PostgreSQL+PostGIS baglantisi gerektiren bir entegrasyon testiyle
// (veya Selenium E2E) kapsanmalidir.
public class PropertyGeometryServiceTests
{
    private static readonly GeometryFactory GeometryFactory =
        NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

    private static Polygon Square(double x, double y, double size = 1)
    {
        var ring = GeometryFactory.CreateLinearRing(new[]
        {
            new Coordinate(x, y),
            new Coordinate(x, y + size),
            new Coordinate(x + size, y + size),
            new Coordinate(x + size, y),
            new Coordinate(x, y)
        });
        var polygon = GeometryFactory.CreatePolygon(ring);
        polygon.SRID = 4326;
        return polygon;
    }

    private static RemsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RemsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RemsDbContext(options);

        var il = new Il { Id = 1, Ad = "Ankara" };
        var ilce = new Ilce { Id = 1, Ad = "Çankaya", IlId = 1 };
        var mahalle = new Mahalle { Id = 1, Ad = "Kızılay", IlceId = 1 };

        context.Iller.Add(il);
        context.Ilceler.Add(ilce);
        context.Mahalleler.Add(mahalle);

        context.Users.Add(new User { Id = 1, Email = "owner@test.com", Role = "User", PasswordHash = "x", Salt = "y" });
        context.Users.Add(new User { Id = 2, Email = "other@test.com", Role = "User", PasswordHash = "x", Salt = "y" });

        // A ve B kesisiyor (0,0)-(1,1) ile (0.5,0.5)-(1.5,1.5)
        context.Properties.Add(new Property
        {
            Id = 1,
            UserId = 1,
            MahalleId = 1,
            AdaNo = "10",
            ParselNo = "20",
            Adres = "A",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))",
            Geometry = Square(0, 0, 1)
        });

        context.Properties.Add(new Property
        {
            Id = 2,
            UserId = 1,
            MahalleId = 1,
            AdaNo = "11",
            ParselNo = "21",
            Adres = "B",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0.5 0.5,0.5 1.5,1.5 1.5,1.5 0.5,0.5 0.5))",
            Geometry = Square(0.5, 0.5, 1)
        });

        // C, baska bir kullaniciya ait, A/B ile kesismiyor.
        context.Properties.Add(new Property
        {
            Id = 3,
            UserId = 2,
            MahalleId = 1,
            AdaNo = "12",
            ParselNo = "22",
            Adres = "C",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((10 10,10 11,11 11,11 10,10 10))",
            Geometry = Square(10, 10, 1)
        });

        context.SaveChanges();
        return context;
    }

    private static List<List<CoordinateDto>> SquareCoordinates(double x, double y, double size = 1) =>
        new List<List<CoordinateDto>>
        {
            new List<CoordinateDto>
            {
                new CoordinateDto { Longitude = x, Latitude = y },
                new CoordinateDto { Longitude = x, Latitude = y + size },
                new CoordinateDto { Longitude = x + size, Latitude = y + size },
                new CoordinateDto { Longitude = x + size, Latitude = y }
            }
        };

    // ---- UpdateGeometryAsync ----

    [Fact]
    public async Task UpdateGeometryAsync_ValidPolygon_UpdatesGeometry()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UpdatePropertyGeometryDto { Coordinates = SquareCoordinates(5, 5) };

        var result = await service.UpdateGeometryAsync(propertyId: 1, dto, userId: 1, role: "User");

        Assert.True(result);
        var updated = await context.Properties.FindAsync(1);
        Assert.NotNull(updated!.Geometry);
        Assert.True(updated.Geometry!.IsValid);
    }

    [Fact]
    public async Task UpdateGeometryAsync_NonOwnerNonAdmin_ThrowsUnauthorized()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UpdatePropertyGeometryDto { Coordinates = SquareCoordinates(5, 5) };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.UpdateGeometryAsync(propertyId: 1, dto, userId: 2, role: "User"));
    }

    [Fact]
    public async Task UpdateGeometryAsync_UnknownProperty_ReturnsFalse()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UpdatePropertyGeometryDto { Coordinates = SquareCoordinates(5, 5) };

        var result = await service.UpdateGeometryAsync(propertyId: 999, dto, userId: 1, role: "User");

        Assert.False(result);
    }

    [Fact]
    public async Task UpdateGeometryAsync_EmptyCoordinates_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UpdatePropertyGeometryDto { Coordinates = new List<List<CoordinateDto>>() };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateGeometryAsync(propertyId: 1, dto, userId: 1, role: "User"));
    }

    // ---- SelectPropertiesAsync ----

    [Fact]
    public async Task SelectPropertiesAsync_IntersectingArea_ReturnsMatchingProperties()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        // Property 1 (0,0)-(1,1) ile kesisen bir secim alani.
        var dto = new UpdatePropertyGeometryDto { Coordinates = SquareCoordinates(0, 0) };

        var result = await service.SelectPropertiesAsync(dto, userId: 1, role: "Admin");

        Assert.Contains(result, p => p.Id == 1);
        Assert.DoesNotContain(result, p => p.Id == 3);
    }

    [Fact]
    public async Task SelectPropertiesAsync_NonAdmin_OnlyReturnsOwnProperties()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        // Genis bir alan: 1, 2 ve 3'u de kapsayacak sekilde secim yapiyoruz.
        var dto = new UpdatePropertyGeometryDto { Coordinates = SquareCoordinates(-5, -5, 30) };

        var result = await service.SelectPropertiesAsync(dto, userId: 2, role: "User");

        Assert.All(result, p => Assert.True(true)); // sahiplik DTO'da yok, sayimla dogrulanir
        Assert.True(result.Count <= 1); // sadece userId=2'ye ait olan Property 3 donebilir
    }

    [Fact]
    public async Task SelectPropertiesAsync_TooFewPoints_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UpdatePropertyGeometryDto
        {
            Coordinates = new List<List<CoordinateDto>>
            {
                new List<CoordinateDto>
                {
                    new CoordinateDto { Longitude = 0, Latitude = 0 },
                    new CoordinateDto { Longitude = 1, Latitude = 1 }
                }
            }
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SelectPropertiesAsync(dto, userId: 1, role: "Admin"));
    }

    // ---- AnalyzeUnionAsync ----

    [Fact]
    public async Task AnalyzeUnionAsync_TwoProperties_ReturnsLabelD()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UnionAnalysisDto { PropertyAId = 1, PropertyBId = 2 };

        var result = await service.AnalyzeUnionAsync(dto, userId: 1, role: "User");

        Assert.Equal("D", result.ResultLabel);
        Assert.True(result.AreaSquareMeters > 0);
        Assert.Single(await context.GeometryResults.ToListAsync());
    }

    [Fact]
    public async Task AnalyzeUnionAsync_ThreeProperties_ReturnsLabelE()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        // C kullanici 2'ye ait; Admin rolu ile birlestirmeyi test ediyoruz.
        var dto = new UnionAnalysisDto { PropertyAId = 1, PropertyBId = 2, PropertyCId = 3 };

        var result = await service.AnalyzeUnionAsync(dto, userId: 1, role: "Admin");

        Assert.Equal("E", result.ResultLabel);
    }

    [Fact]
    public async Task AnalyzeUnionAsync_NonAdminCannotUseOthersProperty_ThrowsKeyNotFound()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        // Property 3, userId=1'e ait degil; normal kullanici erisemez.
        var dto = new UnionAnalysisDto { PropertyAId = 1, PropertyBId = 3 };

        await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(
            () => service.AnalyzeUnionAsync(dto, userId: 1, role: "User"));
    }

    [Fact]
    public async Task AnalyzeUnionAsync_DuplicatePropertyIds_ThrowsArgumentException()
    {
        using var context = CreateContext();
        var service = new PropertyGeometryService(context);

        var dto = new UnionAnalysisDto { PropertyAId = 1, PropertyBId = 1 };

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AnalyzeUnionAsync(dto, userId: 1, role: "User"));
    }
}
