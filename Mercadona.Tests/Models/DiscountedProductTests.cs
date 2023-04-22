using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Shouldly;

namespace Mercadona.Tests.Data
{
    public class DiscountedProductTests
    {
        [Fact]
        public void CreateDiscountedProductWithoutOffer()
        {
            // Arrange
            Product product =
                new(
                    () =>
                        File.Open(
                            "./Resources/validImage.jpeg",
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite
                        )
                )
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 100M,
                    Category = "Surgelé"
                };

            // Act
            DiscountedProduct discountedProduct = new(product);

            // Assert
            discountedProduct.ShouldSatisfyAllConditions(
                p => p.Id.ShouldBe(product.Id),
                p => p.Label.ShouldBe(product.Label),
                p => p.Description.ShouldBe(product.Description),
                p => p.Price.ShouldBe(product.Price),
                p => p.Category.ShouldBe(product.Category),
                p => p.DiscountedPrice.ShouldBe(product.Price)
            );
        }

        [Fact]
        public void CreateDiscountedProductWithOffer()
        {
            // Arrange
            // Produit de 100€
            Product product =
                new(
                    () =>
                        File.Open(
                            "./Resources/validImage.jpeg",
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite
                        )
                )
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 100M,
                    Category = "Surgelé"
                };
            // Promotion de 20%
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 0.2M
                };

            // Act
            DiscountedProduct discountedProduct = new(product, offer);
            decimal discountedProductPrice = discountedProduct.DiscountedPrice;

            // Assert
            discountedProduct.ShouldSatisfyAllConditions(
                p => p.Id.ShouldBe(product.Id),
                p => p.Label.ShouldBe(product.Label),
                p => p.Description.ShouldBe(product.Description),
                p => p.Price.ShouldBe(product.Price),
                p => p.Category.ShouldBe(product.Category),
                p => p.DiscountedPrice.ShouldBe(80M)
            );
        }
    }
}
