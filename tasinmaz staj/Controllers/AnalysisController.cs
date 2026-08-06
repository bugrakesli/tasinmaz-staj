using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
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

        private int GetUserId() =>
            int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        [HttpPost("manual-draw")]
        public async Task<IActionResult> SaveManualGeometry([FromBody] SaveManualGeometryDto dto)
        {
            try
            {
                var result = await _geometryService.SaveManualGeometryAsync(dto, GetUserId());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("auto-select")]
        public async Task<IActionResult> AutoSelect()
        {
            try
            {
                var results = await _geometryService.GetAutoSelectGeometriesAsync(GetUserId());
                return Ok(results);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost("intersection")]
        public async Task<IActionResult> Intersection()
        {
            try
            {
                var result = await _geometryService.ComputeIntersectionAsync(GetUserId());
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("union-ab")]
        public async Task<IActionResult> UnionAB()
        {
            try
            {
                var result = await _geometryService.ComputeUnionAsync(GetUserId(), includeC: false);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("union-abc")]
        public async Task<IActionResult> UnionABC()
        {
            try
            {
                var result = await _geometryService.ComputeUnionAsync(GetUserId(), includeC: true);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            await _geometryService.ClearAsync(GetUserId());
            return Ok(new { message = "Cleared." });
        }
    }
}