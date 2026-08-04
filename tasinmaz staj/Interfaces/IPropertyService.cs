using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPropertyService
{
    Task<List<PropertyDto>> GetAllAsync(int userId, string role);
    Task<PropertyDto> CreateAsync(CreatePropertyDto dto, int userId);
    Task<PropertyDto> UpdateAsync(int propertyId, CreatePropertyDto dto, int userId);
    Task<bool> DeleteAsync(int propertyId, int userId);
    Task<object> GetFilteredAsync(PropertyFilterDto filter, int userId, string role);
}