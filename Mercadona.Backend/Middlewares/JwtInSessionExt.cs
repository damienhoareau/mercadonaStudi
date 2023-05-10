using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Security
{
    /// <summary>
    /// Extensions pour le middleware de stockage d'un JWT dans un cookie de session
    /// </summary>
    public static class JwtInSessionExt
    {
        /// <summary>
        /// Injecte les éléments du middleware de stockage JWT dans un cookie de session.
        /// </summary>
        /// <param name="services">La collection de services de l'application.</param>
        /// <returns></returns>
        public static IServiceCollection AddJwtInSession(this IServiceCollection services)
        {
            services.Configure<SessionOptions>(options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });
            services.AddSession();
            services.AddSingleton<WhiteList>();

            return services;
        }

        /// <summary>
        /// Ajoute le middleware de stockage JWT dans un cookie de session.
        /// </summary>
        /// <param name="app">Le builder d'application.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseJwtInSession(this IApplicationBuilder app)
        {
            app.UseSession();

            app.Use(
                async (context, next) =>
                {
                    // On récupère le token de l'entête Authorization Bearer (Swagger)
                    if (
                        context.Request.Headers.Authorization.ToString() is string authorization
                        && authorization.StartsWith($"{JwtBearerDefaults.AuthenticationScheme} ")
                    )
                    {
                        string accessToken = authorization[
                            $"{JwtBearerDefaults.AuthenticationScheme} ".Length..
                        ];
                        // On récupère le refresh token
                        string? refreshToken = context.Session.GetString(
                            TokenService.REFRESH_TOKEN_NAME
                        );
                        if (refreshToken == null)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                        // On vérifie que le refresh token est bien autorisé
                        WhiteList whiteList =
                            context.RequestServices.GetRequiredService<WhiteList>();
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
                            principal.Claims
                                .FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)
                                ?.Value != refreshToken
                        )
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                    }

                    await next(context);
                }
            );

            return app;
        }
    }

    /// <summary>
    /// Permet de stocker une liste de jetons de renouvellement autorisés.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Caching.Memory.MemoryCache" />
    public class WhiteList : MemoryCache
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="WhiteList"/>.
        /// </summary>
        /// <param name="options">Les options.</param>
        public WhiteList(IOptions<MemoryCacheOptions> options) : base(options) { }
    }
}
