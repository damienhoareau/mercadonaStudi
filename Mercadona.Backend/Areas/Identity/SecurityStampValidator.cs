using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Mercadona.Backend.Areas.Identity
{
    /// <summary>
    /// Interface d'une classe permettant de valider le tampon de sécurité d'un utilisateur
    /// </summary>
    /// <typeparam name="TUser">Le type d'utilisateur.</typeparam>
    public interface ISecurityStampValidator<TUser> where TUser : class
    {
        /// <summary>
        /// Valide le tampon de sécurité.
        /// </summary>
        /// <param name="userManager">Le manager d'utilisateurs.</param>
        /// <param name="principal">L'identité de l'utilisateur.</param>
        /// <param name="options">Les options de la gestion d'identité.</param>
        /// <returns></returns>
        Task<bool> ValidateSecurityStampAsync(
            UserManager<TUser> userManager,
            ClaimsPrincipal principal,
            IdentityOptions options
        );
    }

    /// <summary>
    /// Classe permettant de valider le tampon de sécurité d'un utilisateur
    /// </summary>
    /// <typeparam name="TUser">Le type d'utilisateur.</typeparam>
    public class SecurityStampValidator<TUser> : ISecurityStampValidator<TUser> where TUser : class
    {
        /// <inheritdoc />
        public async Task<bool> ValidateSecurityStampAsync(
            UserManager<TUser> userManager,
            ClaimsPrincipal principal,
            IdentityOptions options
        )
        {
            if (await userManager.GetUserAsync(principal) is not TUser user)
            {
                return false;
            }
            else if (!userManager.SupportsUserSecurityStamp)
            {
                return true;
            }
            else
            {
                string? principalStamp = principal.FindFirstValue(
                    options.ClaimsIdentity.SecurityStampClaimType
                );
                string userStamp = await userManager.GetSecurityStampAsync(user);
                return principalStamp == userStamp;
            }
        }
    }
}
