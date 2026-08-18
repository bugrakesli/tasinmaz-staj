using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LocationService : ILocationService
{
    private readonly RemsDbContext _context;

    public LocationService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<List<IlDto>> GetIllerAsync()
    {
        return await _context.Iller
            .OrderBy(i => i.Ad)
            .Select(i => new IlDto { Id = i.Id, Ad = i.Ad })
            .ToListAsync();
    }

    public async Task<List<IlceDto>> GetIlcelerAsync(int? ilId)
    {
        var query = _context.Ilceler.AsQueryable();

        if (ilId.HasValue)
            query = query.Where(x => x.IlId == ilId.Value);

        return await query
            .OrderBy(x => x.Ad)
            .Select(x => new IlceDto { Id = x.Id, IlId = x.IlId, Ad = x.Ad })
            .ToListAsync();
    }

    public async Task<List<MahalleDto>> GetMahallelerAsync(int ilceId)
    {
        return await _context.Mahalleler
            .Where(x => x.IlceId == ilceId)
            .OrderBy(x => x.Ad)
            .Select(x => new MahalleDto { Id = x.Id, IlceId = x.IlceId, Ad = x.Ad })
            .ToListAsync();
    }
}
