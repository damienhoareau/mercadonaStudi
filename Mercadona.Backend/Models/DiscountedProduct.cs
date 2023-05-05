using Mercadona.Backend.Data;

namespace Mercadona.Backend.Models
{
    /// <summary>
    /// Représente un produit remisé (ou pas)
    /// </summary>
    public class DiscountedProduct
    {
        /// <summary>
        /// Construit un <seealso cref="DiscountedProduct"/> à partir d'un <seealso cref="Product"/> et d'une <seealso cref="Mercadona.Backend.Data.Offer"/>
        /// </summary>
        /// <param name="product">Produit de base</param>
        /// <param name="offer">Promotion appliquée</param>
        public DiscountedProduct(Product product, Offer? offer = null)
        {
            Id = product.Id;
            Label = product.Label;
            Description = product.Description;
            Price = product.Price;
            Category = product.Category;

            Offer = offer;
        }

        /// <summary>
        /// Identifiant du produit
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Libellé du produit
        /// </summary>
        public string Label { get; private set; } = string.Empty;

        /// <summary>
        /// Description du produit
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Prix du produit
        /// </summary>
        public decimal Price { get; private set; }

        /// <summary>
        /// Catégorie du produit
        /// </summary>
        public string Category { get; private set; } = string.Empty;

        /// <summary>
        /// Promotion appliquée au produit<br/>
        /// Peut être <c>null</c>
        /// </summary>
        public Offer? Offer { get; private set; }

        /// <summary>
        /// Prix du produit avec la promotion appliquée
        /// </summary>
        public decimal DiscountedPrice => Math.Round(Price * (1M - (Offer?.Percentage ?? 0M)), 2);
    }
}
