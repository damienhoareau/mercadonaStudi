using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Mercadona.Tests.Controllers
{
    public class DiscountedProductControllerTests
    {
        private readonly DiscountedProduct _item;
        private readonly IEnumerable<DiscountedProduct> _items;
        private readonly IEnumerable<DiscountedProduct> _emptyItems = new List<DiscountedProduct>();

        public DiscountedProductControllerTests()
        {
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
            _item = new(product);
            _items = new List<DiscountedProduct> { _item };
        }

        [Fact]
        public async Task GetAllAsync_ResultContainsAtLeastOneItem_ShouldReturnOK_WithCorrectItems()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_items);
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeActionResult<OkObjectResult, IEnumerable<DiscountedProduct>>(_items);
        }

        [Fact]
        public async Task GetAllAsync_ResultHasNoItem_ShouldReturnNoContent()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_emptyItems);
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeActionResult<NoContentResult>();
        }

        [Fact]
        public async Task GetAllAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Test"));
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task GetAllDiscountedAsync_ResultContainsAtLeastOneItem_ShouldReturnOK_WithCorrectItems()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllDiscountedAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_items);
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllDiscountedAsync();

            // Assert
            result.Should().BeActionResult<OkObjectResult, IEnumerable<DiscountedProduct>>(_items);
        }

        [Fact]
        public async Task GetAllDiscountedAsync_ResultHasNoItem_ShouldReturnNoContent()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllDiscountedAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_emptyItems);
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllDiscountedAsync();

            // Assert
            result.Should().BeActionResult<NoContentResult>();
        }

        [Fact]
        public async Task GetAllDiscountedAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllDiscountedAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Test"));
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.GetAllDiscountedAsync();

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task ApplyOfferAsync_NoException_ShouldReturnCreated()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(
                            _ =>
                                _.ApplyOfferAsync(
                                    It.IsAny<Guid>(),
                                    It.IsAny<Offer>(),
                                    It.IsAny<bool>()
                                )
                        )
                        .ReturnsAsync(_item);
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.ApplyOfferAsync(
                Guid.NewGuid(),
                new Offer(),
                false
            );

            // Assert
            result.Should().BeActionResult<CreatedResult, DiscountedProduct>(_item);
        }

        [Fact]
        public async Task ApplyOfferAsync_ValidationExceptionThrown_ShouldReturnProblem_BadRequest()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(
                            _ =>
                                _.ApplyOfferAsync(
                                    It.IsAny<Guid>(),
                                    It.IsAny<Offer>(),
                                    It.IsAny<bool>()
                                )
                        )
                        .ThrowsAsync(new ValidationException("Test"));
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.ApplyOfferAsync(
                Guid.NewGuid(),
                new Offer(),
                false
            );

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
        }

        [Fact]
        public async Task ApplyOfferAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IDiscountedProductService> mockDiscountedProductService =
                TestsHelper.GetServiceMock<IDiscountedProductService>(mock =>
                {
                    mock.Setup(
                            _ =>
                                _.ApplyOfferAsync(
                                    It.IsAny<Guid>(),
                                    It.IsAny<Offer>(),
                                    It.IsAny<bool>()
                                )
                        )
                        .ThrowsAsync(new Exception("Test"));
                });
            DiscountedProductController controller =
                TestsHelper.CreateController<DiscountedProductController>(
                    mockDiscountedProductService.Object
                );

            // Act
            IActionResult result = await controller.ApplyOfferAsync(
                Guid.NewGuid(),
                new Offer(),
                false
            );

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }
    }
}
