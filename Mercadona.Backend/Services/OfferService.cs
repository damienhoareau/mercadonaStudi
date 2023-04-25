using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercadona.Backend.Services
{
    public class OfferService : IOfferService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly OfferValidator _offerValidator;

        public OfferService(ApplicationDbContext dbContext, OfferValidator offerValidator)
        {
            _dbContext = dbContext;
            _offerValidator = offerValidator;
        }

        public async Task<IEnumerable<Offer>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            return await _dbContext.Offers
                .AsNoTracking()
                .Where(_ => _.EndDate >= today)
                .OrderBy(_ => _.StartDate)
                .ThenBy(_ => _.Percentage)
                .ToListAsync(cancellationToken);
        }

        public async Task<Offer> AddOfferAsync(Offer offer)
        {
            await _offerValidator.ValidateAndThrowAsync(offer);

            EntityEntry<Offer> result = await _dbContext.AddAsync(offer);

            await _dbContext.SaveChangesAsync();

            return result.Entity;
        }
    }
}
