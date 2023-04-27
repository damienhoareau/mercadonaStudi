using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;

namespace Mercadona.Backend.Events
{
    /// <summary>
    /// Gestionnaire d'authentification
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents" />
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        /// <summary>
        /// Empêche la redirection vers la page d'authentification pour les requêtes vers l'API.
        /// </summary>
        /// <param name="context">Le contexte de redirection.</param>
        /// <returns></returns>
        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            if (
                context.Request.Path.StartsWithSegments("/api")
                && context.Response.StatusCode == StatusCodes.Status200OK
            )
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            else
            {
                return base.RedirectToLogin(context);
            }
        }

        /// <summary>
        /// Empêche la redirection lors de l'accès à une requête API.
        /// </summary>
        /// <param name="context">Le contexte de redirection.</param>
        /// <returns></returns>
        public override Task RedirectToAccessDenied(
            RedirectContext<CookieAuthenticationOptions> context
        )
        {
            if (
                context.Request.Path.StartsWithSegments("/api")
                && context.Response.StatusCode == StatusCodes.Status200OK
            )
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            else
            {
                return base.RedirectToAccessDenied(context);
            }
        }
    }
}
