using FluentAssertions;
using Mercadona.Backend.Areas.Identity;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Mercadona.Tests.Moq;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Tests.Security
{
    public class SecurityStampValidatorTests
    {
        private readonly IdentityOptions _identityOptions = new();
        private readonly JwtSecurityToken _jwtSecurityToken;
        private readonly ClaimsPrincipal _principal;
        private readonly ISecurityStampValidator<IdentityUser> _securityStampValidator;

        public SecurityStampValidatorTests()
        {
            _jwtSecurityToken = GetJwtSecurityToken();

            _principal = GetClaimsPrincipal(_jwtSecurityToken);

            _securityStampValidator =
                new Backend.Areas.Identity.SecurityStampValidator<IdentityUser>();
        }

        private static JwtSecurityToken GetJwtSecurityToken()
        {
            return new(
                issuer: TestsHelper.TokenValidationParameters.ValidIssuer,
                audience: TestsHelper.TokenValidationParameters.ValidAudience,
                expires: DateTime.Now.AddMinutes(TokenService.ACCESS_TOKEN_DURATION),
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
            return new JwtSecurityTokenHandler().ValidateToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                TestsHelper.TokenValidationParameters,
                out _
            );
        }

        [Fact]
        public async void ValidateSecurityStampAsync_UserNotExist_ShouldReturnFalse()
        {
            // Arrange
            UserManagerMock userManagerMock = new();

            // Act
            bool result = await _securityStampValidator.ValidateSecurityStampAsync(
                userManagerMock,
                new ClaimsPrincipal(new ClaimsIdentity()),
                _identityOptions
            );

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async void ValidateSecurityStampAsync_DoesNotSupportUserSecurityStamp_ShouldReturnTrue()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });

            // Act
            bool result = await _securityStampValidator.ValidateSecurityStampAsync(
                userManagerMock,
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        _principal.Claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    )
                ),
                _identityOptions
            );

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async void ValidateSecurityStampAsync_DifferentSecurityStamp_ShouldReturnFalse()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
            userManagerMock.SetSupportsUserSecurityStamp(true);

            // Act
            bool result = await _securityStampValidator.ValidateSecurityStampAsync(
                userManagerMock,
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        _principal.Claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    )
                ),
                _identityOptions
            );

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async void ValidateSecurityStampAsync_SameSecurityStamp_ShouldReturnTrue()
        {
            // Arrange
            UserManagerMock userManagerMock =
                new(false, new UserModel { Username = "toto@toto.fr", Password = "V@lidPassw0rd" });
            userManagerMock.UsersList.First().SecurityStamp = _jwtSecurityToken.Claims
                .Single(_ => _.Type == "AspNet.Identity.SecurityStamp")
                .Value;
            userManagerMock.SetSupportsUserSecurityStamp(true);

            // Act
            bool result = await _securityStampValidator.ValidateSecurityStampAsync(
                userManagerMock,
                new ClaimsPrincipal(
                    new ClaimsIdentity(
                        _principal.Claims,
                        CookieAuthenticationDefaults.AuthenticationScheme
                    )
                ),
                _identityOptions
            );

            // Assert
            result.Should().BeTrue();
        }
    }
}
