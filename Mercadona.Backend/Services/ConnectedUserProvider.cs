using Mercadona.Backend.Models;

namespace Mercadona.Backend.Services.Interfaces;

/// <summary>
/// Provider permettant d'obtenir ou définir l'utilisateur connecté
/// </summary>
public class ConnectedUserProvider : IConnectedUserProvider
{
    /// <inheritdoc/>
    public ConnectedUser? ConnectedUser { get; set; }
}
