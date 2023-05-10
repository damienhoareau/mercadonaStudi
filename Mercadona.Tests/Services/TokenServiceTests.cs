using FluentAssertions;
using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Fixtures;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mercadona.Tests.Services
{
    public class TokenServiceTests : IClassFixture<ServiceProviderFixture>
    {
        private readonly ServiceProviderFixture _fixture;
        private readonly ITokenService _tokenService;

        public TokenServiceTests(ServiceProviderFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddMemoryCache();
                services.AddSingleton<WhiteList>();
                services.AddSingleton<IConfiguration>(provider =>
                {
                    ConfigurationBuilder builder = new();
                    builder.AddInMemoryCollection();
                    return builder.Build();
                });
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
                        options.TokenValidationParameters = new TokenValidationParameters()
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidAudience = "https://localhost:44387",
                            ValidIssuer = "https://localhost:44387",
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(
                                    "JWTAuthenticationHIGHsecuredPasswordVVVp1OH7XzyrForTest"
                                )
                            )
                        };
                    });
                services.AddSingleton<ITokenService, TokenService>();

                return services;
            });

            _tokenService = _fixture.GetRequiredService<ITokenService>();
        }

        [Fact]
        public void GenerateAccessToken_ShouldGenerateValidToken()
        {
            // Arrange
            string refreshToken = _tokenService.GenerateRefreshToken();
            List<Claim> authClaims =
                new()
                {
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, refreshToken),
                };

            // Act
            string result = _tokenService.GenerateAccessToken(refreshToken, authClaims);

            // Assert
            new JwtSecurityTokenHandler().ValidateToken(
                result,
                _fixture
                    .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                    .Get(JwtBearerDefaults.AuthenticationScheme)
                    .TokenValidationParameters,
                out SecurityToken validatedToken
            );
            validatedToken.Should().BeOfType<JwtSecurityToken>();
            JwtSecurityToken validatedJwtToken = (JwtSecurityToken)validatedToken;
            validatedJwtToken.Id.Should().Be(refreshToken);
            JwtSecurityToken? inMemoryJwtToken = _fixture
                .GetRequiredService<WhiteList>()
                .Get<JwtSecurityToken>(validatedJwtToken.Id);
            inMemoryJwtToken.Should().NotBeNull();
        }

        [Fact]
        public void GenerateRefreshToken_ShouldGenerateUniqueToken()
        {
            // Arrange

            // Act
            string refreshToken1 = _tokenService.GenerateRefreshToken();
            string refreshToken2 = _tokenService.GenerateRefreshToken();

            // Assert
            refreshToken1.Should().NotBe(refreshToken2);
        }

        [Fact]
        public void GetPrincipalFromToken_InvalidToken_ShouldThrowSecurityTokenException()
        {
            // Arrange
            string refreshToken = _tokenService.GenerateRefreshToken();
            List<Claim> authClaims =
                new()
                {
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, refreshToken),
                };
            JwtBearerOptions jwtBearerOptions = _fixture
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);
            SymmetricSecurityKey authSigningKey = (SymmetricSecurityKey)
                jwtBearerOptions.TokenValidationParameters.IssuerSigningKey;
            JwtSecurityToken token =
                new(
                    issuer: jwtBearerOptions.TokenValidationParameters.ValidIssuer,
                    audience: jwtBearerOptions.TokenValidationParameters.ValidAudience,
                    expires: DateTime.Now.AddMinutes(30),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(
                        authSigningKey,
                        SecurityAlgorithms.HmacSha384
                    )
                );
            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            // Act
            Action act = () =>
            {
                ClaimsPrincipal result = _tokenService.GetPrincipalFromToken(accessToken);
            };

            // Assert
            act.Should().Throw<SecurityTokenException>();
        }

        [Fact]
        public void GetPrincipalFromToken_ShouldGetCorrectPrincipal()
        {
            // Arrange
            string refreshToken = _tokenService.GenerateRefreshToken();
            List<Claim> authClaims =
                new()
                {
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, refreshToken),
                };
            string accessToken = _tokenService.GenerateAccessToken(refreshToken, authClaims);

            // Act
            ClaimsPrincipal result = _tokenService.GetPrincipalFromToken(accessToken);

            // Assert
            result.Identity.Should().NotBeNull();
            result.Identity!.Name.Should().Be("toto@toto.fr");
            result.Claims
                .FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)
                ?.Value.Should()
                .Be(refreshToken);
        }

        [Fact]
        public void RevokeRefreshToken_ShouldRevokeCorrectlyToken()
        {
            // Arrange
            string refreshToken = _tokenService.GenerateRefreshToken();
            List<Claim> authClaims =
                new()
                {
                    new Claim(ClaimTypes.Name, "toto@toto.fr"),
                    new Claim(JwtRegisteredClaimNames.Jti, refreshToken),
                };

            // Act (login)
            _tokenService.GenerateAccessToken(refreshToken, authClaims);

            // Assert (login)
            JwtSecurityToken? inMemoryJwtToken = _fixture
                .GetRequiredService<WhiteList>()
                .Get<JwtSecurityToken>(refreshToken);
            inMemoryJwtToken.Should().NotBeNull();

            // Act (logout)
            _tokenService.RevokeRefreshToken(refreshToken);

            // Assert (logout)
            inMemoryJwtToken = _fixture
                .GetRequiredService<WhiteList>()
                .Get<JwtSecurityToken>(refreshToken);
            inMemoryJwtToken.Should().BeNull();
        }
    }
}
