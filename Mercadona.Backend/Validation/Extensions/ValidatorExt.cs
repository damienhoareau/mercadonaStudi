using FluentValidation;
using FluentValidation.Results;

namespace Mercadona.Backend.Validation
{
    /// <summary>
    /// Extensions pour l'interface IValidator&lt;T&gt;
    /// </summary>
    public static class ValidatorExt
    {
        /// <summary>
        /// Valide uniquement une seule propriété.
        /// </summary>
        /// <param name="validator">Le validateur.</param>
        /// <returns>Une fonction retournant une liste d'erreurs selon le modèle et la propriété définie</returns>
        public static Func<object, string, Task<IEnumerable<string>>> ValidateValue<T>(
            this IValidator<T> validator
        ) =>
            async (model, propertyName) =>
            {
                ValidationResult result = await validator.ValidateAsync(
                    ValidationContext<T>.CreateWithOptions(
                        (T)model,
                        x => x.IncludeProperties(propertyName)
                    )
                );
                if (result.IsValid)
                    return Array.Empty<string>();
                return result.Errors.Select(x => x.ErrorMessage);
            };
    }
}
