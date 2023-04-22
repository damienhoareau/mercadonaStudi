namespace Mercadona.Backend.Data
{
    public class Product
    {
        public static readonly decimal PriceMin = 0.00M;

        /// <summary>
        /// Permet de définir comment stocker les données de l'image
        /// </summary>
        /// <returns>Un <seealso cref="Stream"/></returns>
        private readonly Func<Stream> _newStreamConstructor;

        public Product() : this(null, null) { }

        public Product(Func<Stream> newStreamConstructor) : this(null, newStreamConstructor) { }

        public Product(Guid? id, Func<Stream>? newStreamConstructor = null)
        {
            Id = id ?? Guid.NewGuid();
            _newStreamConstructor = newStreamConstructor ?? (() => new MemoryStream());
            ImageStream = _newStreamConstructor();
        }

        public Guid Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public byte[] Image
        {
            get
            {
                using MemoryStream memStream = new();
                ImageStream.Seek(0, SeekOrigin.Begin);
                ImageStream.CopyTo(memStream);
                ImageStream.Seek(0, SeekOrigin.Begin);
                return memStream.ToArray();
            }
            set
            {
                using MemoryStream memStream = new(value);
                ImageStream.Dispose();
                ImageStream = _newStreamConstructor();
                memStream.CopyTo(ImageStream);
                ImageStream.Seek(0, SeekOrigin.Begin);
            }
        }
        public string Category { get; set; } = string.Empty;

        public ICollection<Offer> Offers { get; set; } = new List<Offer>();

        public Stream ImageStream { get; private set; }
    }
}
