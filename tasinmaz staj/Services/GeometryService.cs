using System;
using System.Threading.Tasks;

public class GeometryService : IGeometryService
{
    private readonly RemsDbContext _context;

    public GeometryService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<bool> SaveUnionResultAsync(SaveGeometryDto dto, int userId)
    {
        var geomResult = new GeometryResult
        {
            Label = dto.ResultType, // "D" veya "E"
            Wkt = dto.ResultWkt,
            SurfaceArea = dto.CalculatedArea,
            CreatedAt = DateTime.UtcNow // Loglama veya takip için oluþturulma zamaný
        };

        await _context.GeometryResults.AddAsync(geomResult);
        var result = await _context.SaveChangesAsync();

        return result > 0;
    }
}