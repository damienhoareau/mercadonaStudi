namespace Mercadona.Backend.Models;

/// <summary>
/// Représente un utilisateur voulant se connecter ou s'enregistrer
/// </summary>
public class UserModel
{
    /// <summary>
    /// Identifiant de l'utilisateur (Email)
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Mot de passe de l'utilisateur
    /// </summary>
    public string Password { get; set; } = string.Empty;
}
