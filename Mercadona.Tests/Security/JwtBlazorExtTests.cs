using FluentAssertions;
using HttpContextMoq;
using HttpContextMoq.Extensions;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mercadona.Tests.Security;

public class JwtBlazorExtTests
{
    [Fact]
    public void AddJwtBlazor_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        ServiceCollection services = new();
        SessionOptions sessionOptions = new();

        // Act
        services.AddJwtBlazor();
        ServiceProvider provider = services.BuildServiceProvider();
        IConfigureOptions<SessionOptions> configureOptions = provider.GetRequiredService<
            IConfigureOptions<SessionOptions>
        >();
        configureOptions.Configure(sessionOptions);

        // Assert
        services.Should().Contain(_ => _.ServiceType == typeof(IConfigureOptions<SessionOptions>));
        services.Should().Contain(_ => _.ServiceType == typeof(ISessionStore));
        services.Should().Contain(_ => _.ServiceType == typeof(IHttpContextAccessor));
        services.Should().Contain(_ => _.ServiceType == typeof(IWhiteList));
        services
            .Should()
            .Contain(_ => _.ServiceType == typeof(AuthAutoValidateAntiforgeryTokenFilter));
        sessionOptions.Cookie.HttpOnly.Should().BeTrue();
        sessionOptions.Cookie.SameSite.Should().Be(SameSiteMode.Strict);
        sessionOptions.Cookie.SecurePolicy.Should().Be(CookieSecurePolicy.SameAsRequest);
    }

    [Fact]
    public void UseJwtBlazor_ShouldUseAllMiddleware()
    {
        // Arrange
        Mock<IApplicationBuilder> mockAppBuilder = new();

        // Act
        mockAppBuilder.Object.UseJwtBlazor();

        // Assert
        mockAppBuilder.Verify();
    }

    [Fact]
    public async Task JwtBlazorMiddleware_NoHeaderAuthorization_ShouldExecuteNext_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        HttpContextMock httpContext = new();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Verify();
    }

    [Fact]
    public async Task JwtBlazorMiddleware_NotBearerAuthorization_ShouldExecuteNext_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        HttpContextMock httpContext = new HttpContextMock().SetupRequestHeaders(
            new Dictionary<string, StringValues>() { { "Authorization", "Basic accessToken" } }
        );
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Verify();
    }

    [Fact]
    public async Task JwtBlazorMiddleware_RefreshTokenIsNull_ShouldReturnStatus401Unauthorized_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        HttpContextMock httpContext = new HttpContextMock()
            .SetupRequestHeaders(
                new Dictionary<string, StringValues>() { { "Authorization", "Bearer accessToken" } }
            )
            .SetupSession();
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Invocations.Should().BeEmpty();
        httpContext.ResponseMock.Mock.VerifySet(
            r => r.StatusCode = StatusCodes.Status401Unauthorized
        );
    }

    [Fact]
    public async Task JwtBlazorMiddleware_RefreshTokenNotAuthorized_ShouldReturnStatus401Unauthorized_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
        mockMemoryCacheOptions
            .SetupGet(_ => _.Value)
            .Returns(new MemoryCacheOptions())
            .Verifiable();
        IWhiteList whiteList = new Mock<WhiteList>(mockMemoryCacheOptions.Object).Object;
        HttpContextMock httpContext = new HttpContextMock()
            .SetupRequestHeaders(
                new Dictionary<string, StringValues>() { { "Authorization", "Bearer accessToken" } }
            )
            .SetupSessionMoq()
            .SetupRequestService(whiteList);
        httpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, "refreshToken");
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Invocations.Should().BeEmpty();
        mockMemoryCacheOptions.Verify();
        httpContext.ResponseMock.Mock.VerifySet(
            r => r.StatusCode = StatusCodes.Status401Unauthorized
        );
    }

    [Fact]
    public async Task JwtBlazorMiddleware_RefreshTokenNotCorrespond_ShouldReturnStatus401Unauthorized_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        JwtSecurityToken token =
            new(
                issuer: "https://localhost:44387",
                audience: "https://localhost:44387",
                expires: DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION),
                claims: new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, "refreshToken"),
                    new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
                },
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            "JWTAuthenticationHIGHsecuredPasswordVVVp1OH7XzyrForTest"
                        )
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            );
        Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
        mockMemoryCacheOptions
            .SetupGet(_ => _.Value)
            .Returns(new MemoryCacheOptions())
            .Verifiable();
        IWhiteList whiteList = new Mock<WhiteList>(mockMemoryCacheOptions.Object).Object;
        whiteList.Set("refreshToken", token);
        Mock<ITokenService> mockTokenService = new();
        mockTokenService
            .Setup(_ => _.GetPrincipalFromToken(It.IsAny<string>()))
            .Returns(
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new List<Claim>()
                        {
                            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Name, "toto@toto.fr"),
                            new Claim(JwtRegisteredClaimNames.Jti, "wrongRefreshToken"),
                            new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
                        }
                    )
                )
            )
            .Verifiable();
        HttpContextMock httpContext = new HttpContextMock()
            .SetupRequestHeaders(
                new Dictionary<string, StringValues>() { { "Authorization", "Bearer accessToken" } }
            )
            .SetupSessionMoq()
            .SetupRequestService(whiteList)
            .SetupRequestService(mockTokenService.Object);
        httpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, "refreshToken");
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Invocations.Should().BeEmpty();
        mockMemoryCacheOptions.Verify();
        mockTokenService.Verify();
        httpContext.ResponseMock.Mock.VerifySet(
            r => r.StatusCode = StatusCodes.Status401Unauthorized
        );
    }

    [Fact]
    public async Task JwtBlazorMiddleware_RefreshTokenCorresponds_ShouldExecuteNext_Async()
    {
        // Arrange
        JwtBlazorMiddleware middleware = new();
        JwtSecurityToken token =
            new(
                issuer: "https://localhost:44387",
                audience: "https://localhost:44387",
                expires: DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION),
                claims: new List<Claim>()
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, "refreshToken"),
                    new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
                },
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            "JWTAuthenticationHIGHsecuredPasswordVVVp1OH7XzyrForTest"
                        )
                    ),
                    SecurityAlgorithms.HmacSha256
                )
            );
        Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
        mockMemoryCacheOptions
            .SetupGet(_ => _.Value)
            .Returns(new MemoryCacheOptions())
            .Verifiable();
        IWhiteList whiteList = new Mock<WhiteList>(mockMemoryCacheOptions.Object).Object;
        whiteList.Set("refreshToken", token);
        Mock<ITokenService> mockTokenService = new();
        mockTokenService
            .Setup(_ => _.GetPrincipalFromToken(It.IsAny<string>()))
            .Returns(
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        new List<Claim>()
                        {
                            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                            new Claim(ClaimTypes.Name, "toto@toto.fr"),
                            new Claim(JwtRegisteredClaimNames.Jti, "refreshToken"),
                            new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
                        }
                    )
                )
            )
            .Verifiable();
        HttpContextMock httpContext = new HttpContextMock()
            .SetupRequestHeaders(
                new Dictionary<string, StringValues>() { { "Authorization", "Bearer accessToken" } }
            )
            .SetupSessionMoq()
            .SetupRequestService(whiteList)
            .SetupRequestService(mockTokenService.Object);
        httpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, "refreshToken");
        Mock<RequestDelegate> mockRequestDelegate = new();
        mockRequestDelegate.Setup(_ => _.Invoke(It.IsAny<HttpContext>())).Verifiable();

        // Act
        await middleware.InvokeAsync(httpContext, mockRequestDelegate.Object);

        // Assert
        mockRequestDelegate.Verify();
        mockMemoryCacheOptions.Verify();
        mockTokenService.Verify();
    }
}
