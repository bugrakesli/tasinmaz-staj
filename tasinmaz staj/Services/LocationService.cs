using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

public class LocationService : ILocationService
{
    private readonly RemsDbContext _context;

    // PostgreSQL varsayilan olarak "C" collation kullandigi icin ORDER BY
    // veritabani tarafinda calistirilirsa Turkce'ye ozgu buyuk harfler
    // (İ, Ç, Ğ, Ö, Ş, Ü) alfabetik sirada degil, UTF-8 kod noktasina gore
    // (Z'den sonra) siralanir ve listelerin sonuna duser. Bu yuzden veriyi
    // once cekip Turkce kultur karsilastiricisiyla bellekte sıralıyoruz.
    private static readonly StringComparer TurkishComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), ignoreCase: false);

    public LocationService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<List<IlDto>> GetIllerAsync()
    {
        // Id sütunu plaka numarasıyla örtüşmediği için ayrı bir PlakaKodu
        // kolonu eklendi (bkz. AddIlPlakaKodu migration). Dropdown, plaka
        // numarasına göre sıralanır; eşleşmeyen (PlakaKodu null olan) il
        // varsa listenin sonuna, kendi arasında alfabetik sıralanarak eklenir.
        var iller = await _context.Iller
            .Select(i => new { i.Id, i.Ad, i.PlakaKodu })
            .ToListAsync();

        return iller
            .OrderBy(i => i.PlakaKodu.HasValue ? 0 : 1)
            .ThenBy(i => i.PlakaKodu)
            .ThenBy(i => i.Ad, TurkishComparer)
            .Select(i => new IlDto { Id = i.Id, Ad = i.Ad })
            .ToList();
    }

    public async Task<List<IlceDto>> GetIlcelerAsync(int? ilId)
    {
        var query = _context.Ilceler.AsQueryable();

        if (ilId.HasValue)
            query = query.Where(x => x.IlId == ilId.Value);

        var ilceler = await query
            .Select(x => new IlceDto { Id = x.Id, IlId = x.IlId, Ad = x.Ad })
            .ToListAsync();

        return ilceler.OrderBy(x => x.Ad, TurkishComparer).ToList();
    }

    public async Task<List<MahalleDto>> GetMahallelerAsync(int ilceId)
    {
        var mahalleler = await _context.Mahalleler
            .Where(x => x.IlceId == ilceId)
            .Select(x => new MahalleDto { Id = x.Id, IlceId = x.IlceId, Ad = x.Ad })
            .ToListAsync();

        return mahalleler.OrderBy(x => x.Ad, TurkishComparer).ToList();
    }
}
