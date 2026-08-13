using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILocationService
{
    Task<List<IlDto>> GetIllerAsync();
    Task<List<IlceDto>> GetIlcelerAsync(int? ilId);
}
