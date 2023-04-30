using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Options;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Mercadona.Tests.Controllers
{
    public class AuthenticateControllerTests
    {
        public AuthenticateControllerTests() { }

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
                    UserManagerMock.GetUserManager<IdentityUser>(),
                    mockJWTOptions.Object,
                    mockUserModelValidator.Object
                );

            // Act
            IActionResult result = await controller.RegisterAsync(new UserModel());

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
        }
    }
}
