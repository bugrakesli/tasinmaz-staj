using System;

public class LogFilterDto
{
    public int? UserId { get; set; }
    public string Status { get; set; }
    public string OperationType { get; set; }
    public string Description { get; set; }
    public string UserIp { get; set; }

    // Timestamp aramasý için aralýk belirtmek her zaman en saðlýklýsýdýr
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Sayfalama (Pagination)
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}