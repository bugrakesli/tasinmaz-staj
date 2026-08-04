using System.Threading.Tasks;

public interface ILogExportService
{
    Task<byte[]> ExportToExcelAsync(LogFilterDto filter);
    Task<byte[]> ExportToPdfAsync(LogFilterDto filter);
}
