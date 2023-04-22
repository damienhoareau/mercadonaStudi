using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;
using Shouldly;

namespace Mercadona.Tests.Validation
{
    public class ProductValidatorTests
    {
        private readonly ProductValidator _productValidator;

        public ProductValidatorTests()
        {
            _productValidator = new ProductValidator();
        }

        [Fact]
        // Ce produit a bien un nom !
        public void ProductValidator_InvalidLabel_Empty()
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
                    Label = string.Empty,
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.LABEL_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien un nom !
        public void ProductValidator_InvalidLabel_WhiteSpaces()
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
                    Label = "       ",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.LABEL_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une description !
        public void ProductValidator_InvalidDescription_Empty()
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
                    Description = string.Empty,
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.DESCRIPTION_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une description !
        public void ProductValidator_InvalidDescription_WhiteSpaces()
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
                    Description = "       ",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.DESCRIPTION_NOT_EMPTY);
        }

        [Fact]
        // On ne fait pas de dons.
        public void ProductValidator_InvalidPrice()
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
                    Price = 0M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.PRICE_GREATER_THAN_0);
        }

        [Fact]
        // Il ne faut pas se laisser duper par une extension.
        public void ProductValidator_InvalidImage()
        {
            // Arrange
            Product product =
                new(
                    () =>
                        File.Open(
                            "./Resources/notAImage.jpeg",
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite
                        )
                )
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.IMAGE_IS_VALID);
        }

        [Fact]
        // Ce produit a bien une catégorie !
        public void ProductValidator_InvalidCategory_Empty()
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
                    Price = 1M,
                    Category = string.Empty
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.CATEGORY_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une catégorie !
        public void ProductValidator_InvalidCategory_WhiteSpaces()
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
                    Price = 1M,
                    Category = "       "
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(ProductValidator.CATEGORY_NOT_EMPTY);
        }

        [Fact]
        public void ProductValidator_Valid()
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
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Act
            ValidationResult result = _productValidator.Validate(product);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.Errors.ShouldBeEmpty();
        }
    }
}
