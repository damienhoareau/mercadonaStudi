using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Models;
using Mercadona.Backend.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Mercadona.Backend.Controllers
{
    /// <summary>
    /// Controlleur gérant les connexions à l'application
    /// </summary>
    [ApiController]
    public class AuthenticateController : ControllerBase
    {
        public static string USER_ALREADY_EXISTS(string username) =>
            $"L'utilisateur {username} existe déjà!";

        public const string USER_CREATION_FAILED =
            "La création de l'utilisateur a échoué! Veuillez réessayer.";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly JWTOptions _jwtOptions;
        private readonly IValidator<UserModel> _userModelValidator;

        /// <summary>
        /// Controlleur gérant les connexions à l'application
        /// </summary>
        /// <param name="userManager">Manager des utilisateurs</param>
        /// <param name="jwtOptions">Configuration JWT</param>
        /// <param name="userModelValidator">Validateur d'UserModel</param>
        public AuthenticateController(
            UserManager<IdentityUser> userManager,
            IOptions<JWTOptions> jwtOptions,
            IValidator<UserModel> userModelValidator
        )
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions.Value;
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
        [ProducesResponseType(typeof(LoginResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LoginAsync([FromBody] UserModel model)
        {
            IdentityUser? user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                List<Claim> authClaims =
                    new()
                    {
                        new Claim(ClaimTypes.Name, user!.UserName),
                        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    };

                JwtSecurityToken token = GetToken(authClaims);

                return Ok(
                    new LoginResult
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        Expiration = token.ValidTo
                    }
                );
            }
            return Unauthorized();
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
            IdentityUser? userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return Problem(
                    USER_ALREADY_EXISTS(model.Username),
                    statusCode: StatusCodes.Status500InternalServerError
                );

            IdentityUser user =
                new()
                {
                    Email = model.Username,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = model.Username
                };
            IdentityResult result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
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
        /// <response code="401">Si l'identifiant ou le mot de passe n'est pas correct.</response>
        [Authorize]
        [HttpPost]
        [Route("account/logout")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> LogoutAsync()
        {
            // TODO : Il faut stocker les tokens dans un dictionnaire (userId, token)
            // Gérer les autorisations lorsque le token a été supprimé
            throw new NotImplementedException();
        }

        /// <summary>
        /// Génère un JWT
        /// </summary>
        /// <param name="authClaims">Les droits d'authentification.</param>
        /// <returns></returns>
        private JwtSecurityToken GetToken(List<Claim> authClaims)
        {
            SymmetricSecurityKey authSigningKey = new(Encoding.UTF8.GetBytes(_jwtOptions.Secret));

            JwtSecurityToken token =
                new(
                    issuer: _jwtOptions.ValidIssuer,
                    audience: _jwtOptions.ValidAudience,
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(
                        authSigningKey,
                        SecurityAlgorithms.HmacSha256
                    )
                );

            return token;
        }
    }
}
