using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class PropertyExportService : IPropertyExportService
{
    private readonly IPropertyService _propertyService;

    private static readonly string[] Headers =
    {
        "Şehir", "İlçe", "Mahalle", "Ada No", "Parsel No",
        "Adres", "Taşınmaz Tipi", "Koordinat (WKT)"
    };

    public PropertyExportService(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    // REQ-3/REQ-8: export, ekranda aktif olan filtreleri ve sırayı yansıtır
    // (sayfalama uygulanmaz, tüm eşleşen kayıtlar dahil edilir).
    private async Task<List<PropertyDto>> GetDataAsync(
        PropertyFilterDto filter, int userId, string role)
    {
        return await _propertyService.GetForExportAsync(filter, userId, role);
    }

    public async Task<byte[]> ExportToExcelAsync(
        PropertyFilterDto filter, int userId, string role)
    {
        var properties = await GetDataAsync(filter, userId, role);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Properties");

        for (int col = 0; col < Headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = Headers[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var p in properties)
        {
            sheet.Cell(row, 1).Value = p.City;
            sheet.Cell(row, 2).Value = p.District;
            sheet.Cell(row, 3).Value = p.Neighborhood;
            sheet.Cell(row, 4).Value = p.AdaNo;
            sheet.Cell(row, 5).Value = p.ParselNo;
            sheet.Cell(row, 6).Value = p.Adres;
            sheet.Cell(row, 7).Value = p.PropertyType;
            sheet.Cell(row, 8).Value = p.Coordinate;
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportToPdfAsync(
        PropertyFilterDto filter, int userId, string role)
    {
        var properties = await GetDataAsync(filter, userId, role);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .Text("Taşınmaz Listesi")
                    .SemiBold().FontSize(16);

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        for (int i = 0; i < Headers.Length; i++)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in Headers)
                        {
                            header.Cell().Element(c => c
                                .Background(Colors.Grey.Lighten2)
                                .Padding(3))
                                .Text(h).SemiBold();
                        }
                    });

                    foreach (var p in properties)
                    {
                        table.Cell().Padding(3).Text(p.City);
                        table.Cell().Padding(3).Text(p.District);
                        table.Cell().Padding(3).Text(p.Neighborhood);
                        table.Cell().Padding(3).Text(p.AdaNo);
                        table.Cell().Padding(3).Text(p.ParselNo);
                        table.Cell().Padding(3).Text(p.Adres);
                        table.Cell().Padding(3).Text(p.PropertyType);
                        table.Cell().Padding(3).Text(p.Coordinate);
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }
}
