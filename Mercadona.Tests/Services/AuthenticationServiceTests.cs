using FluentAssertions;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Fixtures;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace Mercadona.Tests.Services;

public class AuthenticationServiceTests : IClassFixture<ServiceProviderFixture>
{
    private readonly ServiceProviderFixture _fixture;

    public AuthenticationServiceTests(ServiceProviderFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reconfigure(services =>
        {
            services.AddMemoryCache();
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                // Adding Jwt Bearer
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = true;
                    options.TokenValidationParameters = TestsHelper.TokenValidationParameters;
                });
            services.AddSingleton<ITokenService, TokenService>();

            return services;
        });
    }

    [Fact]
    public async Task FindUserByNameAsync_UserDoesNotExist_ShoudReturnNull_Async()
    {
        // Arrange
        Mock<ITokenService> mockTokenService = new();
        AuthenticationService service = new(new UserManagerMock(), mockTokenService.Object);

        // Act
        IdentityUser? result = await service.FindUserByNameAsync("toto@toto.fr");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindUserByNameAsync_UserExist_ShoudReturnUser_Async()
    {
        // Arrange
        Mock<ITokenService> mockTokenService = new();
        AuthenticationService service =
            new(
                new UserManagerMock(
                    false,
                    new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" }
                ),
                mockTokenService.Object
            );

        // Act
        IdentityUser? result = await service.FindUserByNameAsync("toto@toto.fr");

        // Assert
        result.Should().NotBeNull();
        result!.UserName.Should().Be("toto@toto.fr");
    }

    [Fact]
    public async Task CheckPasswordAsync_WrongPassword_ShoudReturnFalse_Async()
    {
        // Arrange
        UserManagerMock userManagerMock =
            new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
        Mock<ITokenService> mockTokenService = new();
        AuthenticationService service = new(userManagerMock, mockTokenService.Object);
        IdentityUser? user = await userManagerMock.FindByNameAsync("toto@toto.fr");
        if (user == null)
            Assert.Fail("L'utilisateur toto@toto.fr non trouvé.");

        // Act
        bool result = await service.CheckPasswordAsync(user, "badPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CheckPasswordAsync_SamePassord_ShoudReturnTrue_Async()
    {
        // Arrange
        UserManagerMock userManagerMock =
            new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
        Mock<ITokenService> mockTokenService = new();
        AuthenticationService service = new(userManagerMock, mockTokenService.Object);
        IdentityUser? user = await userManagerMock.FindByNameAsync("toto@toto.fr");
        if (user == null)
            Assert.Fail("L'utilisateur toto@toto.fr non trouvé.");

        // Act
        bool result = await service.CheckPasswordAsync(user, "V@lidPassw0rd");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_ValidPassword_ShouldReturnCorrectAccessToken_Async()
    {
        // Arrange
        UserManagerMock userManagerMock =
            new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
        Mock<ITokenService> mockTokenService = new();
        mockTokenService.Setup(_ => _.GenerateRefreshToken()).Returns(Guid.Empty.ToString());
        mockTokenService
            .Setup(_ => _.GenerateAccessToken(It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>()))
            .Returns(Guid.Empty.ToString());
        AuthenticationService service = new(userManagerMock, mockTokenService.Object);
        IdentityUser? user = await userManagerMock.FindByNameAsync("toto@toto.fr");
        if (user == null)
            Assert.Fail("L'utilisateur toto@toto.fr non trouvé.");

        // Act
        (string refreshToken, string accessToken) = await service.LoginAsync(user);

        // Assert
        refreshToken.Should().Be(Guid.Empty.ToString());
        accessToken.Should().Be(Guid.Empty.ToString());
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldCall_TokenService_RefreshToken_Async()
    {
        // Arrange
        UserManagerMock userManagerMock =
            new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
        Mock<ITokenService> mockTokenService = new();
        mockTokenService
            .Setup(_ => _.RefreshToken(It.IsAny<string>()))
            .Returns("newAccessToken")
            .Verifiable();
        AuthenticationService service = new(userManagerMock, mockTokenService.Object);

        // Act
        string accessToken = await service.RefreshTokenAsync("refreshToken");

        // Assert
        mockTokenService.Verify();
        accessToken.Should().Be("newAccessToken");
    }

    [Fact]
    public async Task RegisterAsync_CreateFailed_ShouldReturnProblem_InternalServerError_Async()
    {
        // Arrange
        Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
        AuthenticationService service = new(new UserManagerMock(true), mockTokenService.Object);

        // Act
        bool result = await service.RegisterAsync("toto@toto.fr", "V@lidPassw0rd");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task RegisterAsync_ValidUser_ShouldReturnOK_Async()
    {
        // Arrange
        Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
        AuthenticationService service = new(new UserManagerMock(), mockTokenService.Object);

        // Act
        bool result = await service.RegisterAsync("toto@toto.fr", "V@lidPassw0rd");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task LogoutAsync_ShouldCall_ITokenService_RevokeRefreshToken_Async()
    {
        // Arrange
        Mock<ITokenService> mockTokenService = TestsHelper.GetServiceMock<ITokenService>();
        mockTokenService.Setup(_ => _.RevokeRefreshToken(It.IsAny<string>())).Verifiable();
        AuthenticationService service = new(new UserManagerMock(), mockTokenService.Object);

        // Act
        await service.LogoutAsync("refreshToken");

        // Assert
        mockTokenService.Verify();
    }
}
