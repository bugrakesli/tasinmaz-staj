using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

// AuthController projede en kritik guvenlik yuzeyi olmasina ragmen (login,
// JWT blacklist/logout, forgot/reset-password) hic test edilmiyordu. Diger
// test dosyalarindaki InMemory DbContext + Moq deseni burada da kullanildi.
public class AuthControllerTests
{
    private static RemsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RemsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RemsDbContext(options);
    }

    private static (AuthController Controller, RemsDbContext Context, Mock<IEmailService> EmailMock) CreateController()
    {
        var context = CreateContext();

        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Jwt:Key"])
            .Returns("ThisIsAVerySecureKeyWithEnoughLengthToWorkProperlyForHmacSha256!!!");
        configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        configMock.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");
        configMock.Setup(c => c["Frontend:Url"]).Returns("http://localhost:4200");

        var tokenService = new TokenService(configMock.Object);

        var emailMock = new Mock<IEmailService>();
        emailMock
            .Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        var controller = new AuthController(context, tokenService, emailMock.Object, configMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (controller, context, emailMock);
    }

    private static User AddUser(RemsDbContext context, string email, string password, string role = "User")
    {
        var salt = PasswordHelper.GenerateSalt();
        var user = new User
        {
            Email = email,
            Role = role,
            Salt = salt,
            PasswordHash = PasswordHelper.HashPassword(password, salt)
        };

        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    private static void AuthenticateAs(AuthController controller, int userId, string jti)
    {
        var claims = new[]
        {
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, jti),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        controller.ControllerContext.HttpContext.User =
            new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private static string HashToken(string rawToken) =>
        Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

    // ---- Login ----

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsTokenAndRole()
    {
        var (controller, context, _) = CreateController();
        AddUser(context, "owner@test.com", "Passw0rd!", "Admin");

        var result = await controller.Login(
            new LoginRequestDto { Email = "owner@test.com", Password = "Passw0rd!" });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("Admin", response.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var (controller, context, _) = CreateController();
        AddUser(context, "owner@test.com", "Passw0rd!");

        var result = await controller.Login(
            new LoginRequestDto { Email = "owner@test.com", Password = "yanlis-sifre" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var (controller, _, _) = CreateController();

        var result = await controller.Login(
            new LoginRequestDto { Email = "yok@test.com", Password = "x" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    // ---- Logout / JWT blacklist ----

    [Fact]
    public async Task Logout_RevokesTokenJti()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");
        var jti = Guid.NewGuid().ToString();
        AuthenticateAs(controller, user.Id, jti);

        var result = await controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        Assert.True(await context.RevokedTokens.AnyAsync(x => x.Jti == jti));
    }

    [Fact]
    public async Task Logout_CalledTwiceWithSameToken_DoesNotThrowAndStaysSingleRow()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");
        var jti = Guid.NewGuid().ToString();
        AuthenticateAs(controller, user.Id, jti);

        await controller.Logout();
        var secondResult = await controller.Logout();

        Assert.IsType<OkObjectResult>(secondResult);
        Assert.Equal(1, await context.RevokedTokens.CountAsync(x => x.Jti == jti));
    }

    // ---- Forgot password ----

    [Fact]
    public async Task ForgotPassword_UnknownEmail_ReturnsGenericOkWithoutSideEffects()
    {
        var (controller, context, emailMock) = CreateController();

        var result = await controller.ForgotPassword(
            new AuthController.ForgotPasswordRequestDto { Email = "yok@test.com" });

        Assert.IsType<OkObjectResult>(result);
        Assert.False(await context.PasswordResetTokens.AnyAsync());
        emailMock.Verify(
            e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_KnownEmail_CreatesTokenAndSendsEmail()
    {
        var (controller, context, emailMock) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");

        var result = await controller.ForgotPassword(
            new AuthController.ForgotPasswordRequestDto { Email = user.Email });

        Assert.IsType<OkObjectResult>(result);
        Assert.True(await context.PasswordResetTokens
            .AnyAsync(x => x.UserId == user.Id && x.UsedAt == null));
        emailMock.Verify(
            e => e.SendPasswordResetEmailAsync(user.Email, It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_CalledTwice_OnlyNewestTokenStaysActive()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");

        await controller.ForgotPassword(new AuthController.ForgotPasswordRequestDto { Email = user.Email });
        await controller.ForgotPassword(new AuthController.ForgotPasswordRequestDto { Email = user.Email });

        var activeCount = await context.PasswordResetTokens
            .CountAsync(x => x.UserId == user.Id && x.UsedAt == null);
        Assert.Equal(1, activeCount);
    }

    // ---- Reset password ----

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        var (controller, context, _) = CreateController();
        AddUser(context, "owner@test.com", "Passw0rd!");

        var result = await controller.ResetPassword(new AuthController.ResetPasswordRequestDto
        {
            Email = "owner@test.com",
            Token = "gecersiz-token",
            NewPassword = "Yeni1234!"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WithExpiredToken_ReturnsBadRequest()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");

        const string rawToken = "expired-raw-token";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        });
        await context.SaveChangesAsync();

        var result = await controller.ResetPassword(new AuthController.ResetPasswordRequestDto
        {
            Email = user.Email,
            Token = rawToken,
            NewPassword = "Yeni1234!"
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ReturnsBadRequest()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "Passw0rd!");

        const string rawToken = "weak-pw-raw-token";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await context.SaveChangesAsync();

        var result = await controller.ResetPassword(new AuthController.ResetPasswordRequestDto
        {
            Email = user.Email,
            Token = rawToken,
            NewPassword = "zayif" // rakam/ozel karakter yok -> kurala uymuyor
        });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_UpdatesPasswordAndMarksTokenUsed()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "EskiSifre1!");
        var oldHash = user.PasswordHash;

        const string rawToken = "valid-raw-token";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await context.SaveChangesAsync();

        var result = await controller.ResetPassword(new AuthController.ResetPasswordRequestDto
        {
            Email = user.Email,
            Token = rawToken,
            NewPassword = "YeniSifre1!"
        });

        Assert.IsType<OkObjectResult>(result);

        var updatedUser = await context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.NotEqual(oldHash, updatedUser!.PasswordHash);

        var usedToken = await context.PasswordResetTokens
            .FirstAsync(x => x.TokenHash == HashToken(rawToken));
        Assert.NotNull(usedToken.UsedAt);
    }

    [Fact]
    public async Task ResetPassword_TokenCannotBeReusedAfterSuccess()
    {
        var (controller, context, _) = CreateController();
        var user = AddUser(context, "owner@test.com", "EskiSifre1!");

        const string rawToken = "one-time-raw-token";
        context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = HashToken(rawToken),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30)
        });
        await context.SaveChangesAsync();

        var request = new AuthController.ResetPasswordRequestDto
        {
            Email = user.Email,
            Token = rawToken,
            NewPassword = "YeniSifre1!"
        };

        var firstResult = await controller.ResetPassword(request);
        var secondResult = await controller.ResetPassword(request);

        Assert.IsType<OkObjectResult>(firstResult);
        Assert.IsType<BadRequestObjectResult>(secondResult);
    }
}
