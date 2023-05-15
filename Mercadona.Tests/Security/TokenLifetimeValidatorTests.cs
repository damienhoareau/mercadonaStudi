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

namespace Mercadona.Tests.Security
{
    public class TokenLifetimeValidatorTests : IAsyncLifetime
    {
        private readonly ITokenLifetimeValidator _tokenLifetimeValidator;
        private readonly MemoryCacheOptions _memoryCacheOptions = new();
        private readonly WhiteList _whiteList;

        public TokenLifetimeValidatorTests()
        {
            Mock<IOptions<MemoryCacheOptions>> mockMemoryCacheOptions = new();
            mockMemoryCacheOptions.SetupGet(_ => _.Value).Returns(_memoryCacheOptions);

            _whiteList = new(mockMemoryCacheOptions.Object);

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

        private JwtSecurityToken GetJwtSecurityToken(DateTime notBefore, DateTime expires)
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

        private ClaimsPrincipal GetClaimsPrincipal(JwtSecurityToken token)
        {
            TokenValidationParameters tokenValidationParameters =
                TestsHelper.TokenValidationParameters;
            tokenValidationParameters.ValidateLifetime = false;
            return new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                tokenValidationParameters,
                out _
            );
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_RefreshTokenDoesNotExist_ShouldReturnFalseAndRaiseLogoutEvent()
        {
            // Arrange
            ClaimsPrincipal principal = new(new ClaimsIdentity());

            // Act
            bool result = false;
            RaisedEvent<TokenExpirationWarningEnum> raisedEvent =
                await RaisesAsync<TokenExpirationWarningEnum>(
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                    async () =>
                        result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
                );

            // Assert
            result.Should().BeFalse();
            raisedEvent.Arguments.Should().Be(TokenExpirationWarningEnum.LogoutNeeded);
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_RefreshTokenNotInWhitelist_ShouldReturnFalseAndRaiseLogoutEvent()
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
            RaisedEvent<TokenExpirationWarningEnum> raisedEvent =
                await RaisesAsync<TokenExpirationWarningEnum>(
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                    async () =>
                        result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
                );

            // Assert
            result.Should().BeFalse();
            raisedEvent.Arguments.Should().Be(TokenExpirationWarningEnum.LogoutNeeded);
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_AccessTokenExpired_ShouldReturnFalseAndRaiseLogoutEvent()
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
            RaisedEvent<TokenExpirationWarningEnum> raisedEvent =
                await RaisesAsync<TokenExpirationWarningEnum>(
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                    async () =>
                        result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
                );

            // Assert
            result.Should().BeFalse();
            raisedEvent.Arguments.Should().Be(TokenExpirationWarningEnum.LogoutNeeded);
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_AccessTokenOneMinuteLeft_ShouldReturnTrueAndRaiseOneMinuteLeftEvent()
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
            RaisedEvent<TokenExpirationWarningEnum> raisedEvent =
                await RaisesAsync<TokenExpirationWarningEnum>(
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                    async () =>
                        result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
                );

            // Assert
            result.Should().BeTrue();
            raisedEvent.Arguments.Should().Be(TokenExpirationWarningEnum.OneMinuteLeft);
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_AccessTokenFiveMinutesLeft_ShouldReturnTrueAndRaiseFiveMinutesLeftEvent()
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
            RaisedEvent<TokenExpirationWarningEnum> raisedEvent =
                await RaisesAsync<TokenExpirationWarningEnum>(
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged += a,
                    a => _tokenLifetimeValidator.TokenExpirationWarningChanged -= a,
                    async () =>
                        result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal)
                );

            // Assert
            result.Should().BeTrue();
            raisedEvent.Arguments.Should().Be(TokenExpirationWarningEnum.FiveMinutesLeft);
        }

        [Fact]
        public async void ValidateTokenLifetimeAsync_AccessTokenMoreThanFiveMinutesLeft_ShouldReturnTrueAndNotRaiseEvent()
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
            void OnTokenExpirationWarningChanged(object? s, TokenExpirationWarningEnum e) =>
                eventInvoked = true;

            // Act
            _tokenLifetimeValidator.TokenExpirationWarningChanged +=
                OnTokenExpirationWarningChanged;
            bool result = await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(principal);
            _tokenLifetimeValidator.TokenExpirationWarningChanged -=
                OnTokenExpirationWarningChanged;

            // Assert
            result.Should().BeTrue();
            eventInvoked.Should().BeFalse();
        }
    }
}
