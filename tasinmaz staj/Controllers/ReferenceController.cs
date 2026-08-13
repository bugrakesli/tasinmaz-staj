using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ReferenceController : ControllerBase
{
    private readonly IReferenceService _referenceService;

    public ReferenceController(IReferenceService referenceService)
    {
        _referenceService = referenceService;
    }

    [HttpGet("iller")]
    public async Task<IActionResult> GetIller()
    {
        return Ok(await _referenceService.GetIllerAsync());
    }

    [HttpGet("iller/{ilId}/ilceler")]
    public async Task<IActionResult> GetIlceler(int ilId)
    {
        return Ok(await _referenceService.GetIlcelerAsync(ilId));
    }

    [HttpGet("ilceler/{ilceId}/mahalleler")]
    public async Task<IActionResult> GetMahalleler(int ilceId)
    {
        return Ok(await _referenceService.GetMahallelerAsync(ilceId));
    }
}
