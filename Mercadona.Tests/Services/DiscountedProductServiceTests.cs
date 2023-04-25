using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Mercadona.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mercadona.Tests.Services
{
    public class DiscountedProductServiceTests
        : IClassFixture<ApplicationDbContextFixture>,
            IAsyncLifetime
    {
        private readonly ApplicationDbContextFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly IDiscountedProductService _discountedProductService;

        public DiscountedProductServiceTests(ApplicationDbContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddSingleton<OfferValidator>();
                services.AddSingleton<ProductAddOfferValidator>();
                services.AddSingleton<IOfferService, OfferService>();
                services.AddSingleton<IDiscountedProductService, DiscountedProductService>();

                return services;
            });

            _dbContext = _fixture.GetRequiredService<ApplicationDbContext>();
            _discountedProductService = _fixture.GetRequiredService<IDiscountedProductService>();
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
        public async Task GetAllAsync_GetOriginalIfNotDiscounted()
        {
            // Arrange
            Product notDiscountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(notDiscountedProduct);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(1);
            result
                .First()
                .ShouldSatisfyAllConditions(
                    discountedProduct => discountedProduct.Id.ShouldBe(addedProduct.Entity.Id),
                    discountedProduct => discountedProduct.DiscountedPrice.ShouldBe(100M)
                );
        }

        [Fact]
        public async Task GetAllAsync_GetDiscountedIfDiscounted()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(1);
            result
                .First()
                .ShouldSatisfyAllConditions(
                    discountedProduct => discountedProduct.Id.ShouldBe(addedProduct.Entity.Id),
                    discountedProduct => discountedProduct.DiscountedPrice.ShouldBe(80M)
                );
        }

        [Fact]
        public async Task GetAllAsync_GetOriginalIfOfferIsOutdated()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(1);
            result
                .First()
                .ShouldSatisfyAllConditions(
                    discountedProduct => discountedProduct.Id.ShouldBe(addedProduct.Entity.Id),
                    discountedProduct => discountedProduct.DiscountedPrice.ShouldBe(100M)
                );
        }

        [Fact]
        public async Task GetAllAsync_GetOriginalIfOfferIsNotYetApplied()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(1);
            result
                .First()
                .ShouldSatisfyAllConditions(
                    discountedProduct => discountedProduct.Id.ShouldBe(addedProduct.Entity.Id),
                    discountedProduct => discountedProduct.DiscountedPrice.ShouldBe(100M)
                );
        }

        [Fact]
        public async Task GetAllAsync_GetSorted()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Product notDiscountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(1)),
                    Percentage = 0.2M
                };
            Offer outdatedOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> product2 = await _dbContext.AddAsync(discountedProduct);
            EntityEntry<Product> product1 = await _dbContext.AddAsync(notDiscountedProduct);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            EntityEntry<Offer> addedOudatedOffer = await _dbContext.AddAsync(outdatedOffer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(outdatedOffer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(2);
            result.First().Id.Should().Be(product1.Entity.Id);
            result.Last().Id.Should().Be(product2.Entity.Id);
        }

        [Fact]
        public async Task GetAllDiscountedAsync_GetOnlyDiscounted()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Product notDiscountedProduct =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(1)),
                    Percentage = 0.2M
                };
            Offer outdatedOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> product2 = await _dbContext.AddAsync(discountedProduct);
            EntityEntry<Product> product1 = await _dbContext.AddAsync(notDiscountedProduct);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            EntityEntry<Offer> addedOudatedOffer = await _dbContext.AddAsync(outdatedOffer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(outdatedOffer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllDiscountedAsync()
            ).ToList();

            // Assert
            (await _dbContext.Products.CountAsync())
                .Should()
                .Be(2);
            result.Count.Should().Be(1);
            result.First().Id.Should().Be(product2.Entity.Id);
        }

        [Fact]
        public async Task GetAllDiscountedAsync_GetSorted()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Product discountedProduct2 =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Product discountedProduct1 =
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> product2 = await _dbContext.AddAsync(discountedProduct2);
            EntityEntry<Product> product1 = await _dbContext.AddAsync(discountedProduct1);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllDiscountedAsync()
            ).ToList();

            // Assert
            result.Count.Should().Be(2);
            result.First().Id.Should().Be(product1.Entity.Id);
            result.Last().Id.Should().Be(product2.Entity.Id);
        }

        [Fact]
        public async Task ApplyOfferAsync_InvalidOffer_ThrowsValidationException()
        {
            // Arrange
            DateTime today = DateTime.Today;
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer invalidOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            ValidationException exception = await _discountedProductService
                .ApplyOfferAsync(addedProduct.Entity.Id, invalidOffer)
                .ShouldThrowAsync<ValidationException>();
            exception.Errors
                .First()
                .ErrorMessage.Should()
                .Be(OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE);
        }

        [Fact]
        public async Task ApplyOfferAsync_OudatedOffer_ThrowsValidationException()
        {
            // Arrange
            DateTime today = DateTime.Today;
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer oudatedOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(product);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            ValidationException exception = await _discountedProductService
                .ApplyOfferAsync(addedProduct.Entity.Id, oudatedOffer)
                .ShouldThrowAsync<ValidationException>();
            exception.Errors
                .First()
                .ErrorMessage.Should()
                .Be(OfferValidator.START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
        }

        [Fact]
        public async Task ApplyOfferAsync_OfferAlreadyApplied_ThrowsValidationException()
        {
            // Arrange
            DateTime today = DateTime.Today;
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(2)),
                    Percentage = 0.2M
                };
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today.AddDays(3)),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(product);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            product.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            ValidationException exception = await _discountedProductService
                .ApplyOfferAsync(addedProduct.Entity.Id, newOffer)
                .ShouldThrowAsync<ValidationException>();
            exception.Errors
                .First()
                .ErrorMessage.Should()
                .Be(ProductAddOfferValidator.OFFER_ALREADY_EXISTS);
        }

        [Fact]
        public async Task ApplyOfferAsync_OfferAlreadyExists_NoCreateANewOne()
        {
            // Arrange
            DateTime today = DateTime.Today;
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.2M
                };
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.2M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(product);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            DiscountedProduct discountedProduct = await _discountedProductService.ApplyOfferAsync(
                addedProduct.Entity.Id,
                newOffer
            );

            // Assert
            (await _dbContext.Offers.CountAsync())
                .Should()
                .Be(1);
            (
                await _dbContext.Products
                    .Include(_ => _.Offers)
                    .Select(
                        _ =>
                            new Product
                            {
                                Id = _.Id,
                                Label = _.Label,
                                Description = _.Description,
                                Price = _.Price,
                                Category = _.Category,
                                Offers = _.Offers
                            }
                    )
                    .FirstAsync()
            ).Offers.Count.Should().Be(1);
        }

        [Fact]
        public async Task ApplyOfferAsync_OfferAlreadyApplied_Force_ApplyNewOffer()
        {
            // Arrange
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
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
                    Price = 100M,
                    Category = "Surgelé"
                };
            Offer offer =
                new()
                {
                    StartDate = today.AddDays(1),
                    EndDate = today.AddDays(2),
                    Percentage = 0.2M
                };
            Offer newOffer =
                new()
                {
                    StartDate = today,
                    EndDate = today.AddDays(3),
                    Percentage = 0.3M
                };
            EntityEntry<Product> addedProduct = await _dbContext.AddAsync(product);
            EntityEntry<Offer> addedOffer = await _dbContext.AddAsync(offer);
            product.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            await _discountedProductService.ApplyOfferAsync(addedProduct.Entity.Id, newOffer, true);

            // Assert
            Product discountedProduct = await _dbContext.Products
                .Include(p => p.Offers.Where(o => o.StartDate <= today && o.EndDate >= today))
                .Select(
                    _ =>
                        new Product
                        {
                            Id = _.Id,
                            Label = _.Label,
                            Description = _.Description,
                            Price = _.Price,
                            Category = _.Category,
                            Offers = _.Offers
                        }
                )
                .FirstAsync();
            discountedProduct.Offers.Should().ContainSingle();
            discountedProduct.Offers.First().Percentage.Should().Be(newOffer.Percentage);
            (await _dbContext.Offers.CountAsync()).Should().Be(2);
        }
    }
}
