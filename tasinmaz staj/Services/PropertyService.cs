using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class PropertyService : IPropertyService
{
    private readonly RemsDbContext _context;

    public PropertyService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<List<PropertyDto>> GetAllAsync(int userId, string role)
    {
        try
        {
            var query = _context.Properties
                .Include(p => p.Mahalle).ThenInclude(m => m.Ilce).ThenInclude(i => i.Il)
                .AsQueryable();

            // Admin hepsini görür, regular user sadece kendi kayýtlarýný
            if (role != "Admin")
                query = query.Where(p => p.UserId == userId);

            return await query.Select(p => new PropertyDto
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
            }).ToListAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<PropertyDto> CreateAsync(CreatePropertyDto dto, int userId)
    {
        try
        {
            var property = new Property
            {
                UserId = userId,
                MahalleId = dto.MahalleId,
                ParselNo = dto.ParselNo,
                AdaNo = dto.AdaNo,
                Adres = dto.Adres,
                PropertyType = dto.PropertyType,
                Coordinate = dto.Coordinate
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            return await GetByIdInternal(property.Id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<PropertyDto> UpdateAsync(int propertyId, CreatePropertyDto dto, int userId)
    {
        try
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);

            if (property == null)
                return null;

            property.MahalleId = dto.MahalleId;
            property.ParselNo = dto.ParselNo;
            property.AdaNo = dto.AdaNo;
            property.Adres = dto.Adres;
            property.PropertyType = dto.PropertyType;
            property.Coordinate = dto.Coordinate;

            await _context.SaveChangesAsync();

            return await GetByIdInternal(property.Id);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int propertyId, int userId)
    {
        try
        {
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.Id == propertyId && p.UserId == userId);

            if (property == null)
                return false;

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<PropertyDto> GetByIdInternal(int id)
    {
        var p = await _context.Properties
            .Include(x => x.Mahalle).ThenInclude(m => m.Ilce).ThenInclude(i => i.Il)
            .FirstAsync(x => x.Id == id);

        return new PropertyDto
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
        };
    }
}