using System;
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
}
