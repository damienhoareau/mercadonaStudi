using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercadona.Backend.Services
{
    /// <summary>
    /// Service permettant d'inter-agir avec des <seealso cref="Offer"/>
    /// </summary>
    /// <seealso cref="Mercadona.Backend.Services.Interfaces.IOfferService" />
    public class OfferService : IOfferService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Offer> _offerValidator;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="OfferService"/>.
        /// </summary>
        /// <param name="dbContext">Le contexte de la base de donnée.</param>
        /// <param name="offerValidator">Le validateur de promotion.</param>
        public OfferService(ApplicationDbContext dbContext, IValidator<Offer> offerValidator)
        {
            _dbContext = dbContext;
            _offerValidator = offerValidator;
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public async Task<Offer> AddOfferAsync(Offer offer)
        {
            await _offerValidator.ValidateAndThrowAsync(offer);

            EntityEntry<Offer> result = await _dbContext.AddAsync(offer);

            await _dbContext.SaveChangesAsync();

            return result.Entity;
        }
    }
}
