using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Mercadona.Tests.Controllers;

public class AuthenticateControllerTests
{
    [Fact]
    public async Task LoginAsync_NotValidUserModel_ShouldReturnUnauthorized_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        mockUserModelValidator
            .Setup(_ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure() }))
            .Verifiable();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.LoginAsync(
            new UserModel { Username = "toto@toto.fr", Password = "notValid" }
        );

        // Assert
        mockUserModelValidator.Verify();
        result.Should().BeActionResult<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LoginAsync_ServiceNullTokens_ShouldReturnUnauthorized_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService
            .Setup(_ => _.LoginAsync(It.IsAny<UserModel>()))
            .ReturnsAsync((null, null))
            .Verifiable();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        mockUserModelValidator
            .Setup(_ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult())
            .Verifiable();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.LoginAsync(
            new UserModel { Username = "toto@toto.fr", Password = "WrongPassw0rd" }
        );

        // Assert
        mockAuthenticationService.Verify();
        mockUserModelValidator.Verify();
        result.Should().BeActionResult<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task LoginAsync_ServiceSendTokens_ShouldReturnCorrectAccessToken_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        string expectedRefreshToken = Guid.NewGuid().ToString();
        string expectedAccessToken = Guid.NewGuid().ToString();
        mockAuthenticationService
            .Setup(_ => _.LoginAsync(It.IsAny<UserModel>()))
            .ReturnsAsync((expectedRefreshToken, expectedAccessToken))
            .Verifiable();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        mockUserModelValidator
            .Setup(_ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult())
            .Verifiable();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.LoginAsync(
            new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
        );

        // Assert
        mockAuthenticationService.Verify();
        mockUserModelValidator.Verify();
        result
            .Should()
            .BeActionResult<OkObjectResult>(response =>
            {
                response.Value.Should().BeOfType<ConnectedUser>();
                ConnectedUser connectedUser = (ConnectedUser)response.Value!;
                connectedUser.UserName.Should().Be("toto@toto.fr");
                connectedUser.RefreshToken.Should().Be(expectedRefreshToken);
                connectedUser.AccessToken.Should().Be(expectedAccessToken);
            });
    }

    [Fact]
    public async Task RefreshTokenAsync_RefreshTokenNotFound_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.RefreshTokenAsync();

        // Assert
        result
            .Should()
            .BeProblemResult(
                StatusCodes.Status500InternalServerError,
                AuthenticateController.REFRESH_TOKEN_NOT_FOUND
            );
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldStoreNewAccessToken_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService
            .Setup(_ => _.RefreshTokenAsync(It.IsAny<string>()))
            .ReturnsAsync("newAccessToken");
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );
        controller.HttpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, "refreshToken");
        controller.HttpContext.Session.SetString(TokenService.ACCESS_TOKEN_NAME, "accessToken");

        // Act
        controller.HttpContext.Session
            .GetString(TokenService.REFRESH_TOKEN_NAME)
            .Should()
            .Be("refreshToken");
        controller.HttpContext.Session
            .GetString(TokenService.ACCESS_TOKEN_NAME)
            .Should()
            .Be("accessToken");
        IActionResult result = await controller.RefreshTokenAsync();

        // Assert
        controller.HttpContext.Session
            .GetString(TokenService.ACCESS_TOKEN_NAME)
            .Should()
            .Be("newAccessToken");
        result
            .Should()
            .BeActionResult<OkObjectResult>(response =>
            {
                response.Value.Should().BeOfType<TextResponse>();
                TextResponse textResponse = (TextResponse)response.Value!;
                textResponse.Text.Should().Be("newAccessToken");
            });
    }

    [Fact]
    public async Task RegisterAsync_ValidationException_ShouldReturnProblem_BadRequest_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >(
            mock =>
                mock.Setup(
                        _ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(
                        new ValidationResult(
                            new[] { new ValidationFailure(nameof(UserModel.Username), "Test") }
                        )
                    )
        );
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.RegisterAsync(new UserModel());

        // Assert
        result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
    }

    [Fact]
    public async Task RegisterAsync_UserAlreadyExists_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService
            .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new IdentityUser())
            .Verifiable();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >(
            mock =>
                mock.Setup(
                        _ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(new ValidationResult())
        );
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.RegisterAsync(
            new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
        );

        // Assert
        mockAuthenticationService.Verify();
        result
            .Should()
            .BeProblemResult(
                StatusCodes.Status500InternalServerError,
                AuthenticateController.USER_ALREADY_EXISTS("toto@toto.fr")
            );
    }

    [Fact]
    public async Task RegisterAsync_CreateFailed_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >(
            mock =>
                mock.Setup(
                        _ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(new ValidationResult(Array.Empty<ValidationFailure>()))
        );
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.RegisterAsync(
            new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
        );

        // Assert
        result
            .Should()
            .BeProblemResult(
                StatusCodes.Status500InternalServerError,
                AuthenticateController.USER_CREATION_FAILED
            );
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ShouldReturnOK_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService
            .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((IdentityUser?)null)
            .Verifiable();
        mockAuthenticationService
            .Setup(_ => _.RegisterAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true)
            .Verifiable();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >(
            mock =>
                mock.Setup(
                        _ => _.ValidateAsync(It.IsAny<UserModel>(), It.IsAny<CancellationToken>())
                    )
                    .ReturnsAsync(new ValidationResult())
        );
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.RegisterAsync(
            new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
        );

        // Assert
        mockAuthenticationService.Verify();
        result.Should().BeActionResult<OkResult>();
    }

    [Fact]
    public async Task LogoutAsync_NotConnected_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );

        // Act
        IActionResult result = await controller.LogoutAsync();

        // Assert
        result
            .Should()
            .BeProblemResult(
                StatusCodes.Status500InternalServerError,
                AuthenticateController.REFRESH_TOKEN_NOT_FOUND
            );
    }

    [Fact]
    public async Task LogoutAsync_Exception_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService
            .Setup(_ => _.LogoutAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test"));
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );
        controller.ControllerContext.HttpContext.Session.SetString(
            TokenService.REFRESH_TOKEN_NAME,
            Guid.Empty.ToString()
        );

        // Act
        IActionResult result = await controller.LogoutAsync();

        // Assert
        result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
    }

    [Fact]
    public async Task LogoutAsync_Success_ShouldReturnOK_Async()
    {
        // Arrange
        Mock<IAuthenticationService> mockAuthenticationService =
            TestsHelper.GetServiceMock<IAuthenticationService>();
        mockAuthenticationService.Setup(_ => _.LogoutAsync(It.IsAny<string>())).Verifiable();
        Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
            IValidator<UserModel>
        >();
        AuthenticateController controller = TestsHelper.CreateController<AuthenticateController>(
            mockAuthenticationService.Object,
            mockUserModelValidator.Object
        );
        controller.ControllerContext.HttpContext.Session.SetString(
            TokenService.REFRESH_TOKEN_NAME,
            Guid.Empty.ToString()
        );

        // Act
        IActionResult result = await controller.LogoutAsync();

        // Assert
        mockAuthenticationService.Verify();
        result.Should().BeActionResult<OkResult>();
        controller.ControllerContext.HttpContext.Session
            .TryGetValue(TokenService.REFRESH_TOKEN_NAME, out _)
            .Should()
            .BeFalse();
    }
}
