using Mercadona.Backend.Data;

namespace Mercadona.Backend.Services.Interfaces
{
    public interface IOfferService
    {
        /// <summary>
        /// Retourne la liste des promotions en cours ou à venir
        /// </summary>
        /// <param name="cancellationToken">Token d'annulation</param>
        /// <returns></returns>
        Task<IEnumerable<Offer>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Ajoute une promotion
        /// </summary>
        /// <param name="offer"><seealso cref="Offer"/> à ajouter</param>
        /// <returns><seealso cref="Product"/> ajouté</returns>
        Task<Offer> AddOfferAsync(Offer offer);
    }
}
