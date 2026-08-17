using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // REQ-5: Sadece Admin erişebilir
public class LogController : ControllerBase
{
    private readonly ILogService _logService;
    private readonly ILogExportService _logExportService;

    public LogController(ILogService logService, ILogExportService logExportService)
    {
        _logService = logService;
        _logExportService = logExportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto filter)
    {
        try
        {
            var result = await _logService.GetFilteredLogsAsync(filter);
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new { message = "Loglar getirilirken bir hata oluştu." });
        }
    }

    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportToExcel([FromQuery] LogFilterDto filter)
    {
        try
        {
            var fileBytes = await _logExportService.ExportToExcelAsync(filter);
            var fileName = $"logs_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch
        {
            return StatusCode(500, new { message = "Failed to export." });
        }
    }

    [HttpGet("export/pdf")]
    public async Task<IActionResult> ExportToPdf([FromQuery] LogFilterDto filter)
    {
        try
        {
            var fileBytes = await _logExportService.ExportToPdfAsync(filter);
            var fileName = $"logs_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return File(fileBytes, "application/pdf", fileName);
        }
        catch
        {
            return StatusCode(500, new { message = "Failed to export." });
        }
    }
}