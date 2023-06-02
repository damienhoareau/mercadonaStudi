using Mercadona.Backend.Security;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Areas.Identity;

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

    private readonly IWhiteList _whiteList;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="TokenLifetimeValidator"/>.
    /// </summary>
    /// <param name="whiteList">Cache mémoire pour les jetons de renouvellement.</param>
    public TokenLifetimeValidator(IWhiteList whiteList)
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
                    new TokenExpirationWarningChangedArgs(TokenExpirationWarning.LogoutNeeded, null)
                );
                return false;
            }
            if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 1)
            {
                TokenExpirationWarningChanged?.Invoke(
                    this,
                    new TokenExpirationWarningChangedArgs(
                        TokenExpirationWarning.OneMinuteLeft,
                        accessToken.ValidTo
                    )
                );
            }
            else if (accessToken.ValidTo.Subtract(DateTime.UtcNow).TotalMinutes < 5)
            {
                TokenExpirationWarningChanged?.Invoke(
                    this,
                    new TokenExpirationWarningChangedArgs(
                        TokenExpirationWarning.FiveMinutesLeft,
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
    public TokenExpirationWarning TokenExpirationWarningEnum { get; private set; }

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
        TokenExpirationWarning tokenExpirationWarningEnum,
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
public enum TokenExpirationWarning
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
