using System.Collections.Generic;
using System.Threading.Tasks;

public interface IReferenceService
{
    Task<IEnumerable<Il>> GetIllerAsync();
    Task<IEnumerable<Ilce>> GetIlcelerAsync(int ilId);
    Task<IEnumerable<Mahalle>> GetMahallelerAsync(int ilceId);
}
