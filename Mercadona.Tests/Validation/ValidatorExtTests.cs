using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Validation;

namespace Mercadona.Tests.Validation
{
    public class ValidatorExtTests
    {
        class TestClass
        {
            public int Id { get; set; }
        }

        class TestClassValidator : AbstractValidator<TestClass> { }

        class TestClassWithErrorValidator : AbstractValidator<TestClass>
        {
            public TestClassWithErrorValidator()
            {
                RuleFor(_ => _.Id).GreaterThan(0).WithMessage("Error");
            }
        }

        [Fact]
        public async Task ValidateValue_IsValid_ShouldReturnEmptyArray()
        {
            // Arrange
            TestClassValidator validator = new();

            // Act
            IEnumerable<string> result = await validator
                .ValidateValue()
                .Invoke(new TestClass(), nameof(TestClass.Id));

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateValue_HasError_ShouldReturnError()
        {
            // Arrange
            TestClassWithErrorValidator validator = new();

            // Act
            IEnumerable<string> result = await validator
                .ValidateValue()
                .Invoke(new TestClass(), nameof(TestClass.Id));

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain("Error");
        }
    }
}
