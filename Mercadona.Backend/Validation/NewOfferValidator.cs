using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Validation
{
    [Obsolete("On vérifie déjà la validité d'ajout avec OfferValidator")]
    public class NewOfferValidator : AbstractValidator<Offer>
    {
        public const string NEW_OFFER_IS_OUTDATED = "La période de promotion est dépassée.";

        public NewOfferValidator()
        {
            RuleFor(_ => _).Must(NewOfferIsNotOutdated).WithMessage(NEW_OFFER_IS_OUTDATED);
        }

        private bool NewOfferIsNotOutdated(Offer newOffer)
        {
            DateOnly today = DateOnly.FromDateTime(DateTime.Today);
            return newOffer.EndDate >= today;
        }
    }
}
