using FluentAssertions;
using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Validation;

namespace Mercadona.Tests.Validation
{
    public class NewOfferValidatorTests
    {
        private readonly NewOfferValidator _newOfferValidator;

        public NewOfferValidatorTests()
        {
            _newOfferValidator = new NewOfferValidator();
        }

        [Fact]
        public async Task NewOfferValidator_NewOfferOutdated_ShouldNotValidate()
        {
            // Arrange
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
                    Percentage = 0.5M
                };

            // Act
            ValidationResult result = await _newOfferValidator.ValidateAsync(newOffer);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.First().ErrorMessage.Should().Be(NewOfferValidator.NEW_OFFER_IS_OUTDATED);
        }

        [Fact]
        public async Task NewOfferValidator_ValidNewOffer_ShouldValidate()
        {
            // Arrange
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 0.5M
                };

            // Act
            ValidationResult result = await _newOfferValidator.ValidateAsync(newOffer);

            // Assert
            result.IsValid.Should().BeTrue();
        }
    }
}
