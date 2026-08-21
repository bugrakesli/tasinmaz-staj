using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly RemsDbContext _context;
    private readonly TokenService _tokenService;
    private readonly IEmailService _emailService;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public AuthController(
        RemsDbContext context,
        TokenService tokenService,
        IEmailService emailService,
        Microsoft.Extensions.Configuration.IConfiguration configuration,
        IMemoryCache cache)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _configuration = configuration;
        _cache = cache;
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

    // REQ (4.2-13): oturumu sonlandirir; token'in jti'sini RevokedTokens'a
    // ekleyerek ayni token'in tekrar kullanilmasini engeller (bkz. Startup.cs
    // JwtBearerEvents.OnTokenValidated).
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

        if (string.IsNullOrEmpty(jti))
            return BadRequest(new { message = "Geçersiz token." });

        var expClaim = User.FindFirst(
            System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;

        var expiresAt = long.TryParse(expClaim, out var expUnix)
            ? DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime
            : DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpireMinutes"]));

        var userIdClaim = User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        int.TryParse(userIdClaim, out var userId);

        // Ayni token ile art arda logout cagrilirsa (ornegin cift tikla)
        // benzersizlik kisitlamasi hata firlatmasin.
        var alreadyRevoked = await _context.RevokedTokens
            .AnyAsync(x => x.Jti == jti);

        if (!alreadyRevoked)
        {
            await _context.RevokedTokens.AddAsync(new RevokedToken
            {
                Jti = jti,
                UserId = userId,
                RevokedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            });

            await _context.SaveChangesAsync();
        }

        if (expiresAt > DateTime.UtcNow)
        {
            _cache.Set($"revoked_{jti}", true, expiresAt - DateTime.UtcNow);
        }

        return Ok(new { message = "Başarıyla çıkış yapıldı." });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        var genericResponse = new
        {
            message = "E-posta adresi sistemde kayıtlıysa şifre sıfırlama bağlantısı gönderildi."
        };

        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email))
            return Ok(genericResponse);

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
            return Ok(genericResponse);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

        var now = DateTime.UtcNow;

        var activeTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAt == null && x.ExpiresAt > now)
            .ToListAsync();

        foreach (var activeToken in activeTokens)
            activeToken.UsedAt = now;

        await _context.PasswordResetTokens.AddAsync(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(30)
        });

        await _context.SaveChangesAsync();

        var frontendUrl = _configuration["Frontend:Url"] ?? "http://localhost:4200";
        var resetLink = $"{frontendUrl.TrimEnd('/')}/reset-password?email=" +
                        Uri.EscapeDataString(user.Email) +
                        $"&token={Uri.EscapeDataString(rawToken)}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
        }
        catch
        {
            return StatusCode(500, new
            {
                message = "Şifre sıfırlama e-postası gönderilemedi. SMTP ayarlarını kontrol edin."
            });
        }

        return Ok(genericResponse);
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Token) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new { message = "E-posta, token ve yeni şifre zorunludur." });
        }

        if (!IsValidPassword(request.NewPassword))
        {
            return BadRequest(new
            {
                message = "Şifre 8-12 karakter olmalı ve en az bir harf, rakam ve özel karakter içermelidir."
            });
        }

        var tokenHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(request.Token)));

        var resetToken = await _context.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x =>
                x.TokenHash == tokenHash &&
                x.User.Email.ToLower() == request.Email.Trim().ToLower() &&
                x.UsedAt == null &&
                x.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
            return BadRequest(new { message = "Geçersiz veya süresi dolmuş şifre sıfırlama bağlantısı." });

        resetToken.User.Salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        resetToken.User.PasswordHash =
            PasswordHelper.HashPassword(request.NewPassword, resetToken.User.Salt);
        resetToken.UsedAt = DateTime.UtcNow;

        var otherActiveTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == resetToken.UserId && x.Id != resetToken.Id && x.UsedAt == null)
            .ToListAsync();

        foreach (var otherToken in otherActiveTokens)
            otherToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz." });
    }

    private static bool IsValidPassword(string password)
    {
        return password.Length >= 8 &&
               password.Length <= 12 &&
               password.Any(char.IsLetter) &&
               password.Any(char.IsDigit) &&
               password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; }
    }

    public class ResetPasswordRequestDto
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
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