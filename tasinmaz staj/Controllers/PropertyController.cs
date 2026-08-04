using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;
    private readonly IPropertyExportService _propertyExportService;
    private readonly IPropertyImportService _propertyImportService;

    public PropertyController(
        IPropertyService propertyService,
        IPropertyExportService propertyExportService,
        IPropertyImportService propertyImportService)
    {
        _propertyService = propertyService;
        _propertyExportService = propertyExportService;
        _propertyImportService = propertyImportService;
    }

    // Token içindeki claim'lerden userId ve role'ü okuyan yardımcı metotlar
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
            return StatusCode(500, new { message = "Bir hata oluştu." });
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
            return StatusCode(500, new { message = "Bir hata oluştu." });
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

    // REQ-8: Sadece kimlik dogrulamali normal kullanicilar erisebilir.
    [HttpPost("import/excel")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> ImportFromExcel(IFormFile file)
    {
        if (GetRole() == "Admin")
            return Forbid();

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Import failed. Please check the file format and data." });
        }

        // REQ-1: yalnizca .xlsx kabul edilir
        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Import failed. Please check the file format and data." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _propertyImportService.ImportFromExcelAsync(stream, GetUserId());

            if (!result.Success)
            {
                // REQ-4/REQ-5: dogrulama basarisizsa dosyanin tamami reddedilir
                return BadRequest(result);
            }

            // REQ-7: basarili import sonrasi guncel listeyi de birlikte doner,
            // boylece frontend property listesini yenileyebilir.
            var refreshedList = await _propertyService.GetFilteredAsync(
                new PropertyFilterDto(), GetUserId(), GetRole());

            return Ok(new
            {
                message = "Properties imported successfully.",
                importedCount = result.ImportedCount,
                data = refreshedList
            });
        }
        catch
        {
            return StatusCode(500, new { message = "Import failed. Please check the file format and data." });
        }
    }
}