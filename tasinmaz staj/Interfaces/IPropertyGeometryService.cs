using System.Collections.Generic;
using System.Threading.Tasks;

public interface IPropertyGeometryService
{
    Task<bool> UpdateGeometryAsync(
        int propertyId,
        UpdatePropertyGeometryDto dto,
        int userId,
        string role
    );

    Task<List<PropertyDto>> SelectPropertiesAsync(
        UpdatePropertyGeometryDto dto,
        int userId,
        string role
    );

    Task<IntersectionResultDto> AnalyzeIntersectionAsync(
        IntersectionAnalysisDto dto,
        int userId,
        string role
    );
}