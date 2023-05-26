using Microsoft.AspNetCore.Identity;

namespace Mercadona.Backend.Services.Interfaces;

/// <summary>
/// Interface d'un service permettant de gérer l'authentification
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Permet de récupérer un utilisateur
    /// </summary>
    /// <param name="username">Identitifiant de l'utilisateur</param>
    /// <returns>Modèle représentant l'utilisateur; <c>null</c> si non trouvé.</returns>
    Task<IdentityUser?> FindUserByNameAsync(string username);

    /// <summary>
    /// Permet de vérifier le mot de passe d'un utilisateur
    /// </summary>
    /// <param name="user">Utilisateur</param>
    /// <param name="password">Mot de passe à vérifier</param>
    /// <returns></returns>
    Task<bool> CheckPasswordAsync(IdentityUser user, string password);

    /// <summary>
    /// Permet de connecter un utilisateur
    /// </summary>
    /// <param name="user">Utilisateur</param>
    /// <returns>Les jetons de renouvellement et d'accès</returns>
    Task<(string refreshToken, string accessToken)> LoginAsync(IdentityUser user);

    /// <summary>
    /// Permet de prolonger la session d'un utilisateur
    /// </summary>
    /// <param name="refreshToken">Le jeton de renouvellement</param>
    /// <returns>Le nouveau jeton d'accès</returns>
    Task<string> RefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Permet d'enregistrer un utilisateur
    /// </summary>
    /// <param name="username">Identitifant de l'utilisateur</param>
    /// <param name="password">Mode de passe de l'utilisateur</param>
    /// <returns>Le résultat de l'opération d'enregistrement</returns>
    Task<bool> RegisterAsync(string username, string password);

    /// <summary>
    /// Permet de déconnecter un utilisateur
    /// </summary>
    /// <param name="refreshToken">Le jeton de renouvellment de l'utilisateur</param>
    /// <returns></returns>
    Task LogoutAsync(string refreshToken);
}
