using FluentValidation;
using Mercadona.Backend.Data;
using MimeDetective;
using MimeDetective.Engine;
using System.Collections.Immutable;

namespace Mercadona.Backend.Validation
{
    public class ProductValidator : AbstractValidator<Product>
    {
        public const string LABEL_NOT_EMPTY = "Le libellé ne peut pas être vide.";
        public const string DESCRIPTION_NOT_EMPTY = "La description ne peut pas être vide.";
        public const string PRICE_GREATER_THAN_0 = "Le prix doit être supérieur à 0.";
        public const string IMAGE_IS_VALID = "Ce n'est pas une image valide.";
        public const string CATEGORY_NOT_EMPTY = "La catégorie ne peut pas être vide.";

        private readonly ContentInspector _inspector;

        public ProductValidator(ContentInspector inspector)
        {
            _inspector = inspector;

            RuleFor(_ => _.Label).NotEmpty().WithMessage(LABEL_NOT_EMPTY);
            RuleFor(_ => _.Description).NotEmpty().WithMessage(DESCRIPTION_NOT_EMPTY);
            RuleFor(_ => _.Price).GreaterThan(Product.PriceMin).WithMessage(PRICE_GREATER_THAN_0);
            RuleFor(_ => _.ImageStream).Must(BeAImage).WithMessage(IMAGE_IS_VALID);
            RuleFor(_ => _.Category).NotEmpty().WithMessage(CATEGORY_NOT_EMPTY);
        }

        private bool BeAImage(Stream streamData)
        {
            ImmutableArray<DefinitionMatch> result = _inspector.Inspect(streamData);
            return result.Any();
        }
    }
}
