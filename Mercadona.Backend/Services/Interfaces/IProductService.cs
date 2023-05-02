using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface d'un service permettant d'inter-agir avec des <seealso cref="Product"/>
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Recupère la liste des produits
        /// </summary>
        /// <returns>Liste de <seealso cref="Product"/></returns>
        Task<IEnumerable<Product>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Récupère le flux de donnée de l'image d'un produit
        /// </summary>
        /// <param name="productId">Identifiant du <seealso cref="Product"/></param>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>
        /// Donnée de l'image du produit sous forme de <see cref="Stream"/><br/>
        /// Null si le produit n'existe pas
        /// </returns>
        Task<Stream?> GetImageAsync(Guid productId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ajoute un produit
        /// </summary>
        /// <param name="product"><seealso cref="Product"/> à ajouter</param>
        /// <exception cref="ValidationException"/>
        /// <returns>
        /// <seealso cref="Product"/> ajouté<br/>
        /// <seealso cref="ValidationException"/> : Si le produit n'est pas valide
        /// </returns>
        Task<Product> AddProductAsync(Product product);
    }
}
