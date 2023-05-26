using Mercadona.Backend.Security;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Mercadona.Backend.Services;

/// <summary>
/// Service permettant de produire des jetons d'authentification
/// </summary>
public class TokenService : ITokenService
{
#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
    public const string REFRESH_TOKEN_NAME = "mercadonaRefreshToken";
    public const string ACCESS_TOKEN_NAME = "mercadonaAccessToken";
    public const string INVALID_TOKEN = "Invalid token";
#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

    /// <summary>
    /// La durée de vie du jeton d'accès en minutes
    /// </summary>
    public const int ACCESS_TOKEN_DURATION = 15;

    /// <summary>
    /// La durée de vie du jeton de renouvellement en heures
    /// </summary>
    public const int REFRESH_TOKEN_DURATION = 8;

    private readonly WhiteList _whiteList;
    private readonly JwtBearerOptions _jwtOptions;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="OfferService"/>.
    /// </summary>
    /// <param name="whiteList">Cache mémoire pour les jetons de renouvellement.</param>
    /// <param name="jwtOptionsMonitor">Options des jetons JWT.</param>
    public TokenService(WhiteList whiteList, IOptionsMonitor<JwtBearerOptions> jwtOptionsMonitor)
    {
        _whiteList = whiteList;
        _jwtOptions = jwtOptionsMonitor.Get(JwtBearerDefaults.AuthenticationScheme);
    }

    /// <inheritdoc/>
    public string GenerateAccessToken(string refreshToken, IEnumerable<Claim> claims)
    {
        SymmetricSecurityKey authSigningKey = (SymmetricSecurityKey)
            _jwtOptions.TokenValidationParameters.IssuerSigningKey;

        DateTime now = DateTime.Now;
        JwtSecurityToken token =
            new(
                issuer: _jwtOptions.TokenValidationParameters.ValidIssuer,
                audience: _jwtOptions.TokenValidationParameters.ValidAudience,
                notBefore: now,
                expires: now.AddMinutes(ACCESS_TOKEN_DURATION),
                claims: claims,
                signingCredentials: new SigningCredentials(
                    authSigningKey,
                    SecurityAlgorithms.HmacSha256
                )
            );

        // Stocker les jetons dans la whiteList
        // Le jeton de renouvellement est supprimé au bout de 30 minutes d'inactivité
        // (ce qui correspond à la durée de vie du jeton d'accès).
        // Dans tous les cas, une reconnexion est obligatoire au bout de 8 heures.
        _whiteList.Set(
            refreshToken,
            token,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddHours(REFRESH_TOKEN_DURATION),
                SlidingExpiration = token.ValidTo.Subtract(token.ValidFrom)
            }
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
    public string RefreshToken(string refreshToken)
    {
        JwtSecurityToken jwtSecurityToken =
            _whiteList.Get<JwtSecurityToken>(refreshToken)
            ?? throw new SecurityTokenException(INVALID_TOKEN);
        ClaimsPrincipal principal = GetPrincipalFromToken(
            new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken)
        );
        return GenerateAccessToken(refreshToken, principal.Claims);
    }

    /// <inheritdoc/>
    public ClaimsPrincipal GetPrincipalFromToken(string token)
    {
        return ValidateToken(token, false);
    }

    /// <inheritdoc/>
    public ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true)
    {
        try
        {
            TokenValidationParameters tokenValidationParameters =
                _jwtOptions.TokenValidationParameters;
            if (!validateLifetime)
                tokenValidationParameters.ValidateLifetime = false;
            ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                tokenValidationParameters,
                out SecurityToken securityToken
            );
            if (
                securityToken is not JwtSecurityToken jwtSecurityToken
                || !jwtSecurityToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
                throw new SecurityTokenException(INVALID_TOKEN);
            return principal;
        }
        catch (Exception ex)
        {
            throw new SecurityTokenException(INVALID_TOKEN, ex);
        }
    }

    /// <inheritdoc/>
    public void RevokeRefreshToken(string refreshToken)
    {
        _whiteList.Remove(refreshToken);
    }
}
