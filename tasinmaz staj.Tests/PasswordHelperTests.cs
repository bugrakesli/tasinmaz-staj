using System;
using Xunit;

public class PasswordHelperTests
{
    [Fact]
    public void GenerateSalt_ReturnsNonEmptyString()
    {
        // Act
        var salt = PasswordHelper.GenerateSalt();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(salt));
    }

    [Fact]
    public void HashPassword_SameInputAndSalt_ProducesSameHash()
    {
        // Arrange
        var password = "MySecretPassword!";
        var salt = PasswordHelper.GenerateSalt();

        // Act
        var hash1 = PasswordHelper.HashPassword(password, salt);
        var hash2 = PasswordHelper.HashPassword(password, salt);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashPassword_DifferentSalts_ProduceDifferentHashes()
    {
        // Arrange
        var password = "MySecretPassword!";
        var salt1 = PasswordHelper.GenerateSalt();
        var salt2 = PasswordHelper.GenerateSalt();

        // Act
        var hash1 = PasswordHelper.HashPassword(password, salt1);
        var hash2 = PasswordHelper.HashPassword(password, salt2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_EmptyPassword_ReturnsHash()
    {
        // Arrange
        var password = "";
        var salt = PasswordHelper.GenerateSalt();

        // Act
        var hash = PasswordHelper.HashPassword(password, salt);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
    }
}
