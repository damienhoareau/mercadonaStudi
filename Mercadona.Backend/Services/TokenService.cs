using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Mercadona.Backend.Services
{
    /// <summary>
    /// Service permettant de produire des jetons d'authentification
    /// </summary>
    public class TokenService : ITokenService
    {
        public const string REFRESH_TOKEN_NAME = "mercadonaRefreshToken";
        public const string INVALID_TOKEN = "Invalid token";
        public const int TOKEN_EXPIRE_MINUTES = 30;

        private readonly IMemoryCache _whiteList;
        private readonly JwtBearerOptions _jwtOptions;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="OfferService"/>.
        /// </summary>
        /// <param name="whiteList">Cache mémoire pour les jetons de renouvellement.</param>
        /// <param name="jwtOptions">Options des jetons JWT.</param>
        public TokenService(IMemoryCache whiteList, JwtBearerOptions jwtOptions)
        {
            _whiteList = whiteList;
            _jwtOptions = jwtOptions;
        }

        /// <inheritdoc/>
        public string GenerateAccessToken(string refreshToken, IEnumerable<Claim> claims)
        {
            SymmetricSecurityKey authSigningKey = (SymmetricSecurityKey)
                _jwtOptions.TokenValidationParameters.IssuerSigningKey;

            JwtSecurityToken token =
                new(
                    issuer: _jwtOptions.TokenValidationParameters.ValidIssuer,
                    audience: _jwtOptions.TokenValidationParameters.ValidAudience,
                    expires: DateTime.Now.AddMinutes(TOKEN_EXPIRE_MINUTES),
                    claims: claims,
                    signingCredentials: new SigningCredentials(
                        authSigningKey,
                        SecurityAlgorithms.HmacSha256
                    )
                );

            // Stocker les jetons dans la whiteList
            _whiteList.Set(
                refreshToken,
                token,
                new MemoryCacheEntryOptions { AbsoluteExpiration = token.ValidTo }
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <inheritdoc/>
        public string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
            randomNumberGenerator.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        /// <inheritdoc/>
        public ClaimsPrincipal GetPrincipalFromExpiredToken(string expiredToken)
        {
            TokenValidationParameters tokenValidationParameters =
                _jwtOptions.TokenValidationParameters;
            tokenValidationParameters.ValidateLifetime = false;
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                expiredToken,
                tokenValidationParameters,
                out SecurityToken token
            );
            if (
                token is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                throw new SecurityTokenException(INVALID_TOKEN);
            return principal;
        }

        /// <inheritdoc/>
        public void RevokeRefreshToken(string refreshToken)
        {
            _whiteList.Remove(refreshToken);
        }
    }
}
