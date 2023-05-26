using System.Text.Json.Serialization;

namespace Mercadona.Backend.Data;

/// <summary>
/// Représente une promotion
/// </summary>
public class Offer
{
    /// <summary>
    /// Poucentage minimum (exclus) d'une promotion
    /// </summary>
    public const int PercentageMin = 0;

    /// <summary>
    /// Poucentage maximum (exclus) d'une promotion
    /// </summary>
    public const int PercentageMax = 100;

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
    /// Compris entre 0 et 100 exclus
    /// </summary>
    public int Percentage { get; set; }

    /// <summary>
    /// Collection de <seealso cref="Product"/> soumis à l'offre
    /// </summary>
    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();

    /// <summary>
    /// Détermine si un object donné est égal à celui courant.
    /// </summary>
    /// <param name="obj">L'objet à comparer à celui courant.</param>
    /// <returns>
    ///   <see langword="true" /> si l'objet est égal à celui courant; sinon, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is not Offer other)
            return false;
        return StartDate == other.StartDate
            && EndDate == other.EndDate
            && Percentage == other.Percentage;
    }

    /// <summary>
    /// Retourne un code de hashage pour cette instance.
    /// </summary>
    /// <returns>
    /// Un code de hashage pour cette instance.
    /// </returns>
    public override int GetHashCode()
    {
        return StartDate.GetHashCode() + EndDate.GetHashCode() + Percentage.GetHashCode();
    }

    /// <summary>
    /// Convertie en chaîne de caractères.
    /// </summary>
    /// <returns>
    /// Une <see cref="System.String" /> qui représente cette instance.
    /// </returns>
    public override string ToString()
    {
        return $"{StartDate} -> {EndDate} : {Percentage}%";
    }
}
