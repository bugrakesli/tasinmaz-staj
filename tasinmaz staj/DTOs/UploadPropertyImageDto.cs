using Microsoft.AspNetCore.Http;

public class UploadPropertyImageDto
{
    public IFormFile Image { get; set; }
}