using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class PropertyImageService : IPropertyImageService
{
	private readonly RemsDbContext _context;
	private readonly IWebHostEnvironment _environment;

private const long MaxFileSize = 100 * 1024 * 1024;

	public PropertyImageService(
		RemsDbContext context,
		IWebHostEnvironment environment)
	{
		_context = context;
		_environment = environment;
	}

	public async Task<string> UploadAsync(
		int propertyId,
		IFormFile image,
		int userId)
	{
		if (image == null || image.Length == 0)
		{
			throw new ArgumentException(
				"Yüklenecek bir görsel seçilmelidir.");
		}

		if (image.Length > MaxFileSize)
		{
			throw new ArgumentException(
				"Görsel boyutu 100 MB'dan büyük olamaz.");
		}

		var allowedExtensions = new[]
		{
		".jpg",
		".jpeg",
		".png"
	};

		var extension = Path
			.GetExtension(image.FileName)
			.ToLowerInvariant();

		if (Array.IndexOf(
				allowedExtensions,
				extension) < 0)
		{
			throw new ArgumentException(
				"Yalnızca JPG, JPEG ve PNG dosyaları yüklenebilir.");
		}

		var property = await _context.Properties
			.FirstOrDefaultAsync(p =>
				p.Id == propertyId &&
				p.UserId == userId);

		if (property == null)
		{
			throw new KeyNotFoundException(
				"Taşınmaz bulunamadı veya bu işlem için yetkiniz yok.");
		}

		var webRootPath = _environment.WebRootPath;

		if (string.IsNullOrWhiteSpace(webRootPath))
		{
			webRootPath = Path.Combine(
				_environment.ContentRootPath,
				"wwwroot");
		}

		var uploadFolder = Path.Combine(
			webRootPath,
			"uploads",
			"properties");

		if (!Directory.Exists(uploadFolder))
		{
			Directory.CreateDirectory(uploadFolder);
		}

		if (!string.IsNullOrWhiteSpace(property.ImagePath))
		{
			var oldFileName = Path.GetFileName(
				property.ImagePath);

			var oldFilePath = Path.Combine(
				uploadFolder,
				oldFileName);

			if (File.Exists(oldFilePath))
			{
				File.Delete(oldFilePath);
			}
		}

		var fileName =
			Guid.NewGuid().ToString("N") +
			extension;

		var filePath = Path.Combine(
			uploadFolder,
			fileName);

		using (var stream =
			   new FileStream(
				   filePath,
				   FileMode.Create))
		{
			await image.CopyToAsync(stream);
		}

		var imageUrl =
			"/uploads/properties/" +
			fileName;

		property.ImagePath = imageUrl;

		await _context.SaveChangesAsync();

		return imageUrl;
	}

	public async Task<bool> DeleteAsync(
		int propertyId,
		int userId)
	{
		var property = await _context.Properties
			.FirstOrDefaultAsync(p =>
				p.Id == propertyId &&
				p.UserId == userId);

		if (property == null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(
				property.ImagePath))
		{
			return false;
		}

		var webRootPath = _environment.WebRootPath;

		if (string.IsNullOrWhiteSpace(webRootPath))
		{
			webRootPath = Path.Combine(
				_environment.ContentRootPath,
				"wwwroot");
		}

		var fileName = Path.GetFileName(
			property.ImagePath);

		var filePath = Path.Combine(
			webRootPath,
			"uploads",
			"properties",
			fileName);

		if (File.Exists(filePath))
		{
			File.Delete(filePath);
		}

		property.ImagePath = null;

		await _context.SaveChangesAsync();

		return true;
	}
}
