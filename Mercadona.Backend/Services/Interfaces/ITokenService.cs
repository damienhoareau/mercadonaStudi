using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Mercadona.Backend.Services.Interfaces;

/// <summary>
/// Interface d'un service permettant de produire des jetons d'authentification
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Génère un jeton d'accès.
    /// </summary>
    /// <param name="refreshToken">Le jeton de renouvellement.</param>
    /// <param name="claims">Les droits.</param>
    /// <returns>Le jeton d'accès</returns>
    string GenerateAccessToken(string refreshToken, IEnumerable<Claim> claims);

    /// <summary>
    /// Génère un jeton de renouvellement.
    /// </summary>
    /// <returns>Le jeton de renouvellement</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Régénère un jeton d'accès à partir d'un jeton de renouvellement.
    /// </summary>
    /// <param name="refreshToken">Le jeton de renouvellement</param>
    /// <returns>Le nouveau jeton d'accès</returns>
    string RefreshToken(string refreshToken);

    /// <summary>
    /// Extrait l'identité d'un utilisateur à partir d'un jeton d'accès.
    /// </summary>
    /// <param name="token">Le jeton d'accès.</param>
    /// <exception cref="SecurityTokenException"/>
    /// <returns>
    /// <seealso cref="ClaimsPrincipal"/> extrait du jeton d'accès<br/>
    /// <seealso cref="SecurityTokenException"/> : Si le jeton d'accès n'est pas valide<br/>
    /// </returns>
    ClaimsPrincipal GetPrincipalFromToken(string token);

    /// <summary>
    /// Extrait l'identité d'un utilisateur à partir d'un jeton d'accès.
    /// </summary>
    /// <param name="token">Le jeton d'accès.</param>
    /// <param name="validateLifetime">Booléen déterminant si on doit valider la date du jeton.</param>
    /// <exception cref="SecurityTokenException"/>
    /// <returns>
    /// <seealso cref="ClaimsPrincipal"/> extrait du jeton d'accès<br/>
    /// <seealso cref="SecurityTokenException"/> : Si le jeton d'accès n'est pas valide<br/>
    /// </returns>
    ClaimsPrincipal ValidateToken(string token, bool validateLifetime = true);

    /// <summary>
    /// Revoque le jeton de renouvellement.
    /// </summary>
    /// <param name="refreshToken">Le jeton de renouvellement.</param>
    void RevokeRefreshToken(string refreshToken);
}
