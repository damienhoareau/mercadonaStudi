using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Validation
{
    public class OfferValidator : AbstractValidator<Offer>
    {
        public const string START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY =
            "La date de l'offre ne peut pas être antérieure à aujourd'hui.";
        public const string END_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY =
            "La date de fin de l'offre ne peut pas être antérieure à la date de début.";
        public const string PERCENTAGE_BETWEEN_0_AND_1 =
            "Le pourcentage de remise doit être compris entre 0 et 1 exclus.";

        public OfferValidator()
        {
            RuleFor(_ => _.StartDate)
                .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
                .WithMessage(START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
            RuleFor(_ => _.EndDate)
                .GreaterThanOrEqualTo(_ => _.StartDate)
                .WithMessage(END_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
            RuleFor(_ => _.Percentage)
                .ExclusiveBetween(Offer.PercentageMin, Offer.PercentageMax)
                .WithMessage(PERCENTAGE_BETWEEN_0_AND_1);
        }
    }
}
