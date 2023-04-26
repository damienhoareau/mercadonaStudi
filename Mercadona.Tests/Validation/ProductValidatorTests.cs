using FluentAssertions;
using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;
using MimeDetective;

namespace Mercadona.Tests.Validation
{
    public class ProductValidatorTests
    {
        private readonly ProductValidator _productValidator;

        public ProductValidatorTests()
        {
            _productValidator = new ProductValidator(
                new ContentInspectorBuilder()
                {
                    Definitions = MimeDetective.Definitions.Default.FileTypes.Images.All()
                }.Build()
            );
        }

        [Fact]
        // Ce produit a bien un nom !
        public async Task ProductValidator_InvalidLabel_Empty_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.LABEL_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien un nom !
        public async Task ProductValidator_InvalidLabel_WhiteSpaces_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.LABEL_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une description !
        public async Task ProductValidator_InvalidDescription_Empty_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.DESCRIPTION_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une description !
        public async Task ProductValidator_InvalidDescription_WhiteSpaces_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.DESCRIPTION_NOT_EMPTY);
        }

        [Fact]
        // On ne fait pas de dons.
        public async Task ProductValidator_InvalidPrice_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.PRICE_GREATER_THAN_0);
        }

        [Fact]
        // Il ne faut pas se laisser duper par une extension.
        public async Task ProductValidator_InvalidImage_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.IMAGE_IS_VALID);
        }

        [Fact]
        // Ce produit a bien une catégorie !
        public async Task ProductValidator_InvalidCategory_Empty_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.CATEGORY_NOT_EMPTY);
        }

        [Fact]
        // Ce produit a bien une catégorie !
        public async Task ProductValidator_InvalidCategory_WhiteSpaces_ShouldNotValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(ProductValidator.CATEGORY_NOT_EMPTY);
        }

        [Fact]
        public async Task ProductValidator_Valid_ShouldValidate()
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
            ValidationResult result = await _productValidator.ValidateAsync(product);

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }
    }
}
