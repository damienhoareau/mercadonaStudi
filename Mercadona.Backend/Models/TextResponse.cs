namespace Mercadona.Backend.Models
{
    /// <summary>
    /// Représente une réponse d'un controller de type texte
    /// </summary>
    public class TextResponse
    {
        /// <value>
        /// Le texte.
        /// </value>
        public string? Text { get; set; }

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="TextResponse"/>.
        /// </summary>
        /// <param name="text">Le texte.</param>
        public TextResponse(string? text)
        {
            Text = text;
        }
    }
}
