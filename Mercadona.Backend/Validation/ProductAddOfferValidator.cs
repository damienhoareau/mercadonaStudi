using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Validation
{
    /// <summary>
    /// Validateur d'application de promotion à un produit
    /// </summary>
    public class ProductAddOfferValidator : AbstractValidator<(Product product, Offer offer)>
    {
        public const string OFFER_ALREADY_EXISTS =
            "Une(Des) promotion(s) existent déjà sur la période de la nouvelle promotion.";

        /// <summary>
        /// Initialises une nouvelle instance de la classe <see cref="ProductAddOfferValidator"/>.
        /// </summary>
        public ProductAddOfferValidator()
        {
            RuleForEach(_ => _.product.Offers)
                .Must(NoAlreadyOffers)
                .WithMessage(OFFER_ALREADY_EXISTS);
        }

        /// <summary>
        /// Teste si la promotion <c>offer</c> est à cheval sur la promotion <c>newOffer</c>.
        /// </summary>
        /// <param name="param">Tuple (<see cref="Product"/>, <see cref="Offer"/>) correspondant à un produit et la promotion qu'on veut lui appliquer.</param>
        /// <param name="offer">Une promotion déjà appliquée à <c>product</c>.</param>
        /// <returns></returns>
        private bool NoAlreadyOffers((Product product, Offer newOffer) param, Offer offer)
        {
            (Product _, Offer newOffer) = param;
            return !(offer.StartDate <= newOffer.EndDate && newOffer.StartDate <= offer.EndDate);
        }
    }
}
