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
        private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
        private readonly IValidator<Offer> _offerValidator;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="OfferService"/>.
        /// </summary>
        /// <param name="dbContextFactory">La fabrique de contexte de la base de donnée.</param>
        /// <param name="offerValidator">Le validateur de promotion.</param>
        public OfferService(
            IDbContextFactory<ApplicationDbContext> dbContextFactory,
            IValidator<Offer> offerValidator
        )
        {
            _dbContextFactory = dbContextFactory;
            _offerValidator = offerValidator;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Offer>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync(
                cancellationToken
            );

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            return await context.Offers
                .AsNoTracking()
                .Where(_ => _.EndDate >= today)
                .OrderBy(_ => _.StartDate)
                .ThenBy(_ => _.Percentage)
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Offer> AddOfferAsync(Offer offer)
        {
            using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();

            await _offerValidator.ValidateAndThrowAsync(offer);

            EntityEntry<Offer> result = await context.AddAsync(offer);

            await context.SaveChangesAsync();

            return result.Entity;
        }
    }
}
