using System.Collections.Generic;
using System.Threading.Tasks;

public interface ILogService
{
    Task<object> GetFilteredLogsAsync(LogFilterDto filter);
    Task<List<Log>> GetForExportAsync(LogFilterDto filter);
}