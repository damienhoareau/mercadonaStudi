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
        : IClassFixture<InMemoryApplicationDbContextFixture>,
            IAsyncLifetime
    {
        private readonly InMemoryApplicationDbContextFixture _fixture;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly IDiscountedProductService _discountedProductService;

        public DiscountedProductServiceTests(InMemoryApplicationDbContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddSingleton<IValidator<Offer>, OfferValidator>();
                services.AddSingleton<
                    IValidator<(Product product, Offer offer)>,
                    ProductAddOfferValidator
                >();
                services.AddSingleton<IOfferService, OfferService>();
                services.AddSingleton<IDiscountedProductService, DiscountedProductService>();

                return services;
            });

            _dbContextFactory = _fixture.GetRequiredService<
                IDbContextFactory<ApplicationDbContext>
            >();
            _discountedProductService = _fixture.GetRequiredService<IDiscountedProductService>();
        }

        public Task InitializeAsync()
        {
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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
            EntityEntry<Product> addedProduct = await context.AddAsync(notDiscountedProduct);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(discountedProduct);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            addedProduct.Entity.Offers.Add(offer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            Offer outdatedOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 20
                };
            EntityEntry<Product> product2 = await context.AddAsync(discountedProduct);
            EntityEntry<Product> product1 = await context.AddAsync(notDiscountedProduct);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            EntityEntry<Offer> addedOudatedOffer = await context.AddAsync(outdatedOffer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(outdatedOffer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            Offer outdatedOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 20
                };
            EntityEntry<Product> product2 = await context.AddAsync(discountedProduct);
            EntityEntry<Product> product1 = await context.AddAsync(notDiscountedProduct);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            EntityEntry<Offer> addedOudatedOffer = await context.AddAsync(outdatedOffer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(outdatedOffer);
            await context.SaveChangesAsync();

            // Act
            List<DiscountedProduct> result = (
                await _discountedProductService.GetAllDiscountedAsync()
            ).ToList();

            // Assert
            (await context.Products.CountAsync())
                .Should()
                .Be(2);
            result.Count.Should().Be(1);
            result.First().Id.Should().Be(product2.Entity.Id);
        }

        [Fact]
        public async Task GetAllDiscountedAsync_GetSorted()
        {
            // Arrange
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> product2 = await context.AddAsync(discountedProduct2);
            EntityEntry<Product> product1 = await context.AddAsync(discountedProduct1);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            product2.Entity.Offers.Add(offer);
            product1.Entity.Offers.Add(offer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(product);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(product);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today.AddDays(3)),
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(product);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            product.Offers.Add(offer);
            await context.SaveChangesAsync();

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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            Offer newOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 20
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(product);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            await context.SaveChangesAsync();

            // Act
            DiscountedProduct discountedProduct = await _discountedProductService.ApplyOfferAsync(
                addedProduct.Entity.Id,
                newOffer
            );

            // Assert
            (await context.Offers.CountAsync())
                .Should()
                .Be(1);
            (
                await context.Products
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
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
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
                    Percentage = 20
                };
            Offer newOffer =
                new()
                {
                    StartDate = today,
                    EndDate = today.AddDays(3),
                    Percentage = 30
                };
            EntityEntry<Product> addedProduct = await context.AddAsync(product);
            EntityEntry<Offer> addedOffer = await context.AddAsync(offer);
            product.Offers.Add(offer);
            await context.SaveChangesAsync();

            // Act
            await _discountedProductService.ApplyOfferAsync(addedProduct.Entity.Id, newOffer, true);

            // Assert
            Product discountedProduct = await context.Products
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
            (await context.Offers.CountAsync()).Should().Be(2);
        }
    }
}
