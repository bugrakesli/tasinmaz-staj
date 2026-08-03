using System;

public class Log
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; }
    public string OperationType { get; set; }
    public string Description { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserIp { get; set; }
}