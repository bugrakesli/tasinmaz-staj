using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TasinmazStaj.Interfaces;

namespace TasinmazStaj.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AnalysisController : ControllerBase
    {
        private readonly IGeometryService _geometryService;

        public AnalysisController(IGeometryService geometryService)
        {
            _geometryService = geometryService;
        }

        // ... (Keep existing endpoints for auto-select, manual-draw, etc.) ...

        [HttpPost("save-union")]
        public async Task<IActionResult> SaveUnion([FromBody] SaveGeometryDto request)
        {
            // The [Required] annotations in your DTO will automatically trigger 
            // ModelState invalidation if data is missing, but it's good practice to check it explicitly.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _geometryService.SaveUnionResultAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while saving the geometry.", details = ex.Message });
            }
        }
    }
}