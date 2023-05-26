using System.Text.Json.Serialization;

namespace Mercadona.Backend.Data;

/// <summary>
/// Représente un produit
/// </summary>
public class Product
{
    /// <summary>
    /// Prix minimun (exclus) d'un produit
    /// </summary>
    public const decimal PriceMin = 0.00M;

    /// <summary>
    /// Permet de définir comment stocker les données de l'image
    /// </summary>
    /// <returns>Un <seealso cref="Stream"/></returns>
    private readonly Func<Stream> _newStreamConstructor;

    /// <summary>
    /// Contruit un <seealso cref="Product"/> en générant un nouvel identifiant et en construisant la donnée de l'image sous forme d'un <seealso cref="MemoryStream"/> vide
    /// </summary>
    public Product() : this(null, null) { }

    /// <summary>
    /// Contruit un <seealso cref="Product"/> en générant un nouvel identifiant<br/>
    /// Si newStreamConstructor est <c>null</c>, la donnée de l'image sera sous forme d'un <seealso cref="MemoryStream"/> vide
    /// </summary>
    /// <param name="newStreamConstructor">Fonction permettant de construire la donnée de l'image sous forme de <seealso cref="Stream"/><br/>Si <c>null</c>, la donnée de l'image sera sous forme d'un <seealso cref="MemoryStream"/> vide</param>
    public Product(Func<Stream> newStreamConstructor) : this(null, newStreamConstructor) { }

    /// <summary>
    /// Contruit un <seealso cref="Product"/><br/>
    /// Si id est <c>null</c>, un nouvel identifiant sera généré<br/>
    /// Si newStreamConstructor est <c>null</c>, la donnée de l'image sera sous forme d'un <seealso cref="MemoryStream"/> vide
    /// </summary>
    /// <param name="id">Identifiant du produit<br/>Si <c>null</c>, un nouveau sera généré</param>
    /// <param name="newStreamConstructor">Fonction permettant de construire la donnée de l'image sous forme de <seealso cref="Stream"/><br/>Si <c>null</c>, la donnée de l'image sera sous forme d'un <seealso cref="MemoryStream"/> vide</param>
    public Product(Guid? id, Func<Stream>? newStreamConstructor = null)
    {
        Id = id ?? Guid.NewGuid();
        _newStreamConstructor = newStreamConstructor ?? (() => new MemoryStream());
        ImageStream = _newStreamConstructor();
    }

    /// <summary>
    /// Détruit le produit en mémoire et ferme le <seealso cref="ImageStream"/> correspondant
    /// </summary>
    ~Product()
    {
        ImageStream.Dispose();
    }

    /// <summary>
    /// Identifiant du produit
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Libellé du produit
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Description du produit
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Prix du produit
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Donnée de l'image du produit sous forme de tableau de <seealso cref="byte"/>
    /// </summary>
    [JsonIgnore]
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

    /// <summary>
    /// Catégorie du produit
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Liste des promotions appliquées au produit
    /// </summary>
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();

    /// <summary>
    /// Donnée de l'image du produit sous forme de <seealso cref="Stream"/>
    /// </summary>
    [JsonIgnore]
    public Stream ImageStream { get; private set; }
}
