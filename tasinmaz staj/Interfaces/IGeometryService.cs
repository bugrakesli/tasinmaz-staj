using System.Collections.Generic;
using System.Threading.Tasks;

namespace TasinmazStaj.Interfaces
{
    public interface IGeometryService
    {
        Task<GeometryResult> SaveManualGeometryAsync(SaveManualGeometryDto dto, int userId);
        Task<List<GeometryResult>> GetAutoSelectGeometriesAsync(int userId);
        Task<GeometryOperationResultDto> ComputeIntersectionAsync(int userId);
        Task<GeometryOperationResultDto> ComputeUnionAsync(int userId, bool includeC);
        Task ClearAsync(int userId);
    }
}