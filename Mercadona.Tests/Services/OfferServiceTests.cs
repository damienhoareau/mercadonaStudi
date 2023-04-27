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
    public class OfferServiceTests : IClassFixture<ApplicationDbContextFixture>, IAsyncLifetime
    {
        private readonly ApplicationDbContextFixture _fixture;
        private readonly ApplicationDbContext _dbContext;
        private readonly IOfferService _offerService;

        public OfferServiceTests(ApplicationDbContextFixture fixture)
        {
            _fixture = fixture;
            _fixture.Reconfigure(services =>
            {
                services.AddSingleton<IValidator<Offer>, OfferValidator>();
                services.AddSingleton<IOfferService, OfferService>();

                return services;
            });

            _dbContext = _fixture.GetRequiredService<ApplicationDbContext>();
            _offerService = _fixture.GetRequiredService<IOfferService>();
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
        public async Task GetAllAsync_GetOnlyCurrentOrFuture()
        {
            // Arrange
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            Offer oudatedOffer =
                new()
                {
                    StartDate = today.AddDays(-1),
                    EndDate = today.AddDays(-1),
                    Percentage = 0.5M
                };
            Offer currentOffer =
                new()
                {
                    StartDate = today,
                    EndDate = today,
                    Percentage = 0.5M
                };
            Offer futureOffer10Percent =
                new()
                {
                    StartDate = today.AddDays(1),
                    EndDate = today.AddDays(1),
                    Percentage = 0.1M
                };
            Offer futureOffer20Percent =
                new()
                {
                    StartDate = today.AddDays(1),
                    EndDate = today.AddDays(1),
                    Percentage = 0.2M
                };
            await _dbContext.AddAsync(oudatedOffer);
            await _dbContext.AddAsync(currentOffer);
            await _dbContext.AddAsync(futureOffer10Percent);
            await _dbContext.AddAsync(futureOffer20Percent);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

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
            DateTime today = DateTime.Today;
            Offer invalidOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today.AddDays(-1)),
                    Percentage = 0.5M
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
            DateTime today = DateTime.Today;
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.5M
                };
            await _dbContext.AddAsync(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();

            // Act
            // Assert
            await _offerService.AddOfferAsync(offer).ShouldThrowAsync<DbUpdateException>();
        }

        [Fact]
        public async Task AddOfferAsync_ValidOffer_ReturnsAddedOffer()
        {
            // Arrange
            DateTime today = DateTime.Today;
            Offer offer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.5M
                };
            await _dbContext.AddAsync(offer);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            Offer otherOffer =
                new()
                {
                    StartDate = DateOnly.FromDateTime(today),
                    EndDate = DateOnly.FromDateTime(today),
                    Percentage = 0.2M
                };

            // Act
            Offer resultOffer = await _offerService.AddOfferAsync(otherOffer);

            // Assert
            resultOffer.Should().BeEquivalentTo(otherOffer);
            List<Offer> offers = await _dbContext.Offers.OrderBy(_ => _.Percentage).ToListAsync();
            offers.Count.Should().Be(2);
            offers.First().Should().BeEquivalentTo(otherOffer);
            offers.Last().Should().BeEquivalentTo(offer);
        }
    }
}
