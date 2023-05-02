using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Security
{
    /// <summary>
    /// Middleware gérant le stockage des jetons JWT
    /// </summary>
    public class JwtInSession
    {
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="JwtInSession"/>.
        /// </summary>
        /// <param name="tokenService">Le service de gestion des jetons.</param>
        public JwtInSession(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }
    }

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
            });
            services.AddSession();
            services.AddSingleton<JwtInSession>();

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
                    // On récupère le token de l'entête Authorization Bearer
                    string? authorization = context.Request.Headers.Authorization;
                    if (authorization != null && authorization.StartsWith("Bearer "))
                    {
                        string accessToken = authorization.Substring("Bearer ".Length);
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
                        IMemoryCache whiteList =
                            context.RequestServices.GetRequiredService<IMemoryCache>();
                        if (!whiteList.TryGetValue(refreshToken, out _))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return;
                        }
                        // On vérifie que le refresh token correspond bien à l'access token
                        ITokenService tokenService =
                            context.RequestServices.GetRequiredService<ITokenService>();
                        ClaimsPrincipal principal = tokenService.GetPrincipalFromExpiredToken(
                            accessToken
                        );
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
}
