using FluentAssertions;
using Mercadona.Backend.Areas.Identity;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Xunit.Assert;

namespace Mercadona.Tests.Security;

public class TokenLifetimeValidatorTests : IAsyncLifetime
{
    private readonly ITokenLifetimeValidator _tokenLifetimeValidator;
    private readonly MemoryCacheOptions _memoryCacheOptions = new();
    private readonly IWhiteList _whiteList;

    public TokenLifetimeValidatorTests()
    {
        Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
        mockMemoryCacheOptions.SetupGet(_ => _.Value).Returns(_memoryCacheOptions);

        _whiteList = new Mock<WhiteList>(mockMemoryCacheOptions.Object).Object;

        _tokenLifetimeValidator = new TokenLifetimeValidator(_whiteList);
    }

    public Task InitializeAsync()
    {
        return Task.Run(_whiteList.Clear);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static JwtSecurityToken GetJwtSecurityToken(DateTime notBefore, DateTime expires)
    {
        return new(
            issuer: TestsHelper.TokenValidationParameters.ValidIssuer,
            audience: TestsHelper.TokenValidationParameters.ValidAudience,
            notBefore: notBefore,
            expires: expires,
            claims: new List<Claim>()
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, "toto@toto.fr"),
                new Claim(JwtRegisteredClaimNames.Jti, "refreshToken"),
                new Claim("AspNet.Identity.SecurityStamp", Guid.NewGuid().ToString())
            },
            signingCredentials: new SigningCredentials(
                TestsHelper.TokenValidationParameters.IssuerSigningKey,
                SecurityAlgorithms.HmacSha256
            )
        );
    }

    private static ClaimsPrincipal GetClaimsPrincipal(JwtSecurityToken token)
    {
        TokenValidationParameters tokenValidationParameters = TestsHelper.TokenValidationParameters;
        tokenValidationParameters.ValidateLifetime = false;
        return new JwtSecurityTokenHandler().ValidateToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            tokenValidationParameters,
            out _
        );
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_RefreshTokenDoesNotExist_ShouldReturnFalseAndRaiseLogoutEvent_Async()
    {
        // Arrange
        ClaimsPrincipal principal = new(new ClaimsIdentity());

        // Act
        bool result = false;
        RaisedEvent<TokenExpirationWarningChangedArgs> raisedEvent =
            await RaisesAsync<TokenExpirationWarningChangedArgs>(
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                async () =>
                    result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
            );

        // Assert
        result.Should().BeFalse();
        raisedEvent.Arguments.TokenExpirationWarningEnum
            .Should()
            .Be(TokenExpirationWarning.LogoutNeeded);
        raisedEvent.Arguments.ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_RefreshTokenNotInWhitelist_ShouldReturnFalseAndRaiseLogoutEvent_Async()
    {
        // Arrange
        JwtSecurityToken token = GetJwtSecurityToken(
            DateTime.Now,
            DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION)
        );
        ClaimsPrincipal principal = GetClaimsPrincipal(token);
        JwtSecurityToken otherToken = GetJwtSecurityToken(
            DateTime.Now,
            DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION)
        );
        _whiteList.Set(Guid.NewGuid().ToString(), otherToken);

        // Act
        bool result = false;
        RaisedEvent<TokenExpirationWarningChangedArgs> raisedEvent =
            await RaisesAsync<TokenExpirationWarningChangedArgs>(
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                async () =>
                    result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
            );

        // Assert
        result.Should().BeFalse();
        raisedEvent.Arguments.TokenExpirationWarningEnum
            .Should()
            .Be(TokenExpirationWarning.LogoutNeeded);
        raisedEvent.Arguments.ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_AccessTokenExpired_ShouldReturnFalseAndRaiseLogoutEvent_Async()
    {
        // Arrange
        JwtSecurityToken token = GetJwtSecurityToken(
            DateTime.Now.AddHours(-1),
            DateTime.Now.AddHours(-1).AddMinutes(TokenService.ACCESS_TOKEN_DURATION)
        );
        ClaimsPrincipal principal = GetClaimsPrincipal(token);
        // Le jeton étant expiré, il ne devrait pas être dans la liste blanche
        //_whiteList.Set(
        //    token.Claims.Single(_ => _.Type == JwtRegisteredClaimNames.Jti).Value,
        //    token
        //);

        // Act
        bool result = false;
        RaisedEvent<TokenExpirationWarningChangedArgs> raisedEvent =
            await RaisesAsync<TokenExpirationWarningChangedArgs>(
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                async () =>
                    result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
            );

        // Assert
        result.Should().BeFalse();
        raisedEvent.Arguments.TokenExpirationWarningEnum
            .Should()
            .Be(TokenExpirationWarning.LogoutNeeded);
        raisedEvent.Arguments.ValidTo.Should().BeNull();
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_AccessTokenOneMinuteLeft_ShouldReturnTrueAndRaiseOneMinuteLeftEvent_Async()
    {
        // Arrange
        JwtSecurityToken token = GetJwtSecurityToken(
            DateTime.Now.AddMinutes(1 - TokenService.ACCESS_TOKEN_DURATION),
            DateTime.Now.AddMinutes(1)
        );
        ClaimsPrincipal principal = GetClaimsPrincipal(token);
        _whiteList.Set(
            token.Claims.Single(_ => _.Type == JwtRegisteredClaimNames.Jti).Value,
            token
        );

        // Act
        bool result = false;
        RaisedEvent<TokenExpirationWarningChangedArgs> raisedEvent =
            await RaisesAsync<TokenExpirationWarningChangedArgs>(
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                async () =>
                    result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
            );

        // Assert
        result.Should().BeTrue();
        raisedEvent.Arguments.TokenExpirationWarningEnum
            .Should()
            .Be(TokenExpirationWarning.OneMinuteLeft);
        raisedEvent.Arguments.ValidTo.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_AccessTokenFiveMinutesLeft_ShouldReturnTrueAndRaiseFiveMinutesLeftEvent_Async()
    {
        // Arrange
        JwtSecurityToken token = GetJwtSecurityToken(
            DateTime.Now.AddMinutes(5 - TokenService.ACCESS_TOKEN_DURATION),
            DateTime.Now.AddMinutes(5)
        );
        ClaimsPrincipal principal = GetClaimsPrincipal(token);
        _whiteList.Set(
            token.Claims.Single(_ => _.Type == JwtRegisteredClaimNames.Jti).Value,
            token
        );

        // Act
        bool result = false;
        RaisedEvent<TokenExpirationWarningChangedArgs> raisedEvent =
            await RaisesAsync<TokenExpirationWarningChangedArgs>(
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                async () =>
                    result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
            );

        // Assert
        result.Should().BeTrue();
        raisedEvent.Arguments.TokenExpirationWarningEnum
            .Should()
            .Be(TokenExpirationWarning.FiveMinutesLeft);
        raisedEvent.Arguments.ValidTo.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTokenLifetimeAsync_AccessTokenMoreThanFiveMinutesLeft_ShouldReturnTrueAndNotRaiseEvent_Async()
    {
        // Arrange
        JwtSecurityToken token = GetJwtSecurityToken(
            DateTime.Now,
            DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION)
        );
        ClaimsPrincipal principal = GetClaimsPrincipal(token);
        _whiteList.Set(
            token.Claims.Single(_ => _.Type == JwtRegisteredClaimNames.Jti).Value,
            token
        );
        bool eventInvoked = false;
        void OnTokenExpirationWarningChanged(object? s, TokenExpirationWarningChangedArgs e) =>
            eventInvoked = true;

        // Act
        _tokenLifetimeValidator.TokenExpirationWarningChanged += OnTokenExpirationWarningChanged;
        bool result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal);
        _tokenLifetimeValidator.TokenExpirationWarningChanged -= OnTokenExpirationWarningChanged;

        // Assert
        result.Should().BeTrue();
        eventInvoked.Should().BeFalse();
    }
}
