using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using IAuthenticationService = Mercadona.Backend.Services.Interfaces.IAuthenticationService;

namespace Mercadona.Backend.Controllers
{
    /// <summary>
    /// Controlleur gérant les connexions à l'application
    /// </summary>
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
        public static string USER_ALREADY_EXISTS(string username) =>
            $"L'utilisateur {username} existe déjà!";

        public const string USER_CREATION_FAILED =
            "La création de l'utilisateur a échoué! Veuillez réessayer.";

        public const string REFRESH_TOKEN_NOT_FOUND =
            "Le jeton de renouvellement n'a pas été trouvé dans la session.";

        public const string USERNAME_OR_PASSWORD_IS_NOT_VALID =
            "L'identifiant ou le mot de passe est incorrect.";
#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

        private readonly IAuthenticationService _authenticationService;
        private readonly IValidator<UserModel> _userModelValidator;

        /// <summary>
        /// Controlleur gérant les connexions à l'application
        /// </summary>
        /// <param name="authenticationService">Service d'authentification</param>
        /// <param name="userModelValidator">Validateur d'UserModel</param>
        public AuthenticateController(
            IAuthenticationService authenticationService,
            IValidator<UserModel> userModelValidator
        )
        {
            _authenticationService = authenticationService;
            _userModelValidator = userModelValidator;
        }

        /// <summary>
        /// Permet de connecter un utilisateur
        /// </summary>
        /// <param name="model">Modèle représentant l'utilisateur</param>
        /// <returns></returns>
        /// <response code="200">Si l'authentification a réussi.</response>
        /// <response code="401">Si l'identifiant ou le mot de passe n'est pas correct.</response>
        [HttpPost]
        [Route("account/login")]
        [ProducesResponseType(typeof(ConnectedUser), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TextResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginAsync([FromBody] UserModel model)
        {
            IdentityUser? user = await _authenticationService.FindUserByNameAsync(model.Username);
            if (
                user != null
                && await _authenticationService.CheckPasswordAsync(user, model.Password)
            )
            {
                (string refreshToken, string accessToken) = await _authenticationService.LoginAsync(
                    user
                );

                HttpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, refreshToken);

                return Ok(
                    new ConnectedUser
                    {
                        UserName = model.Username,
                        RefreshToken = refreshToken,
                        AccessToken = accessToken
                    }
                );
            }
            return Unauthorized(new TextResponse(USERNAME_OR_PASSWORD_IS_NOT_VALID));
        }

        /// <summary>
        /// Permet de prolonger la session d'un utilisateur
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Si l'authentification a réussi.</response>
        /// <response code="500">Si la prolongation de la session de l'utilisateur a échoué.</response>
        [HttpPost]
        [Route("account/refreshToken")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            string? refreshToken = HttpContext.Session.GetString(TokenService.REFRESH_TOKEN_NAME);
            if (refreshToken != null)
            {
                string accessToken = await _authenticationService.RefreshTokenAsync(refreshToken);

                HttpContext.Session.SetString(TokenService.ACCESS_TOKEN_NAME, accessToken);

                return Ok();
            }
            return Problem(
                REFRESH_TOKEN_NOT_FOUND,
                statusCode: StatusCodes.Status500InternalServerError
            );
        }

        /// <summary>
        /// Permet de stocker les jetons d'authentification Blazor dans un cookie
        /// </summary>
        /// <param name="user">Modèle représentant l'utilisateur connecté</param>
        /// <returns></returns>
        /// <response code="200">Si l'action s'est bien déroulée.</response>
        [HttpPost]
        [Route("account/storeCookie")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> StoreCookieAsync([FromBody] ConnectedUser user)
        {
            await Task.Run(() =>
            {
                HttpContext.Session.SetString(TokenService.REFRESH_TOKEN_NAME, user.RefreshToken);
                HttpContext.Session.SetString(TokenService.ACCESS_TOKEN_NAME, user.AccessToken);
            });

            return Ok();
        }

        /// <summary>
        /// Permet de supprimer les jetons d'authentification Blazor des cookies
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Si l'action s'est bien déroulée.</response>
        [HttpPost]
        [Route("account/clearCookie")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> ClearCookieAsync()
        {
            await Task.Run(() =>
            {
                HttpContext.Session.Clear();
            });

            return Ok();
        }

        /// <summary>
        /// Permet d'enregistrer un utilisateur
        /// </summary>
        /// <param name="model">Modèle représentant l'utilisateur</param>
        /// <returns></returns>
        /// <response code="200">Si l'enregistrement a réussi.</response>
        /// <response code="400">Si l'UserModel model n'est pas valide.</response>
        /// <response code="500">Si l'utilisateur existe déjà ou que la création de l'utilisateur a échoué.</response>
        [HttpPost]
        [Route("account/register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RegisterAsync([FromBody] UserModel model)
        {
            ValidationResult userModelValidationResult = await _userModelValidator.ValidateAsync(
                model,
                HttpContext.RequestAborted
            );
            if (!userModelValidationResult.IsValid)
                return Problem(
                    string.Join("\n", userModelValidationResult.Errors.Select(_ => _.ErrorMessage)),
                    statusCode: StatusCodes.Status400BadRequest
                );
            IdentityUser? userExists = await _authenticationService.FindUserByNameAsync(
                model.Username
            );
            if (userExists != null)
                return Problem(
                    USER_ALREADY_EXISTS(model.Username),
                    statusCode: StatusCodes.Status500InternalServerError
                );

            bool result = await _authenticationService.RegisterAsync(
                model.Username,
                model.Password
            );
            if (!result)
                return Problem(
                    USER_CREATION_FAILED,
                    statusCode: StatusCodes.Status500InternalServerError
                );

            return Ok();
        }

        /// <summary>
        /// Permet de déconnecter un utilisateur
        /// </summary>
        /// <returns></returns>
        /// <response code="200">Si la déconnexion a réussi.</response>
        /// <response code="500">Si la déconnexion de l'utilisateur a échoué.</response>
        [Authorize]
        [HttpPost]
        [Route("account/logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LogoutAsync()
        {
            try
            {
                string? refreshToken = HttpContext.Session.GetString(
                    TokenService.REFRESH_TOKEN_NAME
                );
                if (refreshToken == null)
                    return Problem(
                        REFRESH_TOKEN_NOT_FOUND,
                        statusCode: StatusCodes.Status500InternalServerError
                    );
                await _authenticationService.LogoutAsync(refreshToken);
                HttpContext.Session.Remove(TokenService.REFRESH_TOKEN_NAME);
                return Ok();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
