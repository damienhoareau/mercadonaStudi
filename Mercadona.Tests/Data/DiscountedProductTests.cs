using FluentAssertions;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Shouldly;

namespace Mercadona.Tests.Data
{
    public class DiscountedProductTests
    {
        [Fact]
        public void CreateDiscountedProductWithoutOffer_ShouldKeepPriceAsDiscountedPrice()
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
                p => p.Id.Should().Be(product.Id),
                p => p.Label.Should().Be(product.Label),
                p => p.Description.Should().Be(product.Description),
                p => p.Price.Should().Be(product.Price),
                p => p.Category.Should().Be(product.Category),
                p => p.DiscountedPrice.Should().Be(product.Price)
            );
        }

        [Fact]
        public void CreateDiscountedProductWithOffer_ShouldApplyPercentageToComputeDiscountedPrice()
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
                    Percentage = 20
                };

            // Act
            DiscountedProduct discountedProduct = new(product, offer);
            decimal discountedProductPrice = discountedProduct.DiscountedPrice;

            // Assert
            discountedProduct.ShouldSatisfyAllConditions(
                p => p.Id.Should().Be(product.Id),
                p => p.Label.Should().Be(product.Label),
                p => p.Description.Should().Be(product.Description),
                p => p.Price.Should().Be(product.Price),
                p => p.Category.Should().Be(product.Category),
                p => p.DiscountedPrice.Should().Be(80M)
            );
        }
    }
}
