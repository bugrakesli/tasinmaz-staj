using System.Collections.Generic;
using System.Threading.Tasks;
namespace TasinmazStaj.Interfaces
{
    public interface IGeometryService
    {
        // Keeps the existing methods for A, B, C...
        Task<List<GeometryResult>> GetAutoSelectGeometriesAsync();
        Task<GeometryResult> ComputeIntersectionAsync(string label1, string label2);

        // NEW: Method specifically for saving the frontend's union result
        Task<GeometryResult> SaveUnionResultAsync(SaveGeometryDto dto, int v);
        Task<GeometryResult> SaveUnionResultAsync(SaveGeometryDto request);
    }
}