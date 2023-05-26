using FluentAssertions;
using FluentValidation.Results;
using Mercadona.Backend.Models;
using Mercadona.Backend.Validation;

namespace Mercadona.Tests.Validation;

public class UserModelValidatorTests
{
    private readonly UserModelValidator _userModelValidator;

    public UserModelValidatorTests()
    {
        _userModelValidator = new UserModelValidator();
    }

    [Fact]
    public void ContainsNumericCharacters_NoNumericCharacters_ShouldReturnFalse()
    {
        // Arrange
        int minimumNumericCharacters = 2;
        string password = "nonumeric";

        // Act
        bool result = _userModelValidator.ContainsNumericCharacters(
            password,
            minimumNumericCharacters
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsNumericCharacters_HasNumericCharacters_ShouldReturnTrue()
    {
        // Arrange
        int minimumNumericCharacters = 2;
        string password = "n0num3ric";

        // Act
        bool result = _userModelValidator.ContainsNumericCharacters(
            password,
            minimumNumericCharacters
        );

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsSpecialCharacters_NoSpecialCharacters_ShouldReturnFalse()
    {
        // Arrange
        int minimumSpecialCharacters = 2;
        string password = "nonumeric";

        // Act
        bool result = _userModelValidator.ContainsSpecialCharacters(
            password,
            minimumSpecialCharacters
        );

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsSpecialCharacters_HasSpecialCharacters_ShouldReturnTrue()
    {
        // Arrange
        int minimumSpecialCharacters = 2;
        string password = "n@num$ric";

        // Act
        bool result = _userModelValidator.ContainsSpecialCharacters(
            password,
            minimumSpecialCharacters
        );

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsUppercaseAndLowercaseCharacters_NoUppercaseAndLowercaseCharacters_ShouldReturnFalse()
    {
        // Arrange
        string password = "nonumeric";

        // Act
        bool result = _userModelValidator.ContainsUppercaseAndLowercaseCharacters(password);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ContainsUppercaseAndLowercaseCharacters_HasUppercaseAndLowercaseCharacters_ShouldReturnTrue()
    {
        // Arrange
        string password = "NoNumeric";

        // Act
        bool result = _userModelValidator.ContainsUppercaseAndLowercaseCharacters(password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UserModelValidator_InvalidUsername_ShouldNotValidate_Async()
    {
        // Arrange
        UserModel userModel = new() { Username = "test", Password = "V@lidPassw0rd" };

        // Act
        ValidationResult result = await _userModelValidator.ValidateAsync(userModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorMessage.Should().Be(UserModelValidator.USERNAME_VALID_EMAIL);
    }

    [Fact]
    public async Task UserModelValidator_InvalidPassword_ShouldNotValidate_Async()
    {
        // Arrange
        UserModel userModel = new() { Username = "toto@toto.fr", Password = "invalid" };

        // Act
        ValidationResult result = await _userModelValidator.ValidateAsync(userModel);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorMessage.Should().Be(UserModelValidator.WEAK_PASSWORD);
    }

    [Fact]
    public async Task UserModelValidator_Valid_ShouldValidate_Async()
    {
        // Arrange
        UserModel userModel = new() { Username = "toto@toto.fr", Password = "V@lidPassw0rd" };

        // Act
        ValidationResult result = await _userModelValidator.ValidateAsync(userModel);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
