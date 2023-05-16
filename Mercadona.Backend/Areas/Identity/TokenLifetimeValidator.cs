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
        event EventHandler<TokenExpirationWarningChangedArgs>? TokenExpirationWarningChanged;

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
        public event EventHandler<TokenExpirationWarningChangedArgs>? TokenExpirationWarningChanged;

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
                        new TokenExpirationWarningChangedArgs(
                            TokenExpirationWarningEnum.LogoutNeeded,
                            null
                        )
                    );
                    return false;
                }
                else if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 1)
                {
                    TokenExpirationWarningChanged?.Invoke(
                        this,
                        new TokenExpirationWarningChangedArgs(
                            TokenExpirationWarningEnum.OneMinuteLeft,
                            accessToken.ValidTo
                        )
                    );
                }
                else if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 5)
                {
                    TokenExpirationWarningChanged?.Invoke(
                        this,
                        new TokenExpirationWarningChangedArgs(
                            TokenExpirationWarningEnum.FiveMinutesLeft,
                            accessToken.ValidTo
                        )
                    );
                }

                return true;
            });
        }
    }

    /// <summary>
    /// Représente les arguments de l'événement levé lors de l'avertissement d'expiration du jeton d'accès
    /// </summary>
    public class TokenExpirationWarningChangedArgs
    {
        /// <summary>
        /// Obtient ou définit l'enum d'expiration du jeton d'accès.
        /// </summary>
        public TokenExpirationWarningEnum TokenExpirationWarningEnum { get; private set; }

        /// <summary>
        /// Obtient ou définit la date d'expiration du jeton d'accès.
        /// </summary>
        public DateTime? ValidTo { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenExpirationWarningChangedArgs"/> class.
        /// </summary>
        /// <param name="tokenExpirationWarningEnum">L'enum d'expiration du jeton d'accès.</param>
        /// <param name="validTo">La date d'expiration du jeton d'accès.</param>
        public TokenExpirationWarningChangedArgs(
            TokenExpirationWarningEnum tokenExpirationWarningEnum,
            DateTime? validTo = null
        )
        {
            TokenExpirationWarningEnum = tokenExpirationWarningEnum;
            ValidTo = validTo;
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
