using Mercadona.Backend.Services.Interfaces;

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
                options.IdleTimeout = TimeSpan.FromSeconds(10);
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
                    // On récupère le token de la session
                    // Si il est présent, on le met dans l'entête Authorization Bearer
                    // On récupère le token de l'entête Authorization Bearer
                    // On vérifie qu'il n'a pas expiré
                    // Si le token a expiré, on le supprime des validTokens et on renvoie Unauthorized

                    await next.Invoke();
                }
            );

            return app;
        }
    }
}
