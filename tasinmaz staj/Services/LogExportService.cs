using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class LogExportService : ILogExportService
{
    private readonly ILogService _logService;

    private static readonly string[] Headers =
    {
        "Kullanıcı Id", "Durum", "İşlem Tipi", "Açıklama", "Zaman", "IP Adresi"
    };

    public LogExportService(ILogService logService)
    {
        _logService = logService;
    }

    public async Task<byte[]> ExportToExcelAsync(LogFilterDto filter)
    {
        var logs = await _logService.GetForExportAsync(filter);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Logs");

        for (int col = 0; col < Headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = Headers[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        int row = 2;
        foreach (var log in logs)
        {
            sheet.Cell(row, 1).Value = log.UserId;
            sheet.Cell(row, 2).Value = log.Status;
            sheet.Cell(row, 3).Value = log.OperationType;
            sheet.Cell(row, 4).Value = log.Description;
            sheet.Cell(row, 5).Value = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            sheet.Cell(row, 6).Value = log.UserIp;
            row++;
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<byte[]> ExportToPdfAsync(LogFilterDto filter)
    {
        var logs = await _logService.GetForExportAsync(filter);

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header()
                    .Text("Sistem Logları")
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

                    foreach (var log in logs)
                    {
                        table.Cell().Padding(3).Text(log.UserId.ToString());
                        table.Cell().Padding(3).Text(log.Status);
                        table.Cell().Padding(3).Text(log.OperationType);
                        table.Cell().Padding(3).Text(log.Description);
                        table.Cell().Padding(3).Text(log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
                        table.Cell().Padding(3).Text(log.UserIp);
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
