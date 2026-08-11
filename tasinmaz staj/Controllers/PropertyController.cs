    using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
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
    private readonly IPropertyGeometryService _propertyGeometryService;

    public PropertyController(
        IPropertyService propertyService,
        IPropertyExportService propertyExportService,
        IPropertyImportService propertyImportService,
        IPropertyGeometryService propertyGeometryService)
    {
        _propertyService = propertyService;
        _propertyExportService = propertyExportService;
        _propertyImportService = propertyImportService;
        _propertyGeometryService = propertyGeometryService;
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
            var result = await _propertyService.CreateAsync(
                dto,
                GetUserId()
            );

            return Ok(new
            {
                message = "Property added successfully.",
                data = result
            });
        }
        catch
        {
            return BadRequest(new
            {
                message = "Please fill in all required fields with valid format."
            });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreatePropertyDto dto)
    {
        // REQ-10: Admin property güncelleyemez (Create ile aynı kural)
        if (GetRole() == "Admin")
            return Forbid();

        try
        {
            var result = await _propertyService.UpdateAsync(
                id,
                dto,
                GetUserId()
            );

            return Ok(new
            {
                message = "Property updated successfully.",
                data = result
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch
        {
            return BadRequest(new
            {
                message = "Please fill in all required fields with valid format."
            });
        }
    }

    // Tek seferlik bakim islemi: Geometry alani NULL olan (ornegin bu alan
    // eklenmeden once olusturulmus veya import edilmis) taşınmazlarin
    // Geometry sutununu Coordinate (WKT) alanindan yeniden turetir.
    // Bu calistirilmadan bazi taşınmazlarda spatial/intersection/union
    // analizleri "Property not found or access denied" hatasi verebilir.
    [HttpPost("backfill-geometry")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BackfillGeometry()
    {
        try
        {
            var updatedCount = await _propertyService.BackfillGeometryAsync();
            return Ok(new
            {
                message = "Geometry backfill completed.",
                updatedCount
            });
        }
        catch
        {
            return StatusCode(500, new { message = "Geometry backfill failed." });
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

    [HttpPut("{id}/geometry")]
    public async Task<IActionResult> UpdateGeometry(
        int id,
        [FromBody] UpdatePropertyGeometryDto dto)
    {
        try
        {
            var result = await _propertyGeometryService
                .UpdateGeometryAsync(
                    id,
                    dto,
                    GetUserId(),
                    GetRole()
                );

            if (!result)
            {
                return NotFound(
                    new
                    {
                        message = "Property not found."
                    }
                );
            }

            return Ok(
                new
                {
                    message =
                        "Property geometry updated successfully."
                }
            );
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(
                new
                {
                    message = ex.Message
                }
            );
        }
        catch
        {
            return StatusCode(
                500,
                new
                {
                    message =
                        "An error occurred while updating geometry."
                }
            );
        }
    }

    [HttpPost("spatial/select")]
    public async Task<IActionResult> SelectProperties(
    [FromBody] UpdatePropertyGeometryDto dto)
    {
        try
        {
            var properties =
                await _propertyGeometryService
                    .SelectPropertiesAsync(
                        dto,
                        GetUserId(),
                        GetRole()
                    );

            return Ok(new
            {
                count = properties.Count,
                data = properties
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while selecting properties."
            });
        }
    }
    [HttpPost("spatial/intersection")]
    public async Task<IActionResult> AnalyzeIntersection(
    [FromBody] IntersectionAnalysisDto dto)
    {
        try
        {
            var result =
                await _propertyGeometryService
                    .AnalyzeIntersectionAsync(
                        dto,
                        GetUserId(),
                        GetRole()
                    );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while analyzing the intersection."
            });
        }
    }

    [HttpPost("spatial/union")]
    public async Task<IActionResult> AnalyzeUnion(
    [FromBody] UnionAnalysisDto dto)
    {
        try
        {
            var result =
                await _propertyGeometryService
                    .AnalyzeUnionAsync(
                        dto,
                        GetUserId(),
                        GetRole()
                    );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(500, new
            {
                message =
                    "An error occurred while analyzing the union."
            });
        }
    }

}