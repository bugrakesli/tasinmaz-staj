using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PropertyService : IPropertyService
{
    private readonly RemsDbContext _context;

    // Coordinate (WKT metin) alanindaki poligonu, spatial sorgularda
    // (kesisim/birlesim analizi) kullanilan Geometry kolonuna da yansitmak
    // icin kullaniliyor. PropertyImportService'teki mantikla ayni.
    private readonly WKTReader _wktReader = new WKTReader();

    public PropertyService(RemsDbContext context)
    {
        _context = context;
    }

    // GetAllAsync / GetFilteredAsync / GetForExportAsync ayni PropertyDto
    // mapping'ini kullaniyordu (DRY ihlali). Expression olarak tanimlanip
    // EF Core'un SQL'e cevirebilmesi icin static tutuluyor; boylece
    // performans kaybi olmadan tek yerden yonetiliyor.
    private static readonly System.Linq.Expressions.Expression<Func<Property, PropertyDto>> ToPropertyDto =
        p => new PropertyDto
        {
            Id = p.Id,
            City = p.Mahalle.Ilce.Il.Ad,
            District = p.Mahalle.Ilce.Ad,
            Neighborhood = p.Mahalle.Ad,
            ParselNo = p.ParselNo,
            AdaNo = p.AdaNo,
            Adres = p.Adres,
            PropertyType = p.PropertyType,
            Coordinate = p.Geometry != null ? p.Geometry.AsText() : null,
            ImagePath = p.ImagePath
        };

    // dto.Coordinate gecerli bir WKT polygon ise Geometry (SRID 4326)
    // olarak dondurur; degilse null doner (Coordinate yine de kaydedilir,
    // boylece REQ'lerdeki serbest metin davranisi bozulmaz).
    private Polygon TryParseGeometry(string wkt)
    {
        if (string.IsNullOrWhiteSpace(wkt))
        {
            return null;
        }

        try
        {
            var geometry = _wktReader.Read(wkt);
            if (geometry is Polygon polygon && polygon.IsValid)
            {
                polygon.SRID = 4326;
                return polygon;
            }
        }
        catch
        {
            // Gecersiz WKT: Geometry null birakilir, Coordinate metni
            // yine de saklanir; spatial analizlerde bu kayit atlanir.
        }

        return null;
    }

    public async Task<List<PropertyDto>> GetAllAsync(int userId, string role)
    {
        IQueryable<Property> query = _context.Properties
            .AsNoTracking()
            .Include(p => p.Mahalle)
                .ThenInclude(m => m.Ilce)
                    .ThenInclude(i => i.Il);

        // Admin tüm taşınmazları görebilir.
        // Diğer kullanıcılar yalnızca kendi taşınmazlarını görebilir.
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.UserId == userId);
        }

        return await query
            .Select(ToPropertyDto)
            .ToListAsync();
    }

    private IQueryable<Property> BuildFilteredQuery(
        PropertyFilterDto filter,
        int userId,
        string role)
    {
        bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        IQueryable<Property> query = _context.Properties
            .AsNoTracking()
            .Include(p => p.Mahalle)
                .ThenInclude(m => m.Ilce)
                    .ThenInclude(i => i.Il);

        // REQ-3/REQ-4: Admin tum kayitlari filtreleyebilir, normal kullanici sadece kendisininkini
        if (!isAdmin)
        {
            query = query.Where(p => p.UserId == userId);
        }
        else if (filter.OwnerId.HasValue) // REQ-2: Admin sahibe gore de filtreleyebilir
        {
            query = query.Where(p => p.UserId == filter.OwnerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
            query = query.Where(p => p.Mahalle.Ilce.Il.Ad.ToLower() == filter.City.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.District))
            query = query.Where(p => p.Mahalle.Ilce.Ad.ToLower() == filter.District.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
            query = query.Where(p => p.Mahalle.Ad.ToLower().Contains(filter.Neighborhood.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.ParcelNumber))
            query = query.Where(p => p.ParselNo.ToLower() == filter.ParcelNumber.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.LotNumber))
            query = query.Where(p => p.AdaNo.ToLower() == filter.LotNumber.ToLower());

        if (!string.IsNullOrWhiteSpace(filter.Address))
            query = query.Where(p => p.Adres.ToLower().Contains(filter.Address.ToLower()));

        if (!string.IsNullOrWhiteSpace(filter.PropertyType))
            query = query.Where(p => p.PropertyType.ToLower().Contains(filter.PropertyType.ToLower()));

        return query;
    }

    public async Task<object> GetFilteredAsync(
        PropertyFilterDto filter,
        int userId,
        string role)
    {
        var query = BuildFilteredQuery(filter, userId, role);

        int totalRecords = await query.CountAsync();

        int pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        int pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var data = await query
            .OrderBy(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(ToPropertyDto)
            .ToListAsync();

        // REQ-6: Sonuc bulunamazsa bile 200 donuyoruz; controller/frontend
        // "No properties match the criteria." mesajini bos listeye gore gosterir.
        return new
        {
            TotalCount = totalRecords,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Data = data
        };
    }

    // 3.2.4 REQ-3/REQ-8: Export, ekranda gorunen (filtrelenmis) tum kayitlari
    // kapsamali; export islemine sayfalama uygulanmaz.
    public async Task<List<PropertyDto>> GetForExportAsync(
        PropertyFilterDto filter,
        int userId,
        string role)
    {
        var query = BuildFilteredQuery(filter, userId, role);

        return await query
            .OrderBy(p => p.Id)
            .Select(ToPropertyDto)
            .ToListAsync();
    }

    public async Task<PropertyDto> CreateAsync(
        CreatePropertyDto dto,
        int userId)
    {
        var neighborhood = await GetNeighborhoodAsync(
            dto.City,
            dto.District,
            dto.Neighborhood);

        if (neighborhood == null)
        {
            throw new KeyNotFoundException(
                "Belirtilen şehir, ilçe veya mahalle bulunamadı.");
        }

        var property = new Property
        {
            UserId = userId,
            MahalleId = neighborhood.Id,
            AdaNo = dto.LotNumber,
            ParselNo = dto.ParcelNumber,
            Adres = dto.Address,
            PropertyType = dto.PropertyType,
            Geometry = TryParseGeometry(dto.Coordinate),
            ImagePath = null
        };

        _context.Properties.Add(property);

        await _context.SaveChangesAsync();

        return new PropertyDto
        {
            Id = property.Id,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            AdaNo = property.AdaNo,
            ParselNo = property.ParselNo,
            Adres = property.Adres,
            PropertyType = property.PropertyType,
            Coordinate = property.Geometry != null ? property.Geometry.AsText() : null,
            ImagePath = property.ImagePath
        };
    }

    public async Task<PropertyDto> UpdateAsync(
        int propertyId,
        CreatePropertyDto dto,
        int userId)
    {
        var property = await _context.Properties
            .FirstOrDefaultAsync(p =>
                p.Id == propertyId &&
                p.UserId == userId);

        if (property == null)
        {
            throw new KeyNotFoundException(
                "Taşınmaz bulunamadı veya bu işlem için yetkiniz yok.");
        }

        var neighborhood = await GetNeighborhoodAsync(
            dto.City,
            dto.District,
            dto.Neighborhood);

        if (neighborhood == null)
        {
            throw new KeyNotFoundException(
                "Belirtilen şehir, ilçe veya mahalle bulunamadı.");
        }

        property.MahalleId = neighborhood.Id;
        property.AdaNo = dto.LotNumber;
        property.ParselNo = dto.ParcelNumber;
        property.Adres = dto.Address;
        property.PropertyType = dto.PropertyType;
        property.Geometry = TryParseGeometry(dto.Coordinate);

        await _context.SaveChangesAsync();

        return new PropertyDto
        {
            Id = property.Id,
            City = dto.City,
            District = dto.District,
            Neighborhood = dto.Neighborhood,
            AdaNo = property.AdaNo,
            ParselNo = property.ParselNo,
            Adres = property.Adres,
            PropertyType = property.PropertyType,
            Coordinate = property.Geometry != null ? property.Geometry.AsText() : null,
            ImagePath = property.ImagePath
        };
    }



    public async Task<bool> DeleteAsync(
        int propertyId,
        int userId)
    {
        var property = await _context.Properties
            .FirstOrDefaultAsync(p =>
                p.Id == propertyId &&
                p.UserId == userId);

        if (property == null)
        {
            return false;
        }

        _context.Properties.Remove(property);

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<Mahalle> GetNeighborhoodAsync(
        string city,
        string district,
        string neighborhood)
    {
        return await _context.Mahalleler
            .Include(m => m.Ilce)
                .ThenInclude(i => i.Il)
            .FirstOrDefaultAsync(m =>
                m.Ad == neighborhood &&
                m.Ilce.Ad == district &&
                m.Ilce.Il.Ad == city);
    }

}