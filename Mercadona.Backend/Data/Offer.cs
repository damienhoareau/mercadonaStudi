namespace Mercadona.Backend.Data
{
    public class Offer
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal Percentage { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}