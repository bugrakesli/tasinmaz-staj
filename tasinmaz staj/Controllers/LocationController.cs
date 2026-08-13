using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    // GET api/location/iller
    [HttpGet("iller")]
    public async Task<IActionResult> GetIller()
    {
        var iller = await _locationService.GetIllerAsync();
        return Ok(iller);
    }

    // GET api/location/ilceler?ilId=6
    [HttpGet("ilceler")]
    public async Task<IActionResult> GetIlceler([FromQuery] int? ilId)
    {
        var ilceler = await _locationService.GetIlcelerAsync(ilId);
        return Ok(ilceler);
    }
}
