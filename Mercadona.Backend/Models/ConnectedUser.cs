namespace Mercadona.Backend.Models;

/// <summary>
/// Représente un utilisateur connecté
/// </summary>
public class ConnectedUser
{
    /// <value>
    /// Identifiant de l'utilisateur.
    /// </value>
    public string UserName { get; set; } = string.Empty;

    /// <value>
    /// Jeton de renouvellement de l'utilisateur.
    /// </value>
    public string RefreshToken { get; set; } = string.Empty;

    /// <value>
    /// Jeton d'authentification de l'utilisateur.
    /// </value>
    public string AccessToken { get; set; } = string.Empty;
}
