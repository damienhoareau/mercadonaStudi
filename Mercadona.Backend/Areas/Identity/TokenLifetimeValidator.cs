using Mercadona.Backend.Security;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Areas.Identity
{
    /// <summary>
    /// Interface d'une classe permettant de gérer les notifications d'expiration du jeton d'accès
    /// </summary>
    public interface ITokenLifetimeValidator
    {
        /// <summary>
        /// Est levé lorsqu'il reste peu de temps avant que le jeton d'accès soit expiré.
        /// </summary>
        event EventHandler<TokenExpirationWarningEnum>? TokenExpirationWarningChanged;

        /// <summary>
        /// Valide le jeton.
        /// </summary>
        /// <param name="principal">L'identité de l'utilisateur.</param>
        /// <returns><c>false</c> si le jeton a expiré.</returns>
        Task<bool> ValidateTokenLifetimeAsync(ClaimsPrincipal principal);
    }

    /// <summary>
    /// Classe permettant de gérer les notifications d'expiration du jeton d'accès
    /// </summary>
    public class TokenLifetimeValidator : ITokenLifetimeValidator
    {
        /// <inheritdoc />
        public event EventHandler<TokenExpirationWarningEnum>? TokenExpirationWarningChanged;

        private readonly WhiteList _whiteList;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="TokenLifetimeValidator"/>.
        /// </summary>
        /// <param name="whiteList">Cache mémoire pour les jetons de renouvellement.</param>
        public TokenLifetimeValidator(WhiteList whiteList)
        {
            _whiteList = whiteList;
        }

        /// <inheritdoc />
        public Task<bool> ValidateTokenLifetimeAsync(ClaimsPrincipal principal)
        {
            return Task.Run(() =>
            {
                string? refreshToken = principal.Claims
                    .SingleOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)
                    ?.Value;
                if (
                    refreshToken == null
                    || !_whiteList.TryGetValue(refreshToken, out JwtSecurityToken? accessToken)
                    || accessToken!.ValidTo < DateTime.UtcNow
                )
                {
                    TokenExpirationWarningChanged?.Invoke(
                        this,
                        TokenExpirationWarningEnum.LogoutNeeded
                    );
                    return false;
                }
                else if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 1)
                {
                    TokenExpirationWarningChanged?.Invoke(
                        this,
                        TokenExpirationWarningEnum.OneMinuteLeft
                    );
                }
                else if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 5)
                {
                    TokenExpirationWarningChanged?.Invoke(
                        this,
                        TokenExpirationWarningEnum.FiveMinutesLeft
                    );
                }

                return true;
            });
        }
    }

    /// <summary>
    /// Représente le temps restant avant expiration du jeton d'accès
    /// </summary>
    public enum TokenExpirationWarningEnum
    {
        /// <summary>
        /// Il reste moins de 5 minutes
        /// </summary>
        FiveMinutesLeft,

        /// <summary>
        /// Il reste moins d'une minute
        /// </summary>
        OneMinuteLeft,

        /// <summary>
        /// Le jeton est expiré
        /// </summary>
        LogoutNeeded
    }
}
