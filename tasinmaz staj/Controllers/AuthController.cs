using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RemsDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(RemsDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            await LogLoginAttemptAsync(0, request.Email, success: false);
            return Unauthorized(new { message = "Incorrect e-mail or password." });
        }

        var hashedInput = PasswordHelper.HashPassword(request.Password, user.Salt);

        if (hashedInput != user.PasswordHash)
        {
            await LogLoginAttemptAsync(user.Id, request.Email, success: false);
            return Unauthorized(new { message = "Incorrect e-mail or password." });
        }

        var token = _tokenService.GenerateToken(user);

        await LogLoginAttemptAsync(user.Id, request.Email, success: true);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Role = user.Role,
            Email = user.Email
        });
    }

    // AutoLogFilter yalnizca "user.Identity.IsAuthenticated == true" olan
    // isteklerde calisir; login sirasinda henuz JWT olmadigindan bu istek
    // filtre tarafindan yakalanmaz. SRS (2.1 Product Perspective) login
    // denemelerinin de loglanmasini istedigi icin burada acikca logluyoruz.
    private async Task LogLoginAttemptAsync(int userId, string email, bool success)
    {
        var log = new Log
        {
            UserId = userId,
            OperationType = "Login",
            Description = success
                ? $"{email} basariyla giris yapti."
                : $"{email} icin basarisiz giris denemesi.",
            UserIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor",
            Timestamp = DateTime.UtcNow,
            Status = success ? "Success" : "Failed"
        };

        await _context.Logs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}