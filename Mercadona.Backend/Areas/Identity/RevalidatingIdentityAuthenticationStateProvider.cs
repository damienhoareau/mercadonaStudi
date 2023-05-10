using Mercadona.Backend.Security;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Areas.Identity
{
    /// <summary>
    /// Classe permettant de g�rer l'authentification Blazor
    /// </summary>
    /// <typeparam name="TUser">Le type d'utilisateur.</typeparam>
    /// <seealso cref="Microsoft.AspNetCore.Components.Server.RevalidatingServerAuthenticationStateProvider" />
    public class RevalidatingIdentityAuthenticationStateProvider<TUser>
        : RevalidatingServerAuthenticationStateProvider where TUser : class
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdentityOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ITokenService _tokenService;
        private readonly IAuthenticationService _authenticationService;
        private readonly WhiteList _whiteList;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="RevalidatingIdentityAuthenticationStateProvider{TUser}"/>.
        /// </summary>
        /// <param name="loggerFactory">La fabrique de logger.</param>
        /// <param name="scopeFactory">La fabrique de p�rim�tre.</param>
        /// <param name="optionsAccessor">Options de <c>IdentityOptions</c>.</param>
        /// <param name="contextAccessor">Accesseur de contexte HTTP.</param>
        /// <param name="tokenService">Le service de gestion des jetons.</param>
        /// <param name="authenticationService">Le service d'authentification.</param>
        /// <param name="whiteList">Cache m�moire pour les jetons de renouvellement.</param>
        public RevalidatingIdentityAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> optionsAccessor,
            IHttpContextAccessor contextAccessor,
            ITokenService tokenService,
            IAuthenticationService authenticationService,
            WhiteList whiteList
        ) : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = optionsAccessor.Value;
            _contextAccessor = contextAccessor;
            _tokenService = tokenService;
            _authenticationService = authenticationService;
            _whiteList = whiteList;
        }

        /// <inheritdoc />
        protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(10);

        /// <inheritdoc />
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (
                _contextAccessor.HttpContext?.Session.GetString(TokenService.REFRESH_TOKEN_NAME)
                    is string refreshToken
                && _contextAccessor.HttpContext?.Session.GetString(TokenService.ACCESS_TOKEN_NAME)
                    is string accessToken
            )
            {
                // On r�cup�re l'identit� de l'utilisateur, tout en validant le token d'acc�s
                try
                {
                    ClaimsPrincipal principal = _tokenService.ValidateToken(accessToken);
                    // On v�rifie que l'utilisateur existe toujours
                    IdentityUser? user = await _authenticationService.FindUserByNameAsync(
                        principal.Identity?.Name ?? string.Empty
                    );
                    if (user != null)
                    {
                        // On v�rifie que le refresh token est bien autoris�
                        if (_whiteList.TryGetValue(refreshToken, out _))
                        {
                            // On v�rifie que le refresh token correspond bien � l'access token
                            if (
                                principal.Claims
                                    .FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)
                                    ?.Value == refreshToken
                            )
                            {
                                // On cr�e l'identit� de l'utilisateur
                                ClaimsIdentity identity =
                                    new(
                                        principal.Claims,
                                        Microsoft
                                            .AspNetCore
                                            .Authentication
                                            .Cookies
                                            .CookieAuthenticationDefaults
                                            .AuthenticationScheme
                                    );
                                principal = new ClaimsPrincipal(identity);
                                // On d�finit l'utilisateur comme connect� au circuit Blazor
                                SetAuthenticationState(
                                    Task.FromResult(new AuthenticationState(principal))
                                );
                            }
                        }
                    }
                }
                catch { }
            }
            return await base.GetAuthenticationStateAsync();
        }

        /// <inheritdoc />
        protected override async Task<bool> ValidateAuthenticationStateAsync(
            AuthenticationState authenticationState,
            CancellationToken cancellationToken
        )
        {
            // Get the user manager from a new scope to ensure it fetches fresh data
            IServiceScope scope = _scopeFactory.CreateScope();
            try
            {
                UserManager<TUser> userManager = scope.ServiceProvider.GetRequiredService<
                    UserManager<TUser>
                >();
                return await ValidateSecurityStampAsync(userManager, authenticationState.User);
            }
            finally
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync();
                }
                else
                {
                    scope.Dispose();
                }
            }
        }

        private async Task<bool> ValidateSecurityStampAsync(
            UserManager<TUser> userManager,
            ClaimsPrincipal principal
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
                    _options.ClaimsIdentity.SecurityStampClaimType
                );
                string userStamp = await userManager.GetSecurityStampAsync(user);
                return principalStamp == userStamp;
            }
        }
    }
}
