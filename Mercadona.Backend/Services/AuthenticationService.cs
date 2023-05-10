﻿using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Mercadona.Backend.Services.Interfaces
{
    /// <summary>
    /// Service permettant de gérer l'authentification
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ITokenService _tokenService;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="AuthenticationService"/>.
        /// </summary>
        /// <param name="userManager">Manager des utilisateurs.</param>
        /// <param name="tokenService">Service de gestion des jetons JWT.</param>
        public AuthenticationService(
            UserManager<IdentityUser> userManager,
            ITokenService tokenService
        )
        {
            _userManager = userManager;
            _tokenService = tokenService;
        }

        /// <inheritdoc/>
        public async Task<IdentityUser?> FindUserByNameAsync(string username)
        {
            IdentityUser? user = await _userManager.FindByNameAsync(username);
            return user;
        }

        /// <inheritdoc/>
        public Task<bool> CheckPasswordAsync(IdentityUser user, string password)
        {
            return _userManager.CheckPasswordAsync(user, password);
        }

        /// <inheritdoc/>
        public Task<(string refreshToken, string accessToken)> LoginAsync(IdentityUser user)
        {
            return Task.Run(() =>
            {
                string refreshToken = _tokenService.GenerateRefreshToken();
                List<Claim> authClaims =
                    new()
                    {
                        new Claim(ClaimTypes.Name, user.UserName!),
                        new Claim(JwtRegisteredClaimNames.Jti, refreshToken),
                    };

                string accessToken = _tokenService.GenerateAccessToken(refreshToken, authClaims);

                return (refreshToken, accessToken);
            });
        }

        /// <inheritdoc/>
        public async Task<bool> RegisterAsync(string username, string password)
        {
            IdentityUser user =
                new()
                {
                    Email = username,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = username
                };
            IdentityResult result = await _userManager.CreateAsync(user, password);
            return result.Succeeded;
        }

        /// <inheritdoc/>
        public Task LogoutAsync(string refreshToken)
        {
            return Task.Run(() =>
            {
                _tokenService.RevokeRefreshToken(refreshToken);
            });
        }
    }
}
