﻿using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Validation;

/// <summary>
/// Validateur de promotion
/// </summary>
public class OfferValidator : AbstractValidator<Offer>
{
#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
    public const string START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY =
        "La date de l'offre ne peut pas être antérieure à aujourd'hui.";
    public const string END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE =
        "La date de fin de l'offre ne peut pas être antérieure à la date de début.";
    public const string PERCENTAGE_BETWEEN_0_AND_1 =
        "Le pourcentage de remise doit être compris entre 0% et 100% exclus.";
#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="OfferValidator"/>.
    /// </summary>
    public OfferValidator()
    {
        RuleFor(_ => _.StartDate)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.Today))
            .WithMessage(START_DATE_GREATER_THAN_OR_EQUALS_TO_TODAY);
        RuleFor(_ => _.EndDate)
            .GreaterThanOrEqualTo(_ => _.StartDate)
            .WithMessage(END_DATE_GREATER_THAN_OR_EQUALS_TO_START_DATE);
        RuleFor(_ => _.Percentage)
            .ExclusiveBetween(Offer.PercentageMin, Offer.PercentageMax)
            .WithMessage(PERCENTAGE_BETWEEN_0_AND_1);
    }
}
