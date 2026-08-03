using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

public interface IPropertyImageService
{
    Task<string> UploadAsync(
    int propertyId,
    IFormFile image,
    int userId);

Task<bool> DeleteAsync(
    int propertyId,
    int userId);
}
