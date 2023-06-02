using Mercadona.Backend.Models;

namespace Mercadona.Backend.Services.Interfaces;

/// <summary>
/// Interface d'un provider permettant d'obtenir ou définir l'utilisateur connecté
/// </summary>
public interface IConnectedUserProvider
{
    /// <summary>
    /// Obtient ou définit l'utilisateur connecté.
    /// </summary>
    ConnectedUser? ConnectedUser { get; set; }
}
