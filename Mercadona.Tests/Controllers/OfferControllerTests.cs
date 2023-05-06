using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Controllers;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Tests.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Mercadona.Tests.Controllers
{
    public class OfferControllerTests
    {
        private readonly Offer _item;
        private readonly IEnumerable<Offer> _items;
        private readonly IEnumerable<Offer> _emptyItems = new List<Offer>();

        public OfferControllerTests()
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            _item = new()
            {
                StartDate = today,
                EndDate = today,
                Percentage = 50
            };
            _items = new List<Offer> { _item };
        }

        [Fact]
        public async Task GetAllAsync_ResultContainsAtLeastOneItem_ShouldReturnOK_WithCorrectItems()
        {
            // Arrange
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock =>
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_items)
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
            );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeActionResult<OkObjectResult, IEnumerable<Offer>>(_items);
        }

        [Fact]
        public async Task GetAllAsync_ResultHasNoItem_ShouldReturnNoContent()
        {
            // Arrange
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock =>
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_emptyItems)
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
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
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock =>
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Test"))
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
            );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task AddOfferAsync_NoException_ShouldReturnCreated()
        {
            // Arrange
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock => mock.Setup(_ => _.AddOfferAsync(It.IsAny<Offer>())).ReturnsAsync(_item)
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
            );

            // Act
            IActionResult result = await controller.AddOfferAsync(_item);

            // Assert
            result.Should().BeActionResult<CreatedResult, Offer>(_item);
        }

        [Fact]
        public async Task AddOfferAsync_ValidationExceptionThrown_ShouldReturnProblem_BadRequest()
        {
            // Arrange
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock =>
                    mock.Setup(_ => _.AddOfferAsync(It.IsAny<Offer>()))
                        .ThrowsAsync(new ValidationException("Test"))
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
            );

            // Act
            IActionResult result = await controller.AddOfferAsync(_item);

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
        }

        [Fact]
        public async Task AddOfferAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IOfferService> mockOfferService = TestsHelper.GetServiceMock<IOfferService>(
                mock =>
                    mock.Setup(_ => _.AddOfferAsync(It.IsAny<Offer>()))
                        .ThrowsAsync(new Exception("Test"))
            );
            OfferController controller = TestsHelper.CreateController<OfferController>(
                mockOfferService.Object
            );

            // Act
            IActionResult result = await controller.AddOfferAsync(_item);

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }
    }
}
