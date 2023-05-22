using FluentAssertions;
using HttpContextMoq;
using HttpContextMoq.Extensions;
using Mercadona.Backend.Areas.Identity;
using Mercadona.Backend.Models;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace Mercadona.Tests.Security
{
    public class RevalidatingIdentityAuthenticationStateProviderTests : IAsyncLifetime
    {
        private readonly IdentityOptions _identityOptions = new();
        private readonly Mock<IOptions<IdentityOptions>> _mockIdentityOptions;
        private readonly MemoryCacheOptions _memoryCacheOptions = new();
        private readonly WhiteList _whiteList;

        public RevalidatingIdentityAuthenticationStateProviderTests()
        {
            _mockIdentityOptions = new();
            _mockIdentityOptions.SetupGet(_ => _.Value).Returns(_identityOptions);

            Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
            mockMemoryCacheOptions.SetupGet(_ => _.Value).Returns(_memoryCacheOptions);

            _whiteList = new(mockMemoryCacheOptions.Object);
        }

        public Task InitializeAsync()
        {
            return Task.Run(_whiteList.Clear);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        private IHttpContextAccessor CreateHttpContextAccessor(
            bool setRefreshToken = false,
            bool setAccessToken = false
        )
        {
            HttpContextMock httpContext = new HttpContextMock()
                .SetupSessionMoq()
                .SetupRequestService(_whiteList);

            if (setRefreshToken)
                httpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, "refreshToken");
            if (setAccessToken)
                httpContext.Session.SetString(TokenService.ACCESS_TOKEN_NAME, "accessToken");

            Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
            mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(httpContext);

            return mockHttpContextAccessor.Object;
        }

        private static JwtSecurityToken GetJwtSecurityToken(bool badToken = false)
        {
            return new(
                issuer: TestsHelper.TokenValidationParameters.ValidIssuer,
                audience: TestsHelper.TokenValidationParameters.ValidAudience,
                expires: DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION),
                claims: new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(
                        JwtRegisteredClaimNames.Jti,
                        badToken ? "wrongRefreshToken" : "refreshToken"
                    ),
                    new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
                },
                signingCredentials: new SigningCredentials(
                    TestsHelper.TokenValidationParameters.IssuerSigningKey,
                    SecurityAlgorithms.HmacSha256
                )
            );
        }

        [Fact]
        public async void GetAuthenticationStateAsync_NoRefreshToken_ShouldReturnAnonymous()
        {
            // Arrange
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_NoAccessToken_ShouldReturnAnonymous()
        {
            // Arrange
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_Exception_ShouldReturnAnonymous()
        {
            // Arrange
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ITokenService> mockTokenService = new();
            mockTokenService
                .Setup(_ => _.ValidateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Throws(new Exception());
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true, true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    mockTokenService.Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            mockTokenService.Verify();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_UserDoesNotExist_ShouldReturnAnonymous()
        {
            // Arrange
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ITokenService> mockTokenService = new();
            mockTokenService
                .Setup(_ => _.ValidateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new ClaimsPrincipal())
                .Verifiable();
            Mock<IAuthenticationService> mockAuthenticationService = new();
            mockAuthenticationService
                .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((IdentityUser?)null)
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true, true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    mockTokenService.Object,
                    mockAuthenticationService.Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            mockTokenService.Verify();
            mockAuthenticationService.Verify();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_RefreshTokenNotAuthorized_ShouldReturnAnonymous()
        {
            // Arrange
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ITokenService> mockTokenService = new();
            mockTokenService
                .Setup(_ => _.ValidateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new ClaimsPrincipal())
                .Verifiable();
            Mock<IAuthenticationService> mockAuthenticationService = new();
            mockAuthenticationService
                .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new IdentityUser())
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true, true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    mockTokenService.Object,
                    mockAuthenticationService.Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            mockTokenService.Verify();
            mockAuthenticationService.Verify();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_RefreshTokenNotCorrespondToAccessToken_ShouldReturnAnonymous()
        {
            // Arrange
            _whiteList.Set("refreshToken", GetJwtSecurityToken(true));
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ITokenService> mockTokenService = new();
            mockTokenService
                .Setup(_ => _.ValidateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(new ClaimsPrincipal())
                .Verifiable();
            Mock<IAuthenticationService> mockAuthenticationService = new();
            mockAuthenticationService
                .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new IdentityUser())
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true, true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    mockTokenService.Object,
                    mockAuthenticationService.Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            mockTokenService.Verify();
            mockAuthenticationService.Verify();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async void GetAuthenticationStateAsync_ShouldReturnAuthenticated()
        {
            // Arrange
            JwtSecurityToken token = GetJwtSecurityToken();
            _whiteList.Set("refreshToken", token);
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ITokenService> mockTokenService = new();
            mockTokenService
                .Setup(_ => _.ValidateToken(It.IsAny<string>(), It.IsAny<bool>()))
                .Returns(
                    new JwtSecurityTokenHandler().ValidateToken(
                        new JwtSecurityTokenHandler().WriteToken(token),
                        TestsHelper.TokenValidationParameters,
                        out _
                    )
                )
                .Verifiable();
            Mock<IAuthenticationService> mockAuthenticationService = new();
            mockAuthenticationService
                .Setup(_ => _.FindUserByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(new IdentityUser())
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    new Mock<IServiceScopeFactory>().Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(true, true),
                    new Mock<ISecurityStampValidator<IdentityUser>>().Object,
                    mockTokenLifetimeValidator.Object,
                    mockTokenService.Object,
                    mockAuthenticationService.Object,
                    _whiteList
                );

            // Act
            AuthenticationState authState = await authStateProvider.GetAuthenticationStateAsync();

            // Assert
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            mockTokenService.Verify();
            mockAuthenticationService.Verify();
            authState.User.Identity.Should().NotBeNull();
            authState.User.Identity?.IsAuthenticated.Should().BeTrue();
            authState.User.Identity?.Name.Should().Be("toto@toto.fr");
        }

        [Fact]
        public async void ValidateAuthenticationStateAsync_Exception_ShouldReturnFalse()
        {
            // Arrange
            Mock<IServiceScope> mockSyncScope = new();
            mockSyncScope.Setup(_ => _.Dispose()).Verifiable();
            Mock<IServiceScopeFactory> mockServiceScopeFactory = new();
            mockServiceScopeFactory
                .Setup(_ => _.CreateScope())
                .Returns(mockSyncScope.Object)
                .Verifiable();
            JwtSecurityToken token = GetJwtSecurityToken();
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ISecurityStampValidator<IdentityUser>> mockSecurityStampValidator = new();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    mockServiceScopeFactory.Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    mockSecurityStampValidator.Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );

            // Act
            Task<bool>? task = (Task<bool>?)
                typeof(RevalidatingServerAuthenticationStateProvider).InvokeMember(
                    "ValidateAuthenticationStateAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    authStateProvider,
                    new object?[] { new AuthenticationState(principal), CancellationToken.None }
                );
            if (task == null)
                Assert.Fail("Méthode ValidateAuthenticationStateAsync non trouvée.");
            bool result = await task;

            // Assert
            mockServiceScopeFactory.Verify();
            mockSyncScope.Verify();
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            result.Should().BeFalse();
        }

        public interface IAsyncServiceScope : IServiceScope, IAsyncDisposable { }

        [Fact]
        public async void ValidateAuthenticationStateAsync_ExceptionWithAsyncScope_ShouldReturnFalse()
        {
            // Arrange
            Mock<IAsyncServiceScope> mockAsyncScope = new();
            mockAsyncScope.Setup(_ => _.DisposeAsync()).Verifiable();
            Mock<IServiceScopeFactory> mockServiceScopeFactory = new();
            mockServiceScopeFactory
                .Setup(_ => _.CreateScope())
                .Returns(mockAsyncScope.Object)
                .Verifiable();
            JwtSecurityToken token = GetJwtSecurityToken();
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ISecurityStampValidator<IdentityUser>> mockSecurityStampValidator = new();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    mockServiceScopeFactory.Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    mockSecurityStampValidator.Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );

            // Act
            Task<bool>? task = (Task<bool>?)
                typeof(RevalidatingServerAuthenticationStateProvider).InvokeMember(
                    "ValidateAuthenticationStateAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    authStateProvider,
                    new object?[] { new AuthenticationState(principal), CancellationToken.None }
                );
            if (task == null)
                Assert.Fail("Méthode ValidateAuthenticationStateAsync non trouvée.");
            bool result = await task;

            // Assert
            mockServiceScopeFactory.Verify();
            mockAsyncScope.Verify();
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            result.Should().BeFalse();
        }

        [Fact]
        public async void ValidateAuthenticationStateAsync_ValidateSecurityStampAsyncReturnsFalse_ShouldReturnFalse()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
            userManagerMock.SetSupportsUserSecurityStamp(true);
            Mock<IServiceProvider> mockServiceProvider = new();
            mockServiceProvider
                .Setup(_ => _.GetService(typeof(UserManager<IdentityUser>)))
                .Returns(userManagerMock);
            Mock<IServiceScope> mockSyncScope = new();
            mockSyncScope.Setup(_ => _.Dispose()).Verifiable();
            mockSyncScope
                .SetupGet(_ => _.ServiceProvider)
                .Returns(mockServiceProvider.Object)
                .Verifiable();
            Mock<IServiceScopeFactory> mockServiceScopeFactory = new();
            mockServiceScopeFactory
                .Setup(_ => _.CreateScope())
                .Returns(mockSyncScope.Object)
                .Verifiable();
            JwtSecurityToken token = GetJwtSecurityToken();
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            Mock<ISecurityStampValidator<IdentityUser>> mockSecurityStampValidator = new();
            mockSecurityStampValidator
                .Setup(
                    _ =>
                        _.ValidateSecurityStampAsync(
                            It.IsAny<UserManager<IdentityUser>>(),
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<IdentityOptions>()
                        )
                )
                .ReturnsAsync(false)
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    mockServiceScopeFactory.Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    mockSecurityStampValidator.Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );
            List<Claim> principalClaims = principal.Claims.ToList();
            principalClaims.Add(
                new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
            );

            // Act
            Task<bool>? task = (Task<bool>?)
                typeof(RevalidatingServerAuthenticationStateProvider).InvokeMember(
                    "ValidateAuthenticationStateAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    authStateProvider,
                    new object?[] { new AuthenticationState(principal), CancellationToken.None }
                );
            if (task == null)
                Assert.Fail("Méthode ValidateAuthenticationStateAsync non trouvée.");
            bool result = await task;

            // Assert
            mockServiceScopeFactory.Verify();
            mockSyncScope.Verify();
            mockSecurityStampValidator.Verify();
            mockTokenLifetimeValidator.Invocations.Should().BeEmpty();
            result.Should().BeFalse();
        }

        [Fact]
        public async void ValidateAuthenticationStateAsync_ValidateTokenLifetimeAsyncReturnsFalse_ShouldReturnFalse()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
            userManagerMock.SetSupportsUserSecurityStamp(true);
            Mock<IServiceProvider> mockServiceProvider = new();
            mockServiceProvider
                .Setup(_ => _.GetService(typeof(UserManager<IdentityUser>)))
                .Returns(userManagerMock);
            Mock<IServiceScope> mockSyncScope = new();
            mockSyncScope.Setup(_ => _.Dispose()).Verifiable();
            mockSyncScope
                .SetupGet(_ => _.ServiceProvider)
                .Returns(mockServiceProvider.Object)
                .Verifiable();
            Mock<IServiceScopeFactory> mockServiceScopeFactory = new();
            mockServiceScopeFactory
                .Setup(_ => _.CreateScope())
                .Returns(mockSyncScope.Object)
                .Verifiable();
            JwtSecurityToken token = GetJwtSecurityToken();
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            mockTokenLifetimeValidator
                .Setup(_ => _.ValidateTokenLifetimeAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(false)
                .Verifiable();
            Mock<ISecurityStampValidator<IdentityUser>> mockSecurityStampValidator = new();
            mockSecurityStampValidator
                .Setup(
                    _ =>
                        _.ValidateSecurityStampAsync(
                            It.IsAny<UserManager<IdentityUser>>(),
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<IdentityOptions>()
                        )
                )
                .ReturnsAsync(true)
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    mockServiceScopeFactory.Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    mockSecurityStampValidator.Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );
            List<Claim> principalClaims = principal.Claims.ToList();
            principalClaims.Add(
                new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
            );

            // Act
            Task<bool>? task = (Task<bool>?)
                typeof(RevalidatingServerAuthenticationStateProvider).InvokeMember(
                    "ValidateAuthenticationStateAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    authStateProvider,
                    new object?[] { new AuthenticationState(principal), CancellationToken.None }
                );
            if (task == null)
                Assert.Fail("Méthode ValidateAuthenticationStateAsync non trouvée.");
            bool result = await task;

            // Assert
            mockServiceScopeFactory.Verify();
            mockSyncScope.Verify();
            mockSecurityStampValidator.Verify();
            mockTokenLifetimeValidator.Verify();
            result.Should().BeFalse();
        }

        [Fact]
        public async void ValidateAuthenticationStateAsync_ValidateTokenLifetimeAsyncReturnsTrue_ShouldReturnTrue()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
            userManagerMock.SetSupportsUserSecurityStamp(true);
            Mock<IServiceProvider> mockServiceProvider = new();
            mockServiceProvider
                .Setup(_ => _.GetService(typeof(UserManager<IdentityUser>)))
                .Returns(userManagerMock);
            Mock<IServiceScope> mockSyncScope = new();
            mockSyncScope.Setup(_ => _.Dispose()).Verifiable();
            mockSyncScope
                .SetupGet(_ => _.ServiceProvider)
                .Returns(mockServiceProvider.Object)
                .Verifiable();
            Mock<IServiceScopeFactory> mockServiceScopeFactory = new();
            mockServiceScopeFactory
                .Setup(_ => _.CreateScope())
                .Returns(mockSyncScope.Object)
                .Verifiable();
            JwtSecurityToken token = GetJwtSecurityToken();
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
            Mock<ITokenLifetimeValidator> mockTokenLifetimeValidator = new();
            mockTokenLifetimeValidator
                .Setup(_ => _.ValidateTokenLifetimeAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(true)
                .Verifiable();
            Mock<ISecurityStampValidator<IdentityUser>> mockSecurityStampValidator = new();
            mockSecurityStampValidator
                .Setup(
                    _ =>
                        _.ValidateSecurityStampAsync(
                            It.IsAny<UserManager<IdentityUser>>(),
                            It.IsAny<ClaimsPrincipal>(),
                            It.IsAny<IdentityOptions>()
                        )
                )
                .ReturnsAsync(true)
                .Verifiable();
            RevalidatingIdentityAuthenticationStateProvider<IdentityUser> authStateProvider =
                new(
                    new Mock<ILoggerFactory>().Object,
                    mockServiceScopeFactory.Object,
                    _mockIdentityOptions.Object,
                    CreateHttpContextAccessor(),
                    mockSecurityStampValidator.Object,
                    mockTokenLifetimeValidator.Object,
                    new Mock<ITokenService>().Object,
                    new Mock<IAuthenticationService>().Object,
                    _whiteList
                );
            List<Claim> principalClaims = principal.Claims.ToList();
            principalClaims.Add(
                new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
            );

            // Act
            Task<bool>? task = (Task<bool>?)
                typeof(RevalidatingServerAuthenticationStateProvider).InvokeMember(
                    "ValidateAuthenticationStateAsync",
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    authStateProvider,
                    new object?[] { new AuthenticationState(principal), CancellationToken.None }
                );
            if (task == null)
                Assert.Fail("Méthode ValidateAuthenticationStateAsync non trouvée.");
            bool result = await task;

            // Assert
            mockServiceScopeFactory.Verify();
            mockSyncScope.Verify();
            mockSecurityStampValidator.Verify();
            mockTokenLifetimeValidator.Verify();
            result.Should().BeTrue();
        }
    }
}
