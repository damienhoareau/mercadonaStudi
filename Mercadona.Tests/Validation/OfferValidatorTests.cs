using FluentAssertions;
using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;
using Shouldly;

namespace Mercadona.Tests.Validation;

public class OfferValidatorTests
{
    private readonly OfferValidator _offerValidator;

    public OfferValidatorTests()
    {
        _offerValidator = new OfferValidator();
    }

    [Fact]
    // On ne vit pas dans le passé.
    public async Task OfferValidator_InvalidStartDate_ShouldNotValidate_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                Percentage = 50
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors
            .First()
            .ErrorMessage.Should()
            .Be(OfferValidator.START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
    }

    [Fact]
    // Pas de voyage dans le temps.
    public async Task OfferValidator_InvalidEndDate_ShouldNotValidate_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                EndDate = DateOnly.FromDateTime(DateTime.Today),
                Percentage = 50
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors
            .First()
            .ErrorMessage.Should()
            .Be(OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE);
    }

    [Fact]
    // Ce n'est pas vraiment une promotion
    public async Task OfferValidator_InvalidPercentageMin_ShouldNotValidate_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 0
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorMessage.Should().Be(OfferValidator.PERCENTAGE_BETWEEN_0_AND_1);
    }

    [Fact]
    // Dans ce monde, rien n'est gratuit
    public async Task OfferValidator_InvalidPercentageMax_ShouldNotValidate_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 100
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors.First().ErrorMessage.Should().Be(OfferValidator.PERCENTAGE_BETWEEN_0_AND_1);
    }

    [Fact]
    public async Task OfferValidator_ManyInvalidFields_ShouldHasAllErrors_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                Percentage = 100
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.ShouldSatisfyAllConditions(
            errors => errors.Count.Should().Be(2),
            errors =>
                errors
                    .Should()
                    .Contain(
                        _ =>
                            _.ErrorMessage
                            == OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE
                    ),
            errors =>
                errors
                    .Should()
                    .Contain(_ => _.ErrorMessage == OfferValidator.PERCENTAGE_BETWEEN_0_AND_1)
        );
    }

    [Fact]
    public async Task OfferValidator_Valid_ShouldValidate_Async()
    {
        // Arrange
        Offer offer =
            new()
            {
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                Percentage = 20
            };

        // Act
        ValidationResult result = await _offerValidator.ValidateAsync(offer);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
}
