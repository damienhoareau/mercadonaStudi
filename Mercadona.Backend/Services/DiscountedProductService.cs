using FluentValidation;
using FluentValidation.Results;
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
        private readonly ProductAddOfferValidator _productAddOfferValidator;
        private readonly IOfferService _offerService;

        public DiscountedProductService(
            ApplicationDbContext dbContext,
            OfferValidator offerValidator,
            ProductAddOfferValidator productAddOfferValidator,
            IOfferService offerService
        )
        {
            _dbContext = dbContext;
            _offerValidator = offerValidator;
            _productAddOfferValidator = productAddOfferValidator;
            _offerService = offerService;
        }

        public async Task<IEnumerable<DiscountedProduct>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            List<Product> products = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Offers.Where(o => o.StartDate <= today && o.EndDate >= today))
                .OrderBy(_ => _.Label)
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

        public async Task<IEnumerable<DiscountedProduct>> GetAllDiscountedAsync(
            CancellationToken cancellationToken = default
        )
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            List<Product> products = await _dbContext.Products
                .AsNoTracking()
                .Include(p => p.Offers.Where(o => o.StartDate <= today && o.EndDate >= today))
                .Where(p => p.Offers.Any(o => o.StartDate <= today && o.EndDate >= today))
                .OrderBy(_ => _.Label)
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

        public async Task<DiscountedProduct> ApplyOfferAsync(
            Guid productId,
            Offer offer,
            bool forceReplace = false
        )
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);

            // On vérifie que l'offre est valide
            await _offerValidator.ValidateAndThrowAsync(offer);

            Product product = await _dbContext.Products
                .Include(p => p.Offers.Where(o => o.EndDate >= today)) // Inclure uniquement les offres en cours ou futures
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
                .SingleAsync(_ => _.Id == productId);
            _dbContext.Attach(product);

            // On vérifie qu'une promotion n'est pas en cours durant la promotion 'offer'
            // et que la période de la promotion n'a pas été dépassée.
            ValidationResult validationResult = await _productAddOfferValidator.ValidateAsync(
                (product, offer)
            );
            if (
                validationResult.Errors.Count == 1
                && validationResult.Errors.First().ErrorMessage
                    == ProductAddOfferValidator.OFFER_ALREADY_EXISTS
                && !forceReplace
            )
                throw new ValidationException(validationResult.Errors);

            // Si la promotion existe déjà, on ne la crée pas.
            Offer? existingOffer = await _dbContext.Offers.SingleOrDefaultAsync(
                _ =>
                    _.StartDate == offer.StartDate
                    && _.EndDate == offer.EndDate
                    && _.Percentage == offer.Percentage
            );
            if (existingOffer == null)
                offer = await _offerService.AddOfferAsync(offer);
            else
                offer = existingOffer;

            // On supprime les promotions qui seront à cheval sur celles appliquée
            foreach (
                Offer toDelete in validationResult.Errors
                    .Where(_ => _.AttemptedValue is Offer)
                    .Select(_ => (Offer)_.AttemptedValue)
            )
                product.Offers.Remove(
                    product.Offers.Single(
                        _ =>
                            _.StartDate == toDelete.StartDate
                            && _.EndDate == toDelete.EndDate
                            && _.Percentage == toDelete.Percentage
                    )
                );

            // On applique la promotion 'offer' au produit 'product'.
            product.Offers.Add(offer);
            await _dbContext.SaveChangesAsync();

            return new DiscountedProduct(product, offer);
        }
    }
}
