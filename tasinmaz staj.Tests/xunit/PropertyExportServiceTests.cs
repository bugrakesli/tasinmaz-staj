using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Moq;
using Xunit;

public class PropertyExportServiceTests
{
    private static List<PropertyDto> SampleProperties() => new List<PropertyDto>
    {
        new PropertyDto
        {
            Id = 1,
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Kızılay",
            AdaNo = "10",
            ParselNo = "20",
            Adres = "Test Adres 1",
            PropertyType = "Arsa",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        },
        new PropertyDto
        {
            Id = 2,
            City = "Ankara",
            District = "Çankaya",
            Neighborhood = "Kızılay",
            AdaNo = "11",
            ParselNo = "21",
            Adres = "Test Adres 2",
            PropertyType = "Bina",
            Coordinate = "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        }
    };

    private static Mock<IPropertyService> MockPropertyService(List<PropertyDto> data)
    {
        var mock = new Mock<IPropertyService>();
        mock.Setup(s => s.GetForExportAsync(
                It.IsAny<PropertyFilterDto>(),
                It.IsAny<int>(),
                It.IsAny<string>()))
            .ReturnsAsync(data);
        return mock;
    }

    [Fact]
    public async Task ExportToExcelAsync_WritesHeaderAndOneRowPerProperty()
    {
        var mock = MockPropertyService(SampleProperties());
        var service = new PropertyExportService(mock.Object);

        var bytes = await service.ExportToExcelAsync(new PropertyFilterDto(), userId: 1, role: "Admin");

        Assert.NotEmpty(bytes);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal("Şehir", sheet.Cell(1, 1).GetString());
        Assert.Equal("Koordinat (WKT)", sheet.Cell(1, 8).GetString());

        // 2 veri satiri + 1 baslik satiri = son kullanilan satir 3.
        Assert.Equal(3, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal("Test Adres 1", sheet.Cell(2, 6).GetString());
        Assert.Equal("Test Adres 2", sheet.Cell(3, 6).GetString());
    }

    [Fact]
    public async Task ExportToExcelAsync_NoProperties_WritesOnlyHeaderRow()
    {
        var mock = MockPropertyService(new List<PropertyDto>());
        var service = new PropertyExportService(mock.Object);

        var bytes = await service.ExportToExcelAsync(new PropertyFilterDto(), userId: 1, role: "Admin");

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheet(1);

        Assert.Equal(1, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public async Task ExportToPdfAsync_ReturnsNonEmptyPdfBytes()
    {
        var mock = MockPropertyService(SampleProperties());
        var service = new PropertyExportService(mock.Object);

        var bytes = await service.ExportToPdfAsync(new PropertyFilterDto(), userId: 1, role: "Admin");

        Assert.NotEmpty(bytes);
        // PDF dosyalari "%PDF-" imzasiyla baslar.
        Assert.Equal(0x25, bytes[0]); // '%'
        Assert.Equal(0x50, bytes[1]); // 'P'
        Assert.Equal(0x44, bytes[2]); // 'D'
        Assert.Equal(0x46, bytes[3]); // 'F'
    }

    [Fact]
    public async Task ExportToExcelAsync_PassesFilterUserIdAndRoleThrough()
    {
        var mock = MockPropertyService(SampleProperties());
        var service = new PropertyExportService(mock.Object);

        var filter = new PropertyFilterDto { City = "Ankara" };

        await service.ExportToExcelAsync(filter, userId: 42, role: "User");

        mock.Verify(s => s.GetForExportAsync(filter, 42, "User"), Times.Once);
    }
}
