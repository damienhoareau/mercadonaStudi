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
    /// Classe permettant de gérer l'authentification Blazor
    /// </summary>
    /// <typeparam name="TUser">Le type d'utilisateur.</typeparam>
    /// <seealso cref="Microsoft.AspNetCore.Components.Server.RevalidatingServerAuthenticationStateProvider" />
    public class RevalidatingIdentityAuthenticationStateProvider<TUser>
        : RevalidatingServerAuthenticationStateProvider where TUser : class
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IdentityOptions _options;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ISecurityStampValidator<TUser> _securityStampValidator;
        private readonly ITokenLifetimeValidator _tokenLifetimeValidator;
        private readonly ITokenService _tokenService;
        private readonly IAuthenticationService _authenticationService;
        private readonly WhiteList _whiteList;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="RevalidatingIdentityAuthenticationStateProvider{TUser}"/>.
        /// </summary>
        /// <param name="loggerFactory">La fabrique de logger.</param>
        /// <param name="scopeFactory">La fabrique de périmètre.</param>
        /// <param name="optionsAccessor">Options de <c>IdentityOptions</c>.</param>
        /// <param name="contextAccessor">Accesseur de contexte HTTP.</param>
        /// <param name="securityStampValidator">La classe de validation des tampons de sécurité.</param>
        /// <param name="tokenLifetimeValidator">La classe de validation du temps restant du jeton d'accès.</param>
        /// <param name="tokenService">Le service de gestion des jetons.</param>
        /// <param name="authenticationService">Le service d'authentification.</param>
        /// <param name="whiteList">Cache mémoire pour les jetons de renouvellement.</param>
        public RevalidatingIdentityAuthenticationStateProvider(
            ILoggerFactory loggerFactory,
            IServiceScopeFactory scopeFactory,
            IOptions<IdentityOptions> optionsAccessor,
            IHttpContextAccessor contextAccessor,
            ISecurityStampValidator<TUser> securityStampValidator,
            ITokenLifetimeValidator tokenLifetimeValidator,
            ITokenService tokenService,
            IAuthenticationService authenticationService,
            WhiteList whiteList
        ) : base(loggerFactory)
        {
            _scopeFactory = scopeFactory;
            _options = optionsAccessor.Value;
            _contextAccessor = contextAccessor;
            _securityStampValidator = securityStampValidator;
            _tokenLifetimeValidator = tokenLifetimeValidator;
            _tokenService = tokenService;
            _authenticationService = authenticationService;
            _whiteList = whiteList;
        }

        /// <inheritdoc />
        protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(10);

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
                // On récupère l'identité de l'utilisateur, tout en validant le token d'accès
                try
                {
                    ClaimsPrincipal principal = _tokenService.ValidateToken(accessToken);
                    // On vérifie que l'utilisateur existe toujours
                    IdentityUser? user = await _authenticationService.FindUserByNameAsync(
                        principal.Identity?.Name ?? string.Empty
                    );
                    if (user == null)
                        return AnonymousUser;

                    // On vérifie que le refresh token est bien autorisé
                    if (!_whiteList.TryGetValue(refreshToken, out _))
                        return AnonymousUser;

                    // On vérifie que le refresh token correspond bien à l'access token
                    if (
                        principal.Claims
                            .FirstOrDefault(_ => _.Type == JwtRegisteredClaimNames.Jti)
                            ?.Value == refreshToken
                    )
                    {
                        // On crée l'identité de l'utilisateur
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
                        // On définit l'utilisateur comme connecté au circuit Blazor
                        SetAuthenticationState(Task.FromResult(new AuthenticationState(principal)));
                        return await base.GetAuthenticationStateAsync();
                    }
                    else
                    {
                        return AnonymousUser;
                    }
                }
                catch
                {
                    return AnonymousUser;
                }
            }
            return AnonymousUser;
        }

        /// <value>
        /// Un utilisateur anonyme.
        /// </value>
        private AuthenticationState AnonymousUser => new(new ClaimsPrincipal(new ClaimsIdentity()));

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
                bool securityStampIsValid =
                    await _securityStampValidator.ValidateSecurityStampAsync(
                        userManager,
                        authenticationState.User,
                        _options
                    );
                if (!securityStampIsValid)
                    return false;
                bool tokenLifetimeIsValid =
                    await _tokenLifetimeValidator.ValidateTokenLifetimeAsync(
                        authenticationState.User
                    );
                if (!tokenLifetimeIsValid)
                    return false;
                return true;
            }
            catch
            {
                return false;
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
    }
}
