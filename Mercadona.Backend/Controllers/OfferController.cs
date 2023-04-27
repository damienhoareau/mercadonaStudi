using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadona.Backend.Controllers
{
    [Authorize]
    [ApiController]
    public class OfferController : ControllerBase
    {
        private readonly IOfferService _service;

        /// <summary>
        /// Controlleur gérant des <seealso cref="Offer"/>
        /// </summary>
        /// <param name="service">Service gérant des <seealso cref="Offer"/></param>
        public OfferController(IOfferService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retourne la liste des promotions en cours ou à venir
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/offers")]
        [ProducesResponseType(typeof(IEnumerable<Offer>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                IEnumerable<Offer> offers = await _service.GetAllAsync(HttpContext.RequestAborted);
                return offers.Any() ? Ok(offers) : NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Ajoute une promotion
        /// </summary>
        /// <param name="offer"><seealso cref="Offer"/> à ajouter</param>
        /// <returns><seealso cref="Product"/> ajouté</returns>
        [HttpPost]
        [Route("api/offers")]
        [ProducesResponseType(typeof(Offer), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddOfferAsync([FromBody] Offer offer)
        {
            try
            {
                Offer createdOffer = await _service.AddOfferAsync(offer);
                return Created(string.Empty, createdOffer);
            }
            catch (ValidationException ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
