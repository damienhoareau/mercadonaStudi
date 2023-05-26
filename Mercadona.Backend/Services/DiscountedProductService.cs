using FluentValidation;
using FluentValidation.Results;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercadona.Backend.Services;

/// <summary>
/// Service permettant d'inter-agir avec des <seealso cref="DiscountedProduct"/>
/// </summary>
/// <seealso cref="Mercadona.Backend.Services.Interfaces.IDiscountedProductService" />
public class DiscountedProductService : IDiscountedProductService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly IValidator<Offer> _offerValidator;
    private readonly IValidator<(Product product, Offer offer)> _productAddOfferValidator;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="DiscountedProductService"/>.
    /// </summary>
    /// <param name="dbContextFactory">La fabrique de contexte de la base de donnée.</param>
    /// <param name="offerValidator">Le validateur de promotion.</param>
    /// <param name="productAddOfferValidator">Le validateur d'application de promotion à un produit.</param>
    public DiscountedProductService(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        IValidator<Offer> offerValidator,
        IValidator<(Product product, Offer offer)> productAddOfferValidator
    )
    {
        _dbContextFactory = dbContextFactory;
        _offerValidator = offerValidator;
        _productAddOfferValidator = productAddOfferValidator;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DiscountedProduct>> GetAllAsync(
        CancellationToken cancellationToken = default
    )
    {
        using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        List<Product> products = await context.Products
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

        return products.Select(p => new DiscountedProduct(p, p.GetCurrentOffer())).ToList();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DiscountedProduct>> GetAllDiscountedAsync(
        CancellationToken cancellationToken = default
    )
    {
        using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync(
            cancellationToken
        );

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        List<Product> products = await context.Products
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

        return products.Select(p => new DiscountedProduct(p, p.GetCurrentOffer())).ToList();
    }

    /// <inheritdoc/>
    public async Task<DiscountedProduct> ApplyOfferAsync(
        Guid productId,
        Offer offer,
        bool forceReplace = false
    )
    {
        using ApplicationDbContext context = await _dbContextFactory.CreateDbContextAsync();

        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        // On vérifie que l'offre est valide
        await _offerValidator.ValidateAndThrowAsync(offer);

        Product product = await context.Products
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
        context.Attach(product);

        // On vérifie qu'une promotion n'est pas en cours durant la promotion 'offer'
        // et que la période de la promotion n'a pas été dépassée.
        ValidationResult validationResult = await _productAddOfferValidator.ValidateAsync(
            (product, offer)
        );
        if (
            validationResult.Errors.Any(
                _ => _.ErrorMessage == ProductAddOfferValidator.OFFER_ALREADY_EXISTS
            ) && !forceReplace
        )
            throw new ValidationException(
                validationResult.Errors
                    .Where(
                        _ =>
                            _.ErrorMessage == ProductAddOfferValidator.OFFER_ALREADY_EXISTS
                            && _.AttemptedValue is Offer
                    )
                    .OrderBy(_ => ((Offer)_.AttemptedValue).StartDate)
                    .ThenBy(_ => ((Offer)_.AttemptedValue).Percentage)
            );

        // Si la promotion existe déjà, on ne la crée pas.
        Offer? existingOffer = await context.Offers.SingleOrDefaultAsync(
            _ =>
                _.StartDate == offer.StartDate
                && _.EndDate == offer.EndDate
                && _.Percentage == offer.Percentage
        );
        if (existingOffer == null)
        {
            await _offerValidator.ValidateAndThrowAsync(offer);
            EntityEntry<Offer> result = await context.AddAsync(offer);
            offer = result.Entity;
        }
        else
        {
            offer = existingOffer;
        }

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
        await context.SaveChangesAsync();

        return new DiscountedProduct(product, offer);
    }
}
