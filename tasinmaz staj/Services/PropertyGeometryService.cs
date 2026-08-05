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
                Coordinate = x.Coordinate
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

        // EPSG:4326 derece birimindedir.
        // Alan hesabı için geometriyi metre tabanlı
        // EPSG:3857 koordinat sistemine dönüştürüyoruz.
        var propertyGeometry3857 =
            property.Geometry.Copy();

        var selectionGeometry3857 =
            selectionPolygon.Copy();

        // PostGIS üzerinden metre cinsinden alan hesabı
        // yapacağımız için aşağıdaki alanlar SQL tarafında
        // hesaplanacak.
        var areaResult = await _context.Properties
            .Where(x => x.Id == property.Id)
            .Select(x => new
            {
                PropertyArea =
                    EF.Functions
                        .Transform(
                            x.Geometry,
                            3857
                        )
                        .Area,

                IntersectionArea =
                    EF.Functions
                        .Transform(
                            x.Geometry
                                .Intersection(
                                    selectionPolygon
                                ),
                            3857
                        )
                        .Area
            })
            .FirstAsync();

        double percentage =
            areaResult.PropertyArea > 0
                ? (areaResult.IntersectionArea /
                   areaResult.PropertyArea) * 100
                : 0;

        return new IntersectionResultDto
        {
            PropertyId = property.Id,
            Intersects = true,
            PropertyAreaSquareMeters =
                Math.Round(
                    areaResult.PropertyArea,
                    2
                ),
            IntersectionAreaSquareMeters =
                Math.Round(
                    areaResult.IntersectionArea,
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

        // Union geometrisi EPSG:4326 koordinat sisteminde.
        // Alan hesabı için EPSG:3857'e dönüştürülür.
        var unionGeometry3857 =
            unionGeometry.Copy();

        unionGeometry3857.SRID = 3857;

        // Metre cinsinden alan
        double surfaceArea = Math.Round(
            unionGeometry3857.Area,
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