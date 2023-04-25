using Mercadona.Backend.Data;
using Mercadona.Backend.Models;

namespace Mercadona.Backend.Services.Interfaces
{
    public interface IDiscountedProductService
    {
        /// <summary>
        /// Recupère la liste des produits (sans distinction de promotion en cours)
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<DiscountedProduct>> GetAllAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Recupère la liste des produits avec une promotion en cours
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<DiscountedProduct>> GetAllDiscountedAsync(
            CancellationToken cancellationToken = default
        );

        /// <summary>
        /// Applique une promotion à un produit
        /// </summary>
        /// <param name="productId">Identifiant du <seealso cref="Product"/> à remiser</param>
        /// <param name="offer"><seealso cref="Offer"/> à appliquer</param>
        /// <param name="forceReplace">Force à remplacer la promotion en cours si elle existe</param>
        /// <returns><seealso cref="DiscountedProduct"/> correspondant au <seealso cref="Product"/> remisé</returns>
        Task<DiscountedProduct> ApplyOfferAsync(
            Guid productId,
            Offer offer,
            bool forceReplace = false
        );
    }
}
