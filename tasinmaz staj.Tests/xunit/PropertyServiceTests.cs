using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PropertyServiceTests
{
    private static RemsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RemsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RemsDbContext(options);

        var il = new Il { Id = 1, Ad = "Ankara" };
        var ilce = new Ilce { Id = 1, Ad = "Çankaya", IlId = 1 };
        var mahalle = new Mahalle { Id = 1, Ad = "Kızılay", IlceId = 1 };

        context.Iller.Add(il);
        context.Ilceler.Add(ilce);
        context.Mahalleler.Add(mahalle);

        context.Users.Add(new User { Id = 1, Email = "owner@test.com", Role = "User", PasswordHash = "x", Salt = "y" });
        context.Users.Add(new User { Id = 2, Email = "other@test.com", Role = "User", PasswordHash = "x", Salt = "y" });

        context.Properties.Add(new Property
        {
            Id = 1,
            UserId = 1,
            MahalleId = 1,
            AdaNo = "10",
            ParselNo = "20",
            Adres = "Test Adres 1",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        });

        context.Properties.Add(new Property
        {
            Id = 2,
            UserId = 2,
            MahalleId = 1,
            AdaNo = "11",
            ParselNo = "21",
            Adres = "Test Adres 2",
            PropertyType = "Bina",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task GetAllAsync_NonAdmin_ReturnsOnlyOwnProperties()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var result = await service.GetAllAsync(userId: 1, role: "User");

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_Admin_ReturnsAllProperties()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var result = await service.GetAllAsync(userId: 1, role: "Admin");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task CreateAsync_ValidNeighborhood_CreatesProperty()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var dto = new CreatePropertyDto
        {
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Kızılay",
            LotNumber = "99",
            ParcelNumber = "88",
            Address = "Yeni Adres",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        var result = await service.CreateAsync(dto, userId: 1);

        Assert.NotEqual(0, result.Id);
        Assert.Equal("Ankara", result.City);
        Assert.Equal(3, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task CreateAsync_UnknownNeighborhood_ThrowsKeyNotFound()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var dto = new CreatePropertyDto
        {
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "OlmayanMahalle",
            LotNumber = "1",
            ParcelNumber = "1",
            Address = "Adres",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(
            () => service.CreateAsync(dto, userId: 1));
    }

    [Fact]
    public async Task UpdateAsync_OwnerUpdatesOwnProperty_Succeeds()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var dto = new CreatePropertyDto
        {
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Kızılay",
            LotNumber = "55",
            ParcelNumber = "66",
            Address = "Güncellenmiş Adres",
            PropertyType = "Bina",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        var result = await service.UpdateAsync(propertyId: 1, dto, userId: 1);

        Assert.Equal("Güncellenmiş Adres", result.Adres);
        Assert.Equal("55", result.AdaNo);
    }

    [Fact]
    public async Task UpdateAsync_NonOwnerCannotUpdate_ThrowsKeyNotFound()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var dto = new CreatePropertyDto
        {
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Kızılay",
            LotNumber = "1",
            ParcelNumber = "1",
            Address = "Adres",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        // Property Id 1 belongs to userId 1, not userId 2.
        await Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(
            () => service.UpdateAsync(propertyId: 1, dto, userId: 2));
    }

    [Fact]
    public async Task DeleteAsync_Owner_DeletesProperty()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var deleted = await service.DeleteAsync(propertyId: 1, userId: 1);

        Assert.True(deleted);
        Assert.Equal(1, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task DeleteAsync_NonOwner_ReturnsFalse()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var deleted = await service.DeleteAsync(propertyId: 1, userId: 2);

        Assert.False(deleted);
        Assert.Equal(2, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByCity()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var filter = new PropertyFilterDto { City = "Ankara", PageNumber = 1, PageSize = 10 };

        dynamic result = await service.GetFilteredAsync(filter, userId: 1, role: "Admin");

        Assert.Equal(2, (int)result.TotalCount);
    }

    [Fact]
    public async Task GetFilteredAsync_FiltersByAddressContains()
    {
        using var context = CreateContext();
        var service = new PropertyService(context);

        var filter = new PropertyFilterDto { Address = "Adres 2", PageNumber = 1, PageSize = 10 };

        dynamic result = await service.GetFilteredAsync(filter, userId: 2, role: "User");

        Assert.Equal(1, (int)result.TotalCount);
    }
}
