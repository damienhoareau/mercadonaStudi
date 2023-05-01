using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Models;
using Mercadona.Backend.Options;
using Mercadona.Tests.Extensions;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
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
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(),
                    mockJWTOptions.Object,
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
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockJWTOptions.Object,
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
        public async Task LoginAsync_ValidPassword_ShouldReturnCorrectLoginResult()
        {
            // Arrange
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
            mockJWTOptions
                .SetupGet(_ => _.Value)
                .Returns(
                    new JWTOptions
                    {
                        ValidAudience = "https://localhost:44387",
                        ValidIssuer = "https://localhost:44387",
                        Secret = "JWTAuthenticationHIGHsecuredPasswordVVVp1OH7XzyrForTest"
                    }
                );
            Mock<IValidator<UserModel>> mockUserModelValidator = TestsHelper.GetServiceMock<
                IValidator<UserModel>
            >();
            AuthenticateController controller =
                TestsHelper.CreateController<AuthenticateController>(
                    new UserManagerMock(
                        false,
                        new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                    ),
                    mockJWTOptions.Object,
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
                    response.Value.Should().BeOfType<LoginResult>();
                    LoginResult loginResult = (LoginResult)response.Value;
                    JwtSecurityTokenHandler tokenHandler = new();
                    tokenHandler.ValidateToken(
                        loginResult.Token,
                        new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidAudience = mockJWTOptions.Object.Value.ValidAudience,
                            ValidIssuer = mockJWTOptions.Object.Value.ValidIssuer,
                            IssuerSigningKey =
                                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(mockJWTOptions.Object.Value.Secret)
                                )
                        },
                        out Microsoft.IdentityModel.Tokens.SecurityToken token
                    );
                    loginResult.Expiration.Should().Be(token.ValidTo);
                });
        }

        [Fact]
        public async Task RegisterAsync_ValidationException_ShouldReturnProblem_BadRequest()
        {
            // Arrange
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
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
                    mockJWTOptions.Object,
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
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
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
                    mockJWTOptions.Object,
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
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
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
                    mockJWTOptions.Object,
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
            Mock<IOptions<JWTOptions>> mockJWTOptions = TestsHelper.GetServiceMock<
                IOptions<JWTOptions>
            >();
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
                    mockJWTOptions.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.RegisterAsync(
                new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
            );

            // Assert
            result.Should().BeActionResult<OkResult>();
        }
    }
}
