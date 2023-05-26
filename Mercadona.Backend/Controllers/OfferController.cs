using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercadona.Backend.Controllers;

/// <summary>
/// Controlleur gérant des <seealso cref="Offer"/>
/// </summary>
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
    /// <response code="200">Si il existe au moins une offre.</response>
    /// <response code="204">Si il n'existe aucune offre.</response>
    /// <response code="500">Si une erreur survient au niveau du code.</response>
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
    /// <param name="offer">Promotion à ajouter</param>
    /// <returns>Promotion ajoutée</returns>
    /// <response code="201">Si l'offre a été ajoutée.</response>
    /// <response code="400">Si les données ne sont pas valides.</response>
    /// <response code="500">Si une erreur survient au niveau du code.</response>
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
