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
    public class ProductControllerTests
    {
        private readonly Product _item;
        private readonly IEnumerable<Product> _items;
        private readonly IEnumerable<Product> _emptyItems = new List<Product>();

        public ProductControllerTests()
        {
            _item = new(
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
            _items = new List<Product> { _item };
        }

        [Fact]
        public async Task GetAllAsync_ResultContainsAtLeastOneItem_ShouldReturnOK_WithCorrectItems()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_items);
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeActionResult<OkObjectResult, IEnumerable<Product>>(_items);
        }

        [Fact]
        public async Task GetAllAsync_ResultHasNoItem_ShouldReturnNoContent()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ReturnsAsync(_emptyItems);
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
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
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.GetAllAsync(It.IsAny<CancellationToken>()))
                        .ThrowsAsync(new Exception("Test"));
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.GetAllAsync();

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task GetImageAsync_DataFound_ShouldReturnOK_WithData()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(
                            _ => _.GetImageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
                        )
                        .ReturnsAsync(_item.ImageStream);
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.GetImageAsync(Guid.NewGuid());

            // Assert
            result
                .Should()
                .BeActionResult<FileStreamResult>(typedResult =>
                {
                    MemoryStream memoryStream = new();
                    typedResult.FileStream.CopyTo(memoryStream);
                    memoryStream.Length.Should().Be(_item.ImageStream.Length);
                    memoryStream.ToArray().Should().BeEquivalentTo(_item.Image);
                });
        }

        [Fact]
        public async Task GetImageAsync_NoDataFound_ShouldReturnNotFound()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(
                            _ => _.GetImageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
                        )
                        .ReturnsAsync((Stream?)null);
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.GetImageAsync(Guid.NewGuid());

            // Assert
            result.Should().BeActionResult<NotFoundResult>();
        }

        [Fact]
        public async Task GetImageAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(
                            _ => _.GetImageAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
                        )
                        .ThrowsAsync(new Exception("Test"));
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.GetImageAsync(Guid.NewGuid());

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }

        [Fact]
        public async Task AddProductAsync_NoException_ShouldReturnCreated()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.AddProductAsync(It.IsAny<Product>())).ReturnsAsync(_item);
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.AddProductAsync(_item);

            // Assert
            result.Should().BeActionResult<CreatedResult, Product>(_item);
        }

        [Fact]
        public async Task AddProductAsync_ValidationExceptionThrown_ShouldReturnProblem_BadRequest()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.AddProductAsync(It.IsAny<Product>()))
                        .ThrowsAsync(new ValidationException("Test"));
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.AddProductAsync(_item);

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status400BadRequest, "Test");
        }

        [Fact]
        public async Task AddProductAsync_ExceptionThrown_ShouldReturnProblem_InternalServerError()
        {
            // Arrange
            Mock<IProductService> mockProductService =
                TestsHelper.GetServiceMock<IProductService>(mock =>
                {
                    mock.Setup(_ => _.AddProductAsync(It.IsAny<Product>()))
                        .ThrowsAsync(new Exception("Test"));
                });
            ProductController controller = TestsHelper.CreateController<ProductController>(
                mockProductService.Object,
                TestsHelper.ContentInspector
            );

            // Act
            IActionResult result = await controller.AddProductAsync(_item);

            // Assert
            result.Should().BeProblemResult(StatusCodes.Status500InternalServerError, "Test");
        }
    }
}
