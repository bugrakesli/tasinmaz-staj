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
