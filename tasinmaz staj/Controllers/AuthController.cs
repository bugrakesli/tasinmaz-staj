using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;

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
            return Unauthorized(new { message = "Incorrect e-mail or password." });

        var hashedInput = PasswordHelper.HashPassword(request.Password, user.Salt);

        if (hashedInput != user.PasswordHash)
            return Unauthorized(new { message = "Incorrect e-mail or password." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Role = user.Role,
            Email = user.Email
        });
    }
}