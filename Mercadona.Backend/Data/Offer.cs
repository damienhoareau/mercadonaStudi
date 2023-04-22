namespace Mercadona.Backend.Data
{
    public class Offer
    {
        public static readonly decimal PercentageMin = 0.00M;
        public static readonly decimal PercentageMax = 1.00M;

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        /// <summary>
        /// Poucentage de remise<br/>
        /// Compris entre 0 et 1 exclus
        /// </summary>
        public decimal Percentage { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
