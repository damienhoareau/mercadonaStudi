using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;
using Shouldly;

namespace Mercadona.Tests.Validation
{
    public class OfferValidatorTests
    {
        private readonly OfferValidator _offerValidator;

        public OfferValidatorTests()
        {
            _offerValidator = new OfferValidator();
        }

        [Fact]
        // On ne vit pas dans le passé.
        public void OfferValidator_InvalidStartDate()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today),
                    Percentage = 0.5M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(OfferValidator.START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
        }

        [Fact]
        // Pas de voyage dans le temps.
        public void OfferValidator_InvalidEndDate()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today),
                    Percentage = 0.5M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
        }

        [Fact]
        // Ce n'est pas vraiment une promotion
        public void OfferValidator_InvalidPercentageMin()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 0M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(OfferValidator.PERCENTAGE_BETWEEN_0_AND_1);
        }

        [Fact]
        // Dans ce monde, rien n'est gratuit
        public void OfferValidator_InvalidPercentageMax()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 1M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors
                .ShouldHaveSingleItem()
                .ErrorMessage.ShouldBe(OfferValidator.PERCENTAGE_BETWEEN_0_AND_1);
        }

        [Fact]
        public void OfferValidator_ManyInvalidFields()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    Percentage = 1M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.Errors.ShouldSatisfyAllConditions(
                errors => errors.Count.ShouldBe(2),
                errors =>
                    errors.ShouldContain(
                        _ =>
                            _.ErrorMessage
                            == OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY
                    ),
                errors =>
                    errors.ShouldContain(
                        _ => _.ErrorMessage == OfferValidator.PERCENTAGE_BETWEEN_0_AND_1
                    )
            );
        }

        [Fact]
        public void OfferValidator_Valid()
        {
            // Arrange
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 0.2M
                };

            // Act
            ValidationResult result = _offerValidator.Validate(offer);

            // Assert
            result.IsValid.ShouldBeTrue();
            result.Errors.ShouldBeEmpty();
        }
    }
}
