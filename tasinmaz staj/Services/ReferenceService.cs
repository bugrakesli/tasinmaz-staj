using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class ReferenceService : IReferenceService
{
    private readonly RemsDbContext _context;

    public ReferenceService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Il>> GetIllerAsync()
    {
        return await _context.Iller
            .AsNoTracking()
            .OrderBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<IEnumerable<Ilce>> GetIlcelerAsync(int ilId)
    {
        return await _context.Ilceler
            .AsNoTracking()
            .Where(x => x.IlId == ilId)
            .OrderBy(x => x.Ad)
            .ToListAsync();
    }

    public async Task<IEnumerable<Mahalle>> GetMahallelerAsync(int ilceId)
    {
        return await _context.Mahalleler
            .AsNoTracking()
            .Where(x => x.IlceId == ilceId)
            .OrderBy(x => x.Ad)
            .ToListAsync();
    }
}
