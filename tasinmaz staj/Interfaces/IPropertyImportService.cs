using System.IO;
using System.Threading.Tasks;

public interface IPropertyImportService
{
    Task<PropertyImportResultDto> ImportFromExcelAsync(Stream fileStream, int userId);
}
