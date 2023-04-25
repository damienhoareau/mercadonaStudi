using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Validation
{
    public class ProductAddOfferValidator : AbstractValidator<(Product product, Offer offer)>
    {
        public const string OFFER_ALREADY_EXISTS =
            "Une(Des) promotion(s) existent déjà sur la période de la nouvelle promotion.";

        public ProductAddOfferValidator()
        {
            RuleForEach(_ => _.product.Offers)
                .Must(NoAlreadyOffers)
                .WithMessage(OFFER_ALREADY_EXISTS);
        }

        private bool NoAlreadyOffers((Product product, Offer newOffer) param, Offer offer)
        {
            (Product _, Offer newOffer) = param;
            return !(offer.StartDate <= newOffer.EndDate && newOffer.StartDate <= offer.EndDate);
        }
    }
}
