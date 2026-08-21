using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class PropertyImportServiceTests
{
    private static readonly string[] Headers =
    {
        "Şehir", "İlçe", "Mahalle", "Ada No", "Parsel No",
        "Adres", "Taşınmaz Tipi", "Koordinat (WKT)"
    };

    private static RemsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RemsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new RemsDbContext(options);

        context.Iller.Add(new Il { Id = 1, Ad = "Ankara" });
        context.Ilceler.Add(new Ilce { Id = 1, Ad = "Çankaya", IlId = 1 });
        context.Mahalleler.Add(new Mahalle { Id = 1, Ad = "Kızılay", IlceId = 1 });

        context.SaveChanges();
        return context;
    }

    // headerOverride: null ise standart basliklari kullanir; verilirse
    // eksik/hatali baslik senaryolarini test etmek icin kullanilir.
    // rows: her satir Headers sirasiyla ayni uzunlukta string dizisi olmali.
    private static MemoryStream BuildWorkbook(string[]? headerOverride, params string[][] rows)
    {
        var headers = headerOverride ?? Headers;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Properties");

        for (int col = 0; col < headers.Length; col++)
            sheet.Cell(1, col + 1).Value = headers[col];

        for (int row = 0; row < rows.Length; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
                sheet.Cell(row + 2, col + 1).Value = rows[row][col];
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private static string[] ValidRow(string ada = "10", string parsel = "20") =>
        new[]
        {
            "Ankara", "Çankaya", "Kızılay", ada, parsel,
            "Test Adres", "Arsa", "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

    [Fact]
    public async Task ImportFromExcelAsync_ValidRows_ImportsAllAndReturnsSuccess()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        using var stream = BuildWorkbook(null, ValidRow("10", "20"), ValidRow("11", "21"));

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.True(result.Success);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task ImportFromExcelAsync_MissingRequiredHeader_FailsWithoutImporting()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        // "Koordinat (WKT)" sutunu eksik.
        var incompleteHeaders = Headers.Take(Headers.Length - 1).ToArray();
        using var stream = BuildWorkbook(incompleteHeaders);

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(0, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task ImportFromExcelAsync_InvalidWkt_RejectsWholeFile()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        var badRow = new[]
        {
            "Ankara", "Çankaya", "Kızılay", "10", "20",
            "Test Adres", "Arsa", "GECERSIZ-WKT"
        };

        // Bir gecerli, bir gecersiz satir: REQ-4 geregi dosyanin tamami reddedilmeli.
        using var stream = BuildWorkbook(null, ValidRow(), badRow);

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.False(result.Success);
        Assert.Equal(0, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task ImportFromExcelAsync_UnmatchedNeighborhood_RejectsWholeFile()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        var badRow = new[]
        {
            "Ankara", "Çankaya", "OlmayanMahalle", "10", "20",
            "Test Adres", "Arsa", "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        using var stream = BuildWorkbook(null, badRow);

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.False(result.Success);
        Assert.Contains(result.Errors, e => e.Contains("eşleşen"));
    }

    [Fact]
    public async Task ImportFromExcelAsync_EmptyRequiredField_RejectsWholeFile()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        var badRow = new[]
        {
            "Ankara", "Çankaya", "Kızılay", "", "20",
            "Test Adres", "Arsa", "POLYGON((0 0,0 1,1 1,1 0,0 0))"
        };

        using var stream = BuildWorkbook(null, badRow);

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.False(result.Success);
        Assert.Equal(0, await context.Properties.CountAsync());
    }

    [Fact]
    public async Task ImportFromExcelAsync_NoDataRows_FailsWithMessage()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        using var stream = BuildWorkbook(null);

        var result = await service.ImportFromExcelAsync(stream, userId: 1);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task ImportFromExcelAsync_ImportedProperty_IsAssignedToRequestingUser()
    {
        using var context = CreateContext();
        var service = new PropertyImportService(context);

        using var stream = BuildWorkbook(null, ValidRow());

        await service.ImportFromExcelAsync(stream, userId: 7);

        var property = await context.Properties.FirstAsync();
        Assert.Equal(7, property.UserId);
    }
}
