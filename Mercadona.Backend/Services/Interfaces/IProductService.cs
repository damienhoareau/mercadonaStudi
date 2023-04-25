using Mercadona.Backend.Data;

namespace Mercadona.Backend.Services.Interfaces
{
    public interface IProductService
    {
        /// <summary>
        /// Recupère la liste des produits
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère le flux des données de l'image d'un produit
        /// </summary>
        /// <param name="productId">Identifiant du <seealso cref="Product"/></param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Flux du produit</returns>
        Task<Stream?> GetImageAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ajoute un produit
        /// </summary>
        /// <param name="product"><seealso cref="Product"/> à ajouter</param>
        /// <returns><seealso cref="Product"/> ajouté</returns>
        Task<Product> AddProductAsync(Product product);
    }
}
