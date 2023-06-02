using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Security;

/// <summary>
/// Extensions pour le middleware d'authentification Blazor avec un JWT
/// </summary>
public static class JwtBlazorExt
{
    /// <summary>
    /// Injecte les éléments du middleware d'authentification Blazor avec un JWT.
    /// </summary>
    /// <param name="services">La collection de services de l'application.</param>
    /// <returns></returns>
    public static IServiceCollection AddJwtBlazor(this IServiceCollection services)
    {
        services.Configure<SessionOptions>(options =>
        {
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        });
        services.AddSession();
        services.AddHttpContextAccessor();
        services.AddSingleton<IWhiteList, WhiteList>();
        services.AddSingleton<AuthAutoValidateAntiforgeryTokenFilter>();
        services.AddScoped<JwtBlazorMiddleware>();

        return services;
    }

    /// <summary>
    /// Ajoute le middleware d'authentification Blazor avec un JWT.
    /// </summary>
    /// <param name="app">Le builder d'application.</param>
    /// <returns></returns>
    public static IApplicationBuilder UseJwtBlazor(this IApplicationBuilder app)
    {
        app.UseSession();
        app.UseMiddleware<JwtBlazorMiddleware>();

        return app;
    }
}

/// <summary>
/// Interface d'une classe permettant de stocker une liste de jetons de renouvellement autorisés.
/// </summary>
public interface IWhiteList : IMemoryCache
{
    /// <summary>
    /// Supprime toutes les clés et valeurs du cache.
    /// </summary>
    void Clear();
}

/// <summary>
/// Classe permettant de stocker une liste de jetons de renouvellement autorisés.
/// </summary>
/// <seealso cref="Microsoft.Extensions.Caching.Memory.MemoryCache" />
public class WhiteList : MemoryCache, IWhiteList
{
    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="WhiteList"/>.
    /// </summary>
    /// <param name="options">Les options.</param>
    public WhiteList(IOptions<MemoryCacheOptions> options) : base(options) { }
}

/// <summary>
/// Middleware d'authentification Blazor avec un JWT
/// </summary>
/// <seealso cref="Microsoft.AspNetCore.Http.IMiddleware" />
public class JwtBlazorMiddleware : IMiddleware
{
    /// <summary>
    /// Exécute le middleware.
    /// </summary>
    /// <param name="context">The <see cref="T:Microsoft.AspNetCore.Http.HttpContext" /> pour la requête courante.</param>
    /// <param name="next">Le délégé qui représente le prochain middleware dans la pile.</param>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // On récupère le token de l'entête Authorization Bearer
        if (
            context.Request.Headers.Authorization.ToString() is string authorization
            && authorization.StartsWith($"{JwtBearerDefaults.AuthenticationScheme} ")
        )
        {
            string accessToken = authorization[
                $"{JwtBearerDefaults.AuthenticationScheme} ".Length..
            ];
            // On récupère le refresh token
            string? refreshToken = context.Session.GetString(TokenService.REFRESH_TOKEN_NAME);
            if (refreshToken == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            // On vérifie que le refresh token est bien autorisé
            IWhiteList whiteList = context.RequestServices.GetRequiredService<IWhiteList>();
            if (!whiteList.TryGetValue(refreshToken, out _))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            // On vérifie que le refresh token correspond bien à l'access token
            ITokenService tokenService =
                context.RequestServices.GetRequiredService<ITokenService>();
            ClaimsPrincipal principal = tokenService.GetPrincipalFromToken(accessToken);
            if (
                principal.Claims.FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)?.Value
                != refreshToken
            )
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
        }

        await next(context);
    }
}
