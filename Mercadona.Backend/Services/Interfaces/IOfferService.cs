using FluentValidation;
using Mercadona.Backend.Data;

namespace Mercadona.Backend.Services.Interfaces
{
    /// <summary>
    /// Interface d'un service permettant d'inter-agir avec des <seealso cref="Offer"/>
    /// </summary>
    public interface IOfferService
    {
        /// <summary>
        /// Retourne la liste des promotions en cours ou à venir
        /// </summary>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns>Liste de <seealso cref="Offer"/></returns>
        Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ajoute une promotion
        /// </summary>
        /// <param name="offer"><seealso cref="Offer"/> à ajouter</param>
        /// <exception cref="ValidationException"/>
        /// <returns>
        /// <seealso cref="Offer"/> ajoutée<br/>
        /// <seealso cref="ValidationException"/> : Si l'offre n'est pas valide
        /// </returns>
        Task<Offer> AddOfferAsync(Offer offer);
    }
}
