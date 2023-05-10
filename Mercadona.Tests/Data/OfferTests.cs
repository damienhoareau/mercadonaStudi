using FluentAssertions;
using Mercadona.Backend.Data;

namespace Mercadona.Tests.Data
{
    public class OfferTests
    {
        [Fact]
        public void Equals_NotOffer_ShouldReturnFalse()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            object notOffer = new();

            // Act
            bool result = offer1.Equals(notOffer);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentStartDate_ShouldReturnFalse()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            Offer offer2 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };

            // Act
            bool result = offer1.Equals(offer2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentEndDate_ShouldReturnFalse()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            Offer offer2 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
                    Percentage = 20
                };

            // Act
            bool result = offer1.Equals(offer2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentPercentage_ShouldReturnFalse()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            Offer offer2 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 30
                };

            // Act
            bool result = offer1.Equals(offer2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_ShouldReturnTrue()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            Offer offer2 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };

            // Act
            bool result = offer1.Equals(offer2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_NotSameRefButSameValues_ShouldReturnSameHashCode()
        {
            // Arrange
            Offer offer1 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };
            Offer offer2 =
                new()
                {
                    StartDate = DateOnly.FromDateTime(DateTime.Today),
                    EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                    Percentage = 20
                };

            // Act
            int result1 = offer1.GetHashCode();
            int result2 = offer2.GetHashCode();

            // Assert
            result1.Should().Be(result2);
        }

        [Fact]
        public void ToString_AlwaysSameValue()
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
            string result = offer.ToString();

            // Assert
            result
                .Should()
                .Be(
                    string.Format(
                        "{0} -> {1} : 20%",
                        DateOnly.FromDateTime(DateTime.Today),
                        DateOnly.FromDateTime(DateTime.Today.AddDays(1))
                    )
                );
        }
    }
}
