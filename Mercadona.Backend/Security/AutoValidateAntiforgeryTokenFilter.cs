using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Mercadona.Backend.Security;

/// <summary>
/// Filtre ne validant le jeton anti-falsification que s'il n'y a pas de jeton d'accès
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Mvc.Filters.IAsyncAuthorizationFilter" />
/// <seealso cref="Microsoft.AspNetCore.Mvc.ViewFeatures.IAntiforgeryPolicy" />
public class AuthAutoValidateAntiforgeryTokenFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy
{
    private readonly IAntiforgery _antiforgery;

    /// <summary>
    /// Initialise une instance de la classe <see cref="AuthAutoValidateAntiforgeryTokenFilter"/>.
    /// </summary>
    /// <param name="antiforgery">Le service de gestion anti-falsification.</param>
    public AuthAutoValidateAntiforgeryTokenFilter(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    /// <summary>
    /// Appelée dans la file des filtres pour confirmer que la requête est authorisée.
    /// </summary>
    /// <param name="context">Le <see cref="T:Microsoft.AspNetCore.Mvc.Filters.AuthorizationFilterContext" />.</param>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.IsEffectivePolicy(this))
            return;

        if (ShouldValidate(context))
        {
            try
            {
                await _antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch
            {
                context.Result = new AntiforgeryValidationFailedResult();
            }
        }
    }

    /// <summary>
    /// Détermine si le jeton anti-falsification doit être validé.
    /// </summary>
    /// <param name="context">Le contexte.</param>
    /// <returns></returns>
    protected virtual bool ShouldValidate(AuthorizationFilterContext context)
    {
        // Ignorer la validation si un jeton d'accès est présent
        if (context.HttpContext.Request.Headers.ContainsKey("Authorization"))
            return false;

        // Ignorer la validation pour les méthodes GET, HEAD, TRACE, OPTIONS
        string method = context.HttpContext.Request.Method;
        if (
            HttpMethods.IsGet(method)
            || HttpMethods.IsHead(method)
            || HttpMethods.IsTrace(method)
            || HttpMethods.IsOptions(method)
        )
            return false;

        // On doit valider l'anti-falsification
        return true;
    }
}

/// <summary>
/// Spécifie que la classe ou la méthode sur laquelle l'attribut est appliqué valide un jeton anti-falsification.
/// Si le jeton anti-falsification n'est pas disponible, ou s'il n'est pas valide, la validation échoue
/// et la méthode n'est pas exécutée.
/// </summary>
/// <remarks>
/// Cet attribut aide à défendre contre les attaques Cross-Site.
/// </remarks>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true
)]
public class AuthAutoValidateAntiforgeryTokenAttribute : Attribute, IFilterFactory, IOrderedFilter
{
    /// <summary>
    /// Gets the order value for determining the order of execution of filters. Filters execute in
    /// ascending numeric value of the <see cref="Order"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Filters are executed in an ordering determined by an ascending sort of the <see cref="Order"/> property.
    /// </para>
    /// <para>
    /// The default Order for this attribute is 1000 because it must run after any filter which does authentication
    /// or login in order to allow them to behave as expected (ie Unauthenticated or Redirect instead of 400).
    /// </para>
    /// <para>
    /// Look at <see cref="IOrderedFilter.Order"/> for more detailed info.
    /// </para>
    /// </remarks>
    public int Order { get; set; } = 1000;

    /// <inheritdoc />
    public bool IsReusable => true;

    /// <inheritdoc />
    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<AuthAutoValidateAntiforgeryTokenFilter>();
    }
}
