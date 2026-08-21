using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

public class PropertyGeometryService : IPropertyGeometryService
{
    private readonly RemsDbContext _context;

public PropertyGeometryService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> UpdateGeometryAsync(
        int propertyId,
        UpdatePropertyGeometryDto dto,
        int userId,
        string role)
    {
        if (dto == null ||
            dto.Coordinates == null ||
            dto.Coordinates.Count == 0)
        {
            throw new ArgumentException(
                "Polygon coordinates are required."
            );
        }

        var property = await _context.Properties
            .FirstOrDefaultAsync(x => x.Id == propertyId);

        if (property == null)
        {
            return false;
        }

        // Normal kullanıcı yalnızca kendi mülkünü güncelleyebilir.
        if (role != "Admin" && property.UserId != userId)
        {
            throw new UnauthorizedAccessException(
                "You do not have permission to update this property."
            );
        }

        var geometryFactory =
            NtsGeometryServices.Instance
                .CreateGeometryFactory(srid: 4326);

        var shellCoordinates = dto.Coordinates[0]
            .Select(x => new Coordinate(
                x.Longitude,
                x.Latitude
            ))
            .ToList();

        // Polygon halkası kapalı değilse ilk noktayı sona ekle.
        if (!shellCoordinates.First()
            .Equals2D(shellCoordinates.Last()))
        {
            shellCoordinates.Add(
                new Coordinate(
                    shellCoordinates.First().X,
                    shellCoordinates.First().Y
                )
            );
        }

        if (shellCoordinates.Count < 4)
        {
            throw new ArgumentException(
                "A polygon must contain at least three points."
            );
        }

        var linearRing = geometryFactory
            .CreateLinearRing(
                shellCoordinates.ToArray()
            );

        var polygon = geometryFactory
            .CreatePolygon(linearRing);

        if (!polygon.IsValid)
        {
            throw new ArgumentException(
                "The polygon geometry is invalid."
            );
        }

        property.Geometry = polygon;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<List<PropertyDto>> SelectPropertiesAsync(
    UpdatePropertyGeometryDto dto,
    int userId,
    string role)
    {
        if (dto == null ||
            dto.Coordinates == null ||
            dto.Coordinates.Count == 0 ||
            dto.Coordinates[0] == null ||
            dto.Coordinates[0].Count < 3)
        {
            throw new ArgumentException(
                "A polygon must contain at least three points."
            );
        }

        var geometryFactory =
            NtsGeometryServices.Instance
                .CreateGeometryFactory(srid: 4326);

        var shellCoordinates = dto.Coordinates[0]
            .Select(x => new Coordinate(
                x.Longitude,
                x.Latitude
            ))
            .ToList();

        // İlk ve son nokta aynı değilse poligonu kapat.
        if (!shellCoordinates.First()
            .Equals2D(shellCoordinates.Last()))
        {
            shellCoordinates.Add(
                new Coordinate(
                    shellCoordinates.First().X,
                    shellCoordinates.First().Y
                )
            );
        }

        var linearRing = geometryFactory.CreateLinearRing(
            shellCoordinates.ToArray()
        );

        var selectionPolygon = geometryFactory.CreatePolygon(
            linearRing
        );

        if (!selectionPolygon.IsValid)
        {
            throw new ArgumentException(
                "The polygon geometry is invalid."
            );
        }

        var query = _context.Properties
            .AsNoTracking()
            .Where(x =>
                x.Geometry != null &&
                x.Geometry.Intersects(selectionPolygon)
            );

        // Admin tüm mülkleri görebilir.
        // Normal kullanıcı yalnızca kendi mülklerini görür.
        if (role != "Admin")
        {
            query = query.Where(
                x => x.UserId == userId
            );
        }

        var properties = await query
            .Include(x => x.Mahalle)
                .ThenInclude(x => x.Ilce)
                    .ThenInclude(x => x.Il)
            .Select(x => new PropertyDto
            {
                Id = x.Id,
                City = x.Mahalle.Ilce.Il.Ad,
                District = x.Mahalle.Ilce.Ad,
                Neighborhood = x.Mahalle.Ad,
                AdaNo = x.AdaNo,
                ParselNo = x.ParselNo,
                Adres = x.Adres,
                PropertyType = x.PropertyType,
                Coordinate = x.Geometry != null ? x.Geometry.AsText() : null
            })
            .ToListAsync();

        return properties;
    }

    public async Task<IntersectionResultDto> AnalyzeIntersectionAsync(
    IntersectionAnalysisDto dto,
    int userId,
    string role)
    {
        if (dto == null ||
            dto.Coordinates == null ||
            dto.Coordinates.Count == 0 ||
            dto.Coordinates[0] == null ||
            dto.Coordinates[0].Count < 3)
        {
            throw new ArgumentException(
                "A polygon must contain at least three points."
            );
        }

        var propertyQuery = _context.Properties
            .AsNoTracking()
            .Where(x =>
                x.Id == dto.PropertyId &&
                x.Geometry != null
            );

        // Normal kullanıcı yalnızca kendi mülkünü analiz edebilir.
        if (role != "Admin")
        {
            propertyQuery = propertyQuery.Where(
                x => x.UserId == userId
            );
        }

        var property = await propertyQuery
            .FirstOrDefaultAsync();

        if (property == null)
        {
            throw new KeyNotFoundException(
                "Property not found or access denied."
            );
        }

        var geometryFactory =
            NtsGeometryServices.Instance
                .CreateGeometryFactory(srid: 4326);

        var coordinates = dto.Coordinates[0]
            .Select(x => new Coordinate(
                x.Longitude,
                x.Latitude
            ))
            .ToList();

        // Poligonu otomatik kapat.
        if (!coordinates.First()
            .Equals2D(coordinates.Last()))
        {
            coordinates.Add(
                new Coordinate(
                    coordinates.First().X,
                    coordinates.First().Y
                )
            );
        }

        var ring = geometryFactory.CreateLinearRing(
            coordinates.ToArray()
        );

        var selectionPolygon =
            geometryFactory.CreatePolygon(ring);

        if (!selectionPolygon.IsValid)
        {
            throw new ArgumentException(
                "The polygon geometry is invalid."
            );
        }

        bool intersects =
            property.Geometry.Intersects(
                selectionPolygon
            );

        if (!intersects)
        {
            return new IntersectionResultDto
            {
                PropertyId = property.Id,
                Intersects = false,
                PropertyAreaSquareMeters = 0,
                IntersectionAreaSquareMeters = 0,
                IntersectionPercentage = 0,
                IntersectionGeometry = null
            };
        }

        var intersection =
            property.Geometry.Intersection(
                selectionPolygon
            );

        // SRS 3.2.10: Alan hesabı EPSG:3857 (Web Mercator) yerine, WGS84
        // (EPSG:4326) lon/lat koordinatları üzerinden geodesic olarak
        // hesaplanır. EPSG:3857, Türkiye enlemlerinde alanı ~1.6-1.8x
        // şişiriyordu (bkz. GeodesicAreaCalculator).
        double propertyAreaSquareMeters =
            GeodesicAreaCalculator.ComputeAreaSquareMeters(
                property.Geometry
            );

        double intersectionAreaSquareMeters =
            GeodesicAreaCalculator.ComputeAreaSquareMeters(
                intersection
            );

        double percentage =
            propertyAreaSquareMeters > 0
                ? (intersectionAreaSquareMeters /
                   propertyAreaSquareMeters) * 100
                : 0;

        return new IntersectionResultDto
        {
            PropertyId = property.Id,
            Intersects = true,
            PropertyAreaSquareMeters =
                Math.Round(
                    propertyAreaSquareMeters,
                    2
                ),
            IntersectionAreaSquareMeters =
                Math.Round(
                    intersectionAreaSquareMeters,
                    2
                ),
            IntersectionPercentage =
                Math.Round(
                    percentage,
                    2
                ),
            IntersectionGeometry =
                intersection.AsText()
        };
    }

    public async Task<UnionResultDto> AnalyzeUnionAsync(
    UnionAnalysisDto dto,
    int userId,
    string role)
    {
        if (dto == null)
        {
            throw new ArgumentException(
                "Union information is required."
            );
        }

        if (dto.PropertyAId <= 0 ||
            dto.PropertyBId <= 0)
        {
            throw new ArgumentException(
                "Properties A and B are required."
            );
        }

        var propertyIds = new List<int>
    {
        dto.PropertyAId,
        dto.PropertyBId
    };

        if (dto.PropertyCId.HasValue)
        {
            if (dto.PropertyCId.Value <= 0)
            {
                throw new ArgumentException(
                    "Property C is invalid."
                );
            }

            propertyIds.Add(
                dto.PropertyCId.Value
            );
        }

        // Aynı property'nin birden fazla seçilmesini engelle.
        if (propertyIds.Distinct().Count()
            != propertyIds.Count)
        {
            throw new ArgumentException(
                "A, B and C must be different properties."
            );
        }

        var propertyQuery = _context.Properties
            .Where(x =>
                propertyIds.Contains(x.Id) &&
                x.Geometry != null
            );

        // Normal kullanıcı yalnızca kendi mülkleriyle
        // union işlemi yapabilir.
        if (role != "Admin")
        {
            propertyQuery = propertyQuery.Where(
                x => x.UserId == userId
            );
        }

        var properties = await propertyQuery
            .ToListAsync();

        if (properties.Count != propertyIds.Count)
        {
            throw new KeyNotFoundException(
                "One or more properties were not found or access was denied."
            );
        }

        var propertyA = properties
            .First(x => x.Id == dto.PropertyAId);

        var propertyB = properties
            .First(x => x.Id == dto.PropertyBId);

        // A ∪ B işlemi
        Geometry unionGeometry =
            propertyA.Geometry.Union(
                propertyB.Geometry
            );

        string resultLabel = "D";

        // C gönderilmişse:
        // (A ∪ B) ∪ C = A ∪ B ∪ C
        if (dto.PropertyCId.HasValue)
        {
            var propertyC = properties
                .First(x =>
                    x.Id == dto.PropertyCId.Value
                );

            unionGeometry =
                unionGeometry.Union(
                    propertyC.Geometry
                );

            resultLabel = "E";
        }

        if (unionGeometry == null ||
            unionGeometry.IsEmpty)
        {
            throw new InvalidOperationException(
                "Union geometry could not be created."
            );
        }

        // SRS 3.2.10: Alan hesabı EPSG:4326 lon/lat koordinatları üzerinden
        // geodesic olarak yapılır (bkz. GeodesicAreaCalculator).
        // Not: Geometrinin SRID'ini yalnızca 3857 olarak işaretlemek
        // koordinatları gerçekten dönüştürmüyordu; eski hesap da hatalıydı.
        double surfaceArea = Math.Round(
            GeodesicAreaCalculator.ComputeAreaSquareMeters(unionGeometry),
            2
        );

        var geometryResult = new GeometryResult
        {
            Label = resultLabel,
            Wkt = unionGeometry.AsText(),
            SurfaceArea = surfaceArea,
            CreatedAt = DateTime.UtcNow
        };

        _context.GeometryResults.Add(
            geometryResult
        );

        await _context.SaveChangesAsync();

        return new UnionResultDto
        {
            ResultLabel = resultLabel,
            AreaSquareMeters = surfaceArea,
            Geometry = unionGeometry.AsText()
        };
    }

}