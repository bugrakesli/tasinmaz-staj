using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

public class TokenServiceTests
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        _mockConfig = new Mock<IConfiguration>();

        // Set up IConfiguration mock values required by TokenService
        _mockConfig.Setup(c => c["Jwt:Key"]).Returns("ThisIsAVerySecureKeyWithEnoughLengthToWorkProperlyForHmacSha256!!!");
        _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
        _mockConfig.Setup(c => c["Jwt:ExpireMinutes"]).Returns("60");

        _tokenService = new TokenService(_mockConfig.Object);
    }

    [Fact]
    public void GenerateToken_ReturnsValidJwt()
    {
        // Arrange
        var user = new User { Id = 1, Email = "test@example.com", Role = "Admin" };

        // Act
        var tokenString = _tokenService.GenerateToken(user);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(tokenString));
        var handler = new JwtSecurityTokenHandler();
        Assert.True(handler.CanReadToken(tokenString));
    }

    [Fact]
    public void GenerateToken_ContainsExpectedClaims()
    {
        // Arrange
        var user = new User { Id = 42, Email = "user42@example.com", Role = "User" };

        // Act
        var tokenString = _tokenService.GenerateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(tokenString);

        var nameIdentifierClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);
        var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role);

        Assert.NotNull(nameIdentifierClaim);
        Assert.Equal("42", nameIdentifierClaim.Value);

        Assert.NotNull(emailClaim);
        Assert.Equal("user42@example.com", emailClaim.Value);

        Assert.NotNull(roleClaim);
        Assert.Equal("User", roleClaim.Value);
    }
}

// Minimal mock user class to ensure compilation if User entity is not available in current scope
// This won't conflict with the real one since it's just in the test project namespace (and hopefully we can reference the real one)
// But wait, we referenced the main project, so User should be available. Let's not redefine User unless necessary.
