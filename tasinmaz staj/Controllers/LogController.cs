using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // REQ-5: Sadece Admin eriþebilir
public class LogController : ControllerBase
{
    private readonly ILogService _logService;

    public LogController(ILogService logService)
    {
        _logService = logService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] LogFilterDto filter)
    {
        var result = await _logService.GetFilteredLogsAsync(filter);
        return Ok(result);
    }
}