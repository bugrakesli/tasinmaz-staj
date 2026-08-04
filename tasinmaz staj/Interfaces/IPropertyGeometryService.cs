using System.Threading.Tasks;

public interface IPropertyGeometryService
{
    Task<bool> UpdateGeometryAsync(
    int propertyId,
    UpdatePropertyGeometryDto dto,
    int userId,
    string role
    );
}
