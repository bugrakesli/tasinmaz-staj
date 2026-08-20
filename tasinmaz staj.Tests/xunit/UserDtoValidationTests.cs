using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

public class UserDtoValidationTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void Password_MeetsAllRequirements_IsValid()
    {
        // Arrange
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "Val1d!ps" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Password_TooShort_IsInvalid()
    {
        // Arrange (7 chars total)
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "Va1!dps" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Password_TooLong_IsInvalid()
    {
        // Arrange (13 chars total)
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "Valid1Password!2" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Password_NoSpecialChar_IsInvalid()
    {
        // Arrange
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "Valid1Password" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Password_NoDigit_IsInvalid()
    {
        // Arrange
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "ValidPassword!" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void Password_NoLetter_IsInvalid()
    {
        // Arrange
        var dto = new UserCreateDto { Email = "test@test.com", Role = "User", Password = "1234567!" };

        // Act
        var results = ValidateModel(dto);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }
}
