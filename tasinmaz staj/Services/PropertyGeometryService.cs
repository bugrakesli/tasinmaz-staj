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

        // Normal kullanýcý yalnýzca kendi mülkünü güncelleyebilir.
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

        // Polygon halkasý kapalý deðilse ilk noktayý sona ekle.
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

        // Ýlk ve son nokta ayný deðilse poligonu kapat.
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
        // Normal kullanýcý yalnýzca kendi mülklerini görür.
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

        // Normal kullanýcý yalnýzca kendi mülkünü analiz edebilir.
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
        // Alan hesabý için geometriyi metre tabanlý
        // EPSG:3857 koordinat sistemine dönüþtürüyoruz.
        var propertyGeometry3857 =
            property.Geometry.Copy();

        var selectionGeometry3857 =
            selectionPolygon.Copy();

        // PostGIS üzerinden metre cinsinden alan hesabý
        // yapacaðýmýz için aþaðýdaki alanlar SQL tarafýnda
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
}