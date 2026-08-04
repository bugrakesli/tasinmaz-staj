using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class PropertyService : IPropertyService
{
    private readonly RemsDbContext _context;


    public PropertyService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<List<PropertyDto>> GetAllAsync(int userId, string role)
    {
        IQueryable<Property> query = _context.Properties
            .Include(p => p.Mahalle)
                .ThenInclude(m => m.Ilce)
                    .ThenInclude(i => i.Il);

        // Admin tüm taþýnmazlarý görebilir.
        // Diðer kullanýcýlar yalnýzca kendi taþýnmazlarýný görebilir.
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.UserId == userId);
        }

        return await query
            .Select(p => new PropertyDto
            {
                Id = p.Id,
                City = p.Mahalle.Ilce.Il.Ad,
                District = p.Mahalle.Ilce.Ad,
                Neighborhood = p.Mahalle.Ad,
                ParselNo = p.ParselNo,
                AdaNo = p.AdaNo,
                Adres = p.Adres,
                PropertyType = p.PropertyType,
                Coordinate = p.Coordinate,
                ImagePath = p.ImagePath
            })
            .ToListAsync();
    }

    private IQueryable<Property> BuildFilteredQuery(
        PropertyFilterDto filter,
        int userId,
        string role)
    {
        bool isAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);

        IQueryable<Property> query = _context.Properties
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
            query = query.Where(p => p.Mahalle.Ilce.Il.Ad == filter.City);

        if (!string.IsNullOrWhiteSpace(filter.District))
            query = query.Where(p => p.Mahalle.Ilce.Ad == filter.District);

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
            query = query.Where(p => p.Mahalle.Ad == filter.Neighborhood);

        if (!string.IsNullOrWhiteSpace(filter.ParcelNumber))
            query = query.Where(p => p.ParselNo == filter.ParcelNumber);

        if (!string.IsNullOrWhiteSpace(filter.LotNumber))
            query = query.Where(p => p.AdaNo == filter.LotNumber);

        if (!string.IsNullOrWhiteSpace(filter.Address))
            query = query.Where(p => p.Adres.Contains(filter.Address));

        if (!string.IsNullOrWhiteSpace(filter.PropertyType))
            query = query.Where(p => p.PropertyType == filter.PropertyType);

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
            .Select(p => new PropertyDto
            {
                Id = p.Id,
                City = p.Mahalle.Ilce.Il.Ad,
                District = p.Mahalle.Ilce.Ad,
                Neighborhood = p.Mahalle.Ad,
                ParselNo = p.ParselNo,
                AdaNo = p.AdaNo,
                Adres = p.Adres,
                PropertyType = p.PropertyType,
                Coordinate = p.Coordinate,
                ImagePath = p.ImagePath
            })
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
            .Select(p => new PropertyDto
            {
                Id = p.Id,
                City = p.Mahalle.Ilce.Il.Ad,
                District = p.Mahalle.Ilce.Ad,
                Neighborhood = p.Mahalle.Ad,
                ParselNo = p.ParselNo,
                AdaNo = p.AdaNo,
                Adres = p.Adres,
                PropertyType = p.PropertyType,
                Coordinate = p.Coordinate,
                ImagePath = p.ImagePath
            })
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
                "Belirtilen þehir, ilçe veya mahalle bulunamadý.");
        }

        var property = new Property
        {
            UserId = userId,
            MahalleId = neighborhood.Id,
            AdaNo = dto.LotNumber,
            ParselNo = dto.ParcelNumber,
            Adres = dto.Address,
            PropertyType = dto.PropertyType,
            Coordinate = dto.Coordinate,
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
            Coordinate = property.Coordinate,
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
                "Taþýnmaz bulunamadý veya bu iþlem için yetkiniz yok.");
        }

        var neighborhood = await GetNeighborhoodAsync(
            dto.City,
            dto.District,
            dto.Neighborhood);

        if (neighborhood == null)
        {
            throw new KeyNotFoundException(
                "Belirtilen þehir, ilçe veya mahalle bulunamadý.");
        }

        property.MahalleId = neighborhood.Id;
        property.AdaNo = dto.LotNumber;
        property.ParselNo = dto.ParcelNumber;
        property.Adres = dto.Address;
        property.PropertyType = dto.PropertyType;
        property.Coordinate = dto.Coordinate;

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
            Coordinate = property.Coordinate,
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
