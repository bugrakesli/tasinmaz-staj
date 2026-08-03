using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class LogService : ILogService
{
    private readonly RemsDbContext _context;

    public LogService(RemsDbContext context)
    {
        _context = context;
    }

    public async Task<object> GetFilteredLogsAsync(LogFilterDto filter)
    {
        var query = _context.Logs.AsQueryable();

        if (filter.UserId.HasValue)
            query = query.Where(l => l.UserId == filter.UserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(l => l.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.OperationType))
            query = query.Where(l => l.OperationType == filter.OperationType);

        if (!string.IsNullOrWhiteSpace(filter.Description))
            query = query.Where(l => l.Description.Contains(filter.Description));

        if (!string.IsNullOrWhiteSpace(filter.UserIp))
            query = query.Where(l => l.UserIp == filter.UserIp);

        if (filter.StartDate.HasValue)
            query = query.Where(l => l.Timestamp >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(l => l.Timestamp <= filter.EndDate.Value);

        // Varsayýlan olarak en yeni loglar en üstte gelsin
        query = query.OrderByDescending(l => l.Timestamp);

        int totalRecords = await query.CountAsync();

        var logs = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new
        {
            TotalCount = totalRecords,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize,
            Data = logs
        };
    }
}