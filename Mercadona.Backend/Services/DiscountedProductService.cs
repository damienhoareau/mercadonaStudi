using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Microsoft.EntityFrameworkCore;

namespace Mercadona.Backend.Services
{
    public class DiscountedProductService : IDiscountedProductService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly OfferValidator _offerValidator;
        private readonly IOfferService _offerService;

        public DiscountedProductService(
            ApplicationDbContext dbContext,
            OfferValidator offerValidator,
            IOfferService offerService
        )
        {
            _dbContext = dbContext;
            _offerValidator = offerValidator;
            _offerService = offerService;
        }

        public async Task<IEnumerable<DiscountedProduct>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            List<Product> products = await _dbContext.Products
                .Include(p => p.Offers)
                .OrderBy(_ => _.Label)
                .ToListAsync(cancellationToken);

            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            return products
                .Select(
                    p =>
                        new DiscountedProduct(
                            p,
                            p.Offers.SingleOrDefault(
                                o => o.StartDate <= today && o.EndDate >= today
                            )
                        )
                )
                .ToList();
        }

        public async Task<IEnumerable<DiscountedProduct>> GetAllDiscountedAsync(
            CancellationToken cancellationToken = default
        )
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            List<Product> products = await _dbContext.Products
                .Include(p => p.Offers)
                .Where(p => p.Offers.Any(o => o.StartDate <= today && o.EndDate >= today))
                .OrderBy(_ => _.Label)
                .ToListAsync(cancellationToken);

            return products
                .Select(
                    p =>
                        new DiscountedProduct(
                            p,
                            p.Offers.SingleOrDefault(
                                o => o.StartDate <= today && o.EndDate >= today
                            )
                        )
                )
                .ToList();
        }

        // TODO : Vérifier qu'il n'existe pas une promotion qui chevauche celle qu'on veut affecter
        public async Task<DiscountedProduct> ApplyOfferAsync(
            Guid productId,
            Offer offer,
            CancellationToken cancellationToken = default
        )
        {
            await _offerValidator.ValidateAndThrowAsync(offer, cancellationToken);

            Product product = await _dbContext.Products.SingleAsync(
                _ => _.Id == productId,
                cancellationToken
            );

            Offer? existingOffer = await _dbContext.Offers.SingleOrDefaultAsync(
                _ =>
                    _.StartDate == offer.StartDate
                    && _.EndDate == offer.EndDate
                    && _.Percentage == offer.Percentage,
                cancellationToken
            );
            if (existingOffer == null)
                offer = await _offerService.AddOfferAsync(offer, cancellationToken);

            product.Offers.Add(offer);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new DiscountedProduct(product, offer);
        }
    }
}
