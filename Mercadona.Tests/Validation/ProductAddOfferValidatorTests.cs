using FluentAssertions;
using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;
using Shouldly;

namespace Mercadona.Tests.Validation;

public class ProductAddOfferValidatorTests
{
    private readonly ProductAddOfferValidator _productAddOfferValidator;

    public ProductAddOfferValidatorTests()
    {
        _productAddOfferValidator = new ProductAddOfferValidator();
    }

    [Fact]
    public async Task ProductAddOfferValidator_NoExistingOffer_ShouldValidate_Async()
    {
        // Arrange
        Offer newOffer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 50
            };

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
        ValidationResult result = await _productAddOfferValidator.ValidateAsync(
            (product, newOffer)
        );

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ProductAddOfferValidator_OfferAlreadyExists_ShouldNotValidate_Async()
    {
        // Arrange
        Offer newOffer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 50
            };

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
        product.Offers.Add(
            new Offer
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Percentage = 10
            }
        );

        // Act
        ValidationResult result = await _productAddOfferValidator.ValidateAsync(
            (product, newOffer)
        );

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors
            .First()
            .ErrorMessage.Should()
            .Be(ProductAddOfferValidator.OFFER_ALREADY_EXISTS);
    }

    [Fact]
    public async Task ProductAddOfferValidator_ManyOfferAlreadyExists_ShouldReturnAllConflictedOffers_Async()
    {
        // Arrange
        Offer newOffer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 50
            };

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
        Offer offer1 =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                Percentage = 10
            };
        Offer offer2 =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                Percentage = 10
            };
        product.Offers.Add(offer1);
        product.Offers.Add(offer2);

        // Act
        ValidationResult result = await _productAddOfferValidator.ValidateAsync(
            (product, newOffer)
        );

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.ShouldSatisfyAllConditions(
            errors => errors.Count.Should().Be(2),
            errors =>
                errors
                    .First()
                    .ShouldSatisfyAllConditions(
                        validationFailure =>
                            validationFailure.ErrorMessage
                                .Should()
                                .Be(ProductAddOfferValidator.OFFER_ALREADY_EXISTS),
                        validationFailure => validationFailure.AttemptedValue.Should().Be(offer1)
                    ),
            errors =>
                errors
                    .Last()
                    .ShouldSatisfyAllConditions(
                        validationFailure =>
                            validationFailure.ErrorMessage
                                .Should()
                                .Be(ProductAddOfferValidator.OFFER_ALREADY_EXISTS),
                        validationFailure => validationFailure.AttemptedValue.Should().Be(offer2)
                    )
        );
    }
}
