using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;

namespace Mercadona.Backend.Services.Interfaces;

/// <summary>
/// Interface d'un service permettant d'inter-agir avec des <seealso cref="DiscountedProduct"/>
/// </summary>
public interface IDiscountedProductService
{
    /// <summary>
    /// Recupère la liste des produits (sans distinction de promotion en cours)
    /// </summary>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Liste de <seealso cref="DiscountedProduct"/></returns>
    Task<IEnumerable<DiscountedProduct>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Recupère la liste des produits avec une promotion en cours
    /// </summary>
    /// <param name="cancellationToken">Token d'annulation</param>
    /// <returns>Liste de <seealso cref="DiscountedProduct"/></returns>
    Task<IEnumerable<DiscountedProduct>> GetAllDiscountedAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Applique une promotion à un produit
    /// </summary>
    /// <param name="productId">Identifiant du <seealso cref="Product"/> à remiser</param>
    /// <param name="offer"><seealso cref="Offer"/> à appliquer</param>
    /// <param name="forceReplace">Force à remplacer la promotion en cours si elle existe</param>
    /// <exception cref="ValidationException"/>
    /// <returns>
    /// <seealso cref="DiscountedProduct"/> correspondant au <seealso cref="Product"/> remisé<br/>
    /// <seealso cref="ValidationException"/> : Si l'offre n'est pas valide<br/>
    /// <seealso cref="ValidationException"/> : Si une offre est à cheval sur celle qu'on veut appliquer
    /// </returns>
    Task<DiscountedProduct> ApplyOfferAsync(Guid productId, Offer offer, bool forceReplace = false);
}
