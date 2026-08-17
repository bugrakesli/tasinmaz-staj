using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class LogService : ILogService
{
    private readonly RemsDbContext _context;

    public LogService(RemsDbContext context)
    {
        _context = context;
    }

    private static DateTime NormalizeToUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            // Unspecified: tarayicidan Kind bilgisi olmadan gelen yerel saat
            // olarak kabul edip sunucunun yerel saatinden UTC'ye ceviriyoruz.
            _ => DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime()
        };
    }

    private IQueryable<Log> BuildFilteredQuery(LogFilterDto filter)
    {
        var query = _context.Logs.AsQueryable();

        if (filter.Id.HasValue)
            query = query.Where(l => l.Id == filter.Id.Value);

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

        // Timestamp kolonu PostgreSQL'de "timestamp with time zone".
        // Frontend'den (datetime-local input) gelen deger Kind=Unspecified
        // oluyor; Npgsql bu kind ile timestamptz karsilastirmasinda hata
        // fırlatiyordu (filtre calismiyor gibi gorunuyordu). Kullanicinin
        // yerel saatini girdigi varsayimiyla UTC'ye ceviriyoruz.
        if (filter.StartDate.HasValue)
        {
            var startUtc = NormalizeToUtc(filter.StartDate.Value);
            query = query.Where(l => l.Timestamp >= startUtc);
        }

        if (filter.EndDate.HasValue)
        {
            var endUtc = NormalizeToUtc(filter.EndDate.Value);
            query = query.Where(l => l.Timestamp <= endUtc);
        }

        // Varsayılan olarak en yeni loglar en üstte gelsin
        return query.OrderByDescending(l => l.Timestamp);
    }

    public async Task<object> GetFilteredLogsAsync(LogFilterDto filter)
    {
        var query = BuildFilteredQuery(filter);

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

    // 3.2.6 REQ-3/REQ-7: export, aktif filtreleri ve sirayi yansitmali;
    // sayfalama uygulanmaz, filtreye uyan tum kayitlar dahil edilir.
    public async Task<List<Log>> GetForExportAsync(LogFilterDto filter)
    {
        return await BuildFilteredQuery(filter).ToListAsync();
    }
}