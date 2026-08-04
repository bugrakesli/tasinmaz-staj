using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

[ApiController]
[Route("api/properties/{propertyId}/image")]
[Authorize]
public class PropertyImageController : ControllerBase
{
    private readonly IPropertyImageService
    _propertyImageService;


public PropertyImageController(
    IPropertyImageService propertyImageService)
    {
        _propertyImageService =
            propertyImageService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        int propertyId,
        [FromForm] UploadPropertyImageDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();

            var imagePath = await _propertyImageService.UploadAsync(
                propertyId,
                dto.Image,
                userId);

            return Ok(new
            {
                Message = "Görsel başarıyla yüklendi.",
                ImagePath = imagePath
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                Message = ex.Message
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new
            {
                Message = ex.Message
            });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        int propertyId)
    {
        var userId = GetCurrentUserId();

        var deleted =
            await _propertyImageService
                .DeleteAsync(
                    propertyId,
                    userId);

        if (!deleted)
        {
            return NotFound(new
            {
                Message =
                    "Görsel veya taşınmaz bulunamadı."
            });
        }

        return Ok(new
        {
            Message =
                "Görsel başarıyla silindi."
        });
    }

    private int GetCurrentUserId()
    {
        var userIdValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!int.TryParse(
                userIdValue,
                out int userId))
        {
            throw new UnauthorizedAccessException(
                "Geçerli kullanıcı bilgisi bulunamadı.");
        }

        return userId;
    }

}
