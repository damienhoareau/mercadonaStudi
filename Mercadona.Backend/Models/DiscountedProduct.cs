using Mercadona.Backend.Data;

namespace Mercadona.Backend.Models
{
    public class DiscountedProduct
    {
        public DiscountedProduct(Product product, Offer? offer = null)
        {
            Id = product.Id;
            Label = product.Label;
            Description = product.Description;
            Price = product.Price;
            Category = product.Category;

            Offer = offer;
        }

        public Guid Id { get; private set; }
        public string Label { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public decimal Price { get; private set; }
        public string Category { get; private set; } = string.Empty;

        public Offer? Offer { get; private set; }

        public decimal DiscountedPrice => Price * (1M - (Offer?.Percentage ?? 0M));
    }
}
