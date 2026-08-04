using System.Threading.Tasks;

public interface IPropertyExportService
{
    Task<byte[]> ExportToExcelAsync(PropertyFilterDto filter, int userId, string role);
    Task<byte[]> ExportToPdfAsync(PropertyFilterDto filter, int userId, string role);
}
