using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mercadona.Tests.Controllers
{
    public class AuthenticateControllerTests
    {
        public AuthenticateControllerTests() { }

        [Fact]
        public async Task LoginAsync_UserDoesNotExist_ShouldReturnUnauthorized()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.LoginAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );

            // Assert
            result.Should().BeActionResult<UnauthorizedResult>();
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ShouldReturnUnauthorized()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.LoginAsync(
                new UserModel { Username = "toto@toto.fr", Password = "WrongPassw0rd" }
            );

            // Assert
            result.Should().BeActionResult<UnauthorizedResult>();
        }

        [Fact]
        public async Task LoginAsync_ValidPassword_ShouldReturnCorrectAccessToken()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            mockTokenService.Setup(_ => _.GenerateRefreshToken()).Returns("RefreshToken");
            mockTokenService
                .Setup(
                    _ => _.GenerateAccessToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>())
                )
                .Returns("RefreshToken");
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.LoginAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );

            // Assert
            result
                .Should()
                .BeActionResult<OkObjectResult>(response =>
                {
                    response.Value.Should().BeOfType<string>();
                });
        }

        [Fact]
        public async Task RegisterAsync_ValidationException_ShouldReturnProblem_BadRequest()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >(
                mock =>
                    mock.Setup(
                            _ =>
                                _.ValidateAsync(
                                    It.IsAny<UserModel>(),
                                    It.IsAny<CancellationToken>()
                                )
                        )
                        .ReturnsAsync(
                            new ValidationResult(
                                new[] { new ValidationFailure(nameof(UserModel.Username), "Test") }
                            )
                        )
            );
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.RegisterAsync(new UserModel());

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
        }

        [Fact]
        public async Task RegisterAsync_UserAlreadyExists_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >(
                mock =>
                    mock.Setup(
                            _ =>
                                _.ValidateAsync(
                                    It.IsAny<UserModel>(),
                                    It.IsAny<CancellationToken>()
                                )
                        )
                        .ReturnsAsync(new ValidationResult())
            );
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel() { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
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
                    AuthenticateController.USER_ALREADY_EXISTS("toto@toto.fr")
                );
        }

        [Fact]
        public async Task RegisterAsync_CreateFailed_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >(
                mock =>
                    mock.Setup(
                            _ =>
                                _.ValidateAsync(
                                    It.IsAny<UserModel>(),
                                    It.IsAny<CancellationToken>()
                                )
                        )
                        .ReturnsAsync(new ValidationResult(Array.Empty<ValidationFailure>()))
            );
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(true),
                    mockTokenService.Object,
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
        public async Task RegisterAsync_ValidUser_ShouldReturnOK()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >(
                mock =>
                    mock.Setup(
                            _ =>
                                _.ValidateAsync(
                                    It.IsAny<UserModel>(),
                                    It.IsAny<CancellationToken>()
                                )
                        )
                        .ReturnsAsync(new ValidationResult())
            );
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.RegisterAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );

            // Assert
            result.Should().BeActionResult<OkResult>();
        }

        [Fact]
        public async Task LogoutAsync_NotConnected_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.LogoutAsync();

            // Assert
            result
                .Should()
                .BeProblemResult(
                    StatusCodes.Status500InternalServerError,
                    AuthenticateController.TOKEN_NOT_FOUND
                );
        }

        [Fact]
        public async Task LogoutAsync_Exception_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            mockTokenService.Setup(_ => _.GenerateRefreshToken()).Returns("RefreshToken");
            mockTokenService
                .Setup(
                    _ => _.GenerateAccessToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>())
                )
                .Returns("RefreshToken");
            mockTokenService
                .Setup(_ => _.RevokeRefreshToken(It.IsAny<string>()))
                .Throws(new Exception("Test"));
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            await controller.LoginAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );
            IActionResult result = await controller.LogoutAsync();

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task LogoutAsync_Success_ShouldReturnOK()
        {
            // Arrange
            Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
            mockTokenService.Setup(_ => _.GenerateRefreshToken()).Returns("RefreshToken");
            mockTokenService
                .Setup(
                    _ => _.GenerateAccessToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>())
                )
                .Returns("RefreshToken");
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockTokenService.Object,
                    mockUserModelValidator.Object
                );

            // Act
            await controller.LoginAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );
            IActionResult result = await controller.LogoutAsync();

            // Assert
            result.Should().BeActionResult<OkResult>();
            controller.ControllerContext.HttpContext.Session
                .TryGetValue(TokenService.REFRESH_TOKEN_NAME, out _)
                .Should()
                .BeFalse();
        }
    }
}
