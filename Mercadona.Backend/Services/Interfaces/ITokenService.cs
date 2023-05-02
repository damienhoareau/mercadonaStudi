using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Mercadona.Backend.Services.Interfaces
{
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
        /// Extrait l'identité d'un utilisateur à partir d'un jeton expiré.
        /// </summary>
        /// <param name="expiredToken">Le jeton expiré.</param>
        /// <exception cref="SecurityTokenException"/>
        /// <returns>
        /// <seealso cref="ClaimsPrincipal"/> extrait du jeton<br/>
        /// <seealso cref="SecurityTokenException"/> : Si le jeton n'est pas valide<br/>
        /// </returns>
        ClaimsPrincipal GetPrincipalFromExpiredToken(string expiredToken);

        /// <summary>
        /// Revoque le jeton de renouvellement.
        /// </summary>
        /// <param name="refreshToken">Le jeton de renouvellement.</param>
        void RevokeRefreshToken(string refreshToken);
    }
}
