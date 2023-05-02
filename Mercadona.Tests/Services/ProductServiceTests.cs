using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Mercadona.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using MimeDetective;
using Shouldly;

namespace Mercadona.Tests.Services
{
    public class ProductServiceTests : IClassFixture<ApplicationDbContextFixture>, IAsyncLifetime
    {
        private readonly ApplicationDbContextFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly IProductService _productService;

        public ProductServiceTests(ApplicationDbContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddSingleton<IValidator<Product>, ProductValidator>();
                services.AddSingleton(
                    new ContentInspectorBuilder()
                    {
                        Definitions = MimeDetective.Definitions.Default.FileTypes.Images.All()
                    }.Build()
                );
                services.AddSingleton<IProductService, ProductService>();

                return services;
            });

            _dbContext = _fixture.GetRequiredService<ApplicationDbContext>();
            _productService = _fixture.GetRequiredService<IProductService>();
        }

        public Task InitializeAsync()
        {
            _dbContext.ChangeTracker.Clear();
            return _fixture.ResetDbAsync();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        [Fact]
        public async Task EFQueriesCanSetImage()
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
            long imageStreamLength = product.ImageStream.Length;
            await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            byte[] data = await _dbContext.Products.Select(_ => _.Image).FirstAsync();
            data.LongLength.Should().Be(imageStreamLength);
        }

        [Fact]
        public async Task EFQueriesDoNotGetImage()
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
            await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            Product dbProduct = await _dbContext.Products
                .Select(
                    _ =>
                        new Product
                        {
                            Id = _.Id,
                            Label = _.Label,
                            Description = _.Description,
                            Price = _.Price,
                            Category = _.Category
                        }
                )
                .FirstAsync();
            dbProduct.Image.LongLength.Should().Be(0);
        }

        [Fact]
        public async Task GetAllAsync_GetAllSorted()
        {
            // Arrange
            Product product3 =
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
                    Label = "Mon produit 3",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };
            Product product2 =
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
                    Label = "Mon produit 2",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };
            Product product1 =
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
                    Label = "Mon produit 1",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };
            await _dbContext.AddAsync(product3);
            await _dbContext.AddAsync(product2);
            await _dbContext.AddAsync(product1);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<Product> result = (await _productService.GetAllAsync()).ToList();

            // Assert
            result.Count.Should().Be(3);
            result[0]
                .Should()
                .BeEquivalentTo(
                    product1,
                    option => option.Excluding(_ => _.Image).Excluding(_ => _.ImageStream)
                );
            result[1]
                .Should()
                .BeEquivalentTo(
                    product2,
                    option => option.Excluding(_ => _.Image).Excluding(_ => _.ImageStream)
                );
            result[2]
                .Should()
                .BeEquivalentTo(
                    product3,
                    option => option.Excluding(_ => _.Image).Excluding(_ => _.ImageStream)
                );
        }

        [Fact]
        public async Task AddProductAsync_InvalidProduct_ThrowsValidationException()
        {
            // Arrange
            Product invalidProduct =
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
            // Assert
            ValidationException exception = await _productService
                .AddProductAsync(invalidProduct)
                .ShouldThrowAsync<ValidationException>();
            exception.Errors.First().ErrorMessage.Should().Be(ProductValidator.LABEL_NOT_EMPTY);
        }

        [Fact]
        public async Task AddProductAsync_ValidProduct_ReturnsAddedProduct()
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
            Product resultProduct = await _productService.AddProductAsync(product);

            // Assert
            resultProduct.Should().BeEquivalentTo(product);
            List<Product> products = await _dbContext.Products
                .Select(
                    _ =>
                        new Product
                        {
                            Id = _.Id,
                            Label = _.Label,
                            Description = _.Description,
                            Price = _.Price,
                            Category = _.Category
                        }
                )
                .ToListAsync();
            products.Count.Should().Be(1);
            products
                .First()
                .Should()
                .BeEquivalentTo(
                    resultProduct,
                    option => option.Excluding(_ => _.Image).Excluding(_ => _.ImageStream)
                );
        }

        [Fact]
        public async Task GetImageAsync_NotExists_ReturnsNull()
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
            EntityEntry<Product> productEntry = await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            Stream? result = await _productService.GetImageAsync(Guid.NewGuid());

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetImageAsync_Exists_ReturnsCorrespondingStream()
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
            long streamSize = product.Image.LongLength;
            EntityEntry<Product> productEntry = await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            Stream? result = await _productService.GetImageAsync(productEntry.Entity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Length.Should().Be(streamSize);
        }
    }
}
