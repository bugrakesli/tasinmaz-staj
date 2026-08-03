using System.Threading.Tasks;

public interface IGeometryService
{
    Task<bool> SaveUnionResultAsync(SaveGeometryDto dto, int userId);
}