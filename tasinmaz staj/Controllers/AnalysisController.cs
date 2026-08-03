using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize] // REQ-1: Area Analysis sayfasýna sadece doðrulanmýþ kullanýcýlar eriþebilir[cite: 1]
public class AnalysisController : ControllerBase
{
    private readonly IGeometryService _geometryService;

    public AnalysisController(IGeometryService geometryService)
    {
        _geometryService = geometryService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim.Value) : 0;
    }

    [HttpPost("save-union")]
    public async Task<IActionResult> SaveUnionGeometry([FromBody] SaveGeometryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Lütfen geçerli geometri ve alan verisi saðlayýn." });

        var success = await _geometryService.SaveUnionResultAsync(dto, GetUserId());

        if (success)
            return Ok(new { message = "Geometri baþarýyla veritabanýna kaydedildi." });

        return StatusCode(500, new { message = "Geometri kaydedilirken bir hata oluþtu." });
    }
}