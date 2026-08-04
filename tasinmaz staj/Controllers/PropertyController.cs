using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IPropertyExportService _propertyExportService;

    public PropertyController(
        IPropertyService propertyService,
        IPropertyExportService propertyExportService)
    {
        _propertyService = propertyService;
        _propertyExportService = propertyExportService;
    }

    // Token içindeki claim'lerden userId ve role'ü okuyan yardýmcý metotlar
    private int GetUserId() =>
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

    private string GetRole() =>
        User.FindFirst(ClaimTypes.Role).Value;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PropertyFilterDto filter)
    {
        try
        {
            var result = await _propertyService.GetFilteredAsync(filter, GetUserId(), GetRole());
            return Ok(result);
        }
        catch
        {
            return StatusCode(500, new { message = "Bir hata oluþtu." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyDto dto)
    {
        // REQ-10: Admin property ekleyemez
        if (GetRole() == "Admin")
            return Forbid();

        try
        {
            var result = await _propertyService.CreateAsync(dto, GetUserId());
            return Ok(new { message = "Property added successfully.", data = result });
        }
        catch
        {
            return BadRequest(new { message = "Please fill in all required fields with valid format." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePropertyDto dto)
    {
        if (GetRole() == "Admin")
            return Forbid();

        try
        {
            var result = await _propertyService.UpdateAsync(id, dto, GetUserId());
            if (result == null)
                return NotFound(new { message = "Property not found." });

            return Ok(new { message = "Property updated successfully.", data = result });
        }
        catch
        {
            return BadRequest(new { message = "Please enter valid property details." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (GetRole() == "Admin")
            return Forbid();

        try
        {
            var success = await _propertyService.DeleteAsync(id, GetUserId());
            if (!success)
                return NotFound(new { message = "Property not found." });

            return Ok(new { message = "Property deleted successfully." });
        }
        catch
        {
            return StatusCode(500, new { message = "Bir hata oluþtu." });
        }
    }

    // REQ-1/REQ-2: Admin tum kayitlari (filtreliyse filtrelenmis), normal
    // kullanici sadece kendi kayitlarini export edebilir.
    [HttpGet("export/excel")]
    public async Task<IActionResult> ExportToExcel([FromQuery] PropertyFilterDto filter)
    {
        try
        {
            var fileBytes = await _propertyExportService.ExportToExcelAsync(filter, GetUserId(), GetRole());
            var fileName = $"properties_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";
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
    public async Task<IActionResult> ExportToPdf([FromQuery] PropertyFilterDto filter)
    {
        try
        {
            var fileBytes = await _propertyExportService.ExportToPdfAsync(filter, GetUserId(), GetRole());
            var fileName = $"properties_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
            return File(fileBytes, "application/pdf", fileName);
        }
        catch
        {
            return StatusCode(500, new { message = "Failed to export." });
        }
    }
}