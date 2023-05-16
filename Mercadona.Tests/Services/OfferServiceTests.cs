using FluentAssertions;
using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Mercadona.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Mercadona.Tests.Services
{
    public class OfferServiceTests
        : IClassFixture<InMemoryApplicationDbContextFixture>,
            IAsyncLifetime
    {
        private readonly InMemoryApplicationDbContextFixture _fixture;
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly IOfferService _offerService;

        public OfferServiceTests(InMemoryApplicationDbContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddSingleton<IValidator<Offer>, OfferValidator>();
                services.AddSingleton<IOfferService, OfferService>();

                return services;
            });

            _dbContextFactory = _fixture.GetRequiredService<
                IDbContextFactory<ApplicationDbContext>
            >();
            _offerService = _fixture.GetRequiredService<IOfferService>();
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
        public async Task GetAllAsync_GetOnlyCurrentOrFuture()
        {
            // Arrange
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            Offer oudatedOffer =
                new()
                {
                    StartDate = today.AddDays(-1),
                    EndDate = today.AddDays(-1),
                    Percentage = 50
                };
            Offer currentOffer =
                new()
                {
                    StartDate = today,
                    EndDate = today,
                    Percentage = 50
                };
            Offer futureOffer10Percent =
                new()
                {
                    StartDate = today.AddDays(1),
                    EndDate = today.AddDays(1),
                    Percentage = 10
                };
            Offer futureOffer20Percent =
                new()
                {
                    StartDate = today.AddDays(1),
                    EndDate = today.AddDays(1),
                    Percentage = 20
                };
            await context.AddAsync(oudatedOffer);
            await context.AddAsync(currentOffer);
            await context.AddAsync(futureOffer10Percent);
            await context.AddAsync(futureOffer20Percent);
            await context.SaveChangesAsync();

            // Act
            List<Offer> result = (await _offerService.GetAllAsync()).ToList();

            // Assert
            result.Count.Should().Be(3);
            result[0].Should().BeEquivalentTo(currentOffer);
            result[1].Should().BeEquivalentTo(futureOffer10Percent);
            result[2].Should().BeEquivalentTo(futureOffer20Percent);
        }

        [Fact]
        public async Task AddOfferAsync_InvalidOffer_ThrowsValidationException()
        {
            // Arrange
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
            DateTime today = DateTime.Today;
            Offer invalidOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 50
                };

            // Act
            // Assert
            ValidationException exception = await _offerService
                .AddOfferAsync(invalidOffer)
                .ShouldThrowAsync<ValidationException>();
            exception.Errors
                .First()
                .ErrorMessage.Should()
                .Be(OfferValidator.END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE);
        }

        [Fact]
        public async Task AddOfferAsync_AlreadyExists_ThrowsDbUpdateException()
        {
            // Arrange
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
            DateTime today = DateTime.Today;
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 50
                };
            await context.AddAsync(offer);
            await context.SaveChangesAsync();

            // Act
            // Assert
            //await _offerService.AddOfferAsync(offer).ShouldThrowAsync<DbUpdateException>();
            await _offerService.AddOfferAsync(offer).ShouldThrowAsync<Exception>(); // Exception needed for InMemoryDb
        }

        [Fact]
        public async Task AddOfferAsync_ValidOffer_ReturnsAddedOffer()
        {
            // Arrange
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();
            DateTime today = DateTime.Today;
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 50
                };
            await context.AddAsync(offer);
            await context.SaveChangesAsync();
            Offer otherOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 20
                };

            // Act
            Offer resultOffer = await _offerService.AddOfferAsync(otherOffer);

            // Assert
            resultOffer.Should().BeEquivalentTo(otherOffer);
            List<Offer> offers = await context.Offers.OrderBy(_ => _.Percentage).ToListAsync();
            offers.Count.Should().Be(2);
            offers.First().Should().BeEquivalentTo(otherOffer);
            offers.Last().Should().BeEquivalentTo(offer);
        }
    }
}
