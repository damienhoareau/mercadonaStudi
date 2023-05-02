using System.Text.Json.Serialization;

namespace Mercadona.Backend.Data
{
    /// <summary>
    /// Représente une promotion
    /// </summary>
    public class Offer
    {
        /// <summary>
        /// Poucentage minimum (exclus) d'une promotion
        /// </summary>
        public static readonly decimal PercentageMin = 0.00M;

        /// <summary>
        /// Poucentage maximum (exclus) d'une promotion
        /// </summary>
        public static readonly decimal PercentageMax = 1.00M;

        /// <summary>
        /// Date de début de la promotion
        /// </summary>
        public DateOnly StartDate { get; set; }

        /// <summary>
        /// Date de fin de la promotion
        /// </summary>
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Poucentage de remise<br/>
        /// Compris entre 0 et 1 exclus
        /// </summary>
        public decimal Percentage { get; set; }

        /// <summary>
        /// Collection de <seealso cref="Product"/> soumis à l'offre
        /// </summary>
        [JsonIgnore]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
