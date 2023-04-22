namespace Mercadona.Backend.Data
{
    public class Product
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public byte[] Image { get; set; } = Array.Empty<byte>();
        public string Category { get; set; } = string.Empty;

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
    }
}