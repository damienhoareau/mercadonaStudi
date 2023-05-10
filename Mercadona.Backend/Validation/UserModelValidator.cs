using FluentValidation;
using Mercadona.Backend.Models;
using System.Text.RegularExpressions;

namespace Mercadona.Backend.Validation
{
    /// <summary>
    /// Validateur d'utilisateur
    /// </summary>
    public partial class UserModelValidator : AbstractValidator<UserModel>
    {
#pragma warning disable CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement
        public const string USERNAME_VALID_EMAIL = "Il ne s'agit pas d'un email valide.";
        public const string WEAK_PASSWORD =
            "Le mot de passe doit faire 8 caractères minimum,\ncomporter au moins 1 chiffre et un caractère spécial\net comporter des lettres en majuscule et minuscule.";
#pragma warning restore CS1591 // Commentaire XML manquant pour le type ou le membre visible publiquement

        private static readonly int MinimumNumericCharacters = 1;
        private static readonly int MinimumSymbolCharacters = 1;
        private static readonly int PreferredPasswordLength = 8;
        private static readonly bool RequiresUpperAndLowerCaseCharacters = true;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="UserModelValidator"/>.
        /// </summary>
        public UserModelValidator()
        {
            RuleFor(_ => _.Username).EmailAddress().WithMessage(USERNAME_VALID_EMAIL);
            RuleFor(_ => _.Password).Must(IsNotWeak).WithMessage(WEAK_PASSWORD);
        }

        /// <summary>
        /// Teste si le mot de passe est fort.
        /// </summary>
        /// <param name="password">Le mot de passe.</param>
        /// <returns>
        /// <c>true</c> si le mot de passe contient est fort<br/>
        /// <c>false</c> si le mot de passe n'est pas fort
        /// </returns>
        public virtual bool IsNotWeak(string password)
        {
            return password.Length >= PreferredPasswordLength
                && ContainsNumericCharacters(password, MinimumNumericCharacters)
                && ContainsSpecialCharacters(password, MinimumSymbolCharacters)
                && (
                    !RequiresUpperAndLowerCaseCharacters
                    || ContainsUppercaseAndLowercaseCharacters(password)
                );
        }

        /// <summary>
        /// Teste si le mot de passe contient assez de caractères numériques.
        /// </summary>
        /// <param name="password">Le mot de passe.</param>
        /// <param name="minimumNumericCharacters">Le nombre de caractère numérique minimum.</param>
        /// <returns>
        /// <c>true</c> si le mot de passe contient assez de caractères numériques<br/>
        /// <c>false</c> si le mot de passe ne contient pas assez de caractères numériques
        /// </returns>
        public virtual bool ContainsNumericCharacters(string password, int minimumNumericCharacters)
        {
            int numericCharacters = NumericRegex().Matches(password).Count;
            return numericCharacters >= minimumNumericCharacters;
        }

        /// <summary>
        /// Teste si le mot de passe contient assez de caractères spéciaux.
        /// </summary>
        /// <param name="password">Le mot de passe.</param>
        /// <param name="minimumSpecialCharacters">Le nombre de caractère spécial minimum.</param>
        /// <returns>
        /// <c>true</c> si le mot de passe contient assez de caractères spéciaux<br/>
        /// <c>false</c> si le mot de passe ne contient pas assez de caractères spéciaux
        /// </returns>
        public virtual bool ContainsSpecialCharacters(string password, int minimumSpecialCharacters)
        {
            int specialCharacters = SpecialCharacterRegex().Matches(password).Count;
            return specialCharacters >= minimumSpecialCharacters;
        }

        /// <summary>
        /// Teste si le mot de passe contient des caractères en majuscule et en minuscule.
        /// </summary>
        /// <param name="password">Le mot de passe.</param>
        /// <returns>
        /// <c>true</c> si le mot de passe contient assez de caractères en majuscule et en minuscule<br/>
        /// <c>false</c> si le mot de passe ne contient pas assez de caractères en majuscule et en minuscule
        /// </returns>
        public virtual bool ContainsUppercaseAndLowercaseCharacters(string password)
        {
            bool hasLowercaseCharacters = LowerCaseRegex().Matches(password).Any();
            bool hasUppercaseCharacters = UpperCaseRegex().Matches(password).Any();
            return hasLowercaseCharacters && hasUppercaseCharacters;
        }

        [GeneratedRegex("\\d")]
        private static partial Regex NumericRegex();

        [GeneratedRegex("[^0-9a-zA-Z\\s]")]
        private static partial Regex SpecialCharacterRegex();

        [GeneratedRegex("[a-z]")]
        private static partial Regex LowerCaseRegex();

        [GeneratedRegex("[A-Z]")]
        private static partial Regex UpperCaseRegex();
    }
}
