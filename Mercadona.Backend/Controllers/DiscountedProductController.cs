using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Models;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace Mercadona.Backend.Controllers
{
    [Authorize]
    [ApiController]
    public class DiscountedProductController : ControllerBase
    {
        private readonly IDiscountedProductService _service;

        /// <summary>
        /// Controlleur gérant des <seealso cref="DiscountedProduct"/>
        /// </summary>
        /// <param name="service">Service gérant des <seealso cref="DiscountedProduct"/></param>
        public DiscountedProductController(IDiscountedProductService service)
        {
            _service = service;
        }

        /// <summary>
        /// Recupère la liste des produits (sans distinction de promotion en cours)
        /// </summary>
        /// <returns>
        /// <seealso cref="OkResult"/> :
        /// <list type="bullet">
        /// <item>Liste de <seealso cref="DiscountedProduct"/></item>
        /// </list>
        /// <seealso cref="NoContentResult"/> :<list type="bullet"/>
        /// <seealso cref="ProblemDetails"/> :
        /// <list type="bullet">
        /// <item><seealso cref="StatusCodes.Status500InternalServerError"/> : Si une erreur survient au niveau du code</item>
        /// </list>
        /// </returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("api/discountedProducts")]
        [ProducesResponseType(typeof(IEnumerable<DiscountedProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                IEnumerable<DiscountedProduct> discountedProducts = await _service.GetAllAsync(
                    HttpContext.RequestAborted
                );
                return discountedProducts.Any() ? Ok(discountedProducts) : NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Recupère la liste des produits avec une promotion en cours
        /// </summary>
        /// <returns>
        /// <seealso cref="OkResult"/> :
        /// <list type="bullet">
        /// <item>Liste de <seealso cref="DiscountedProduct"/></item>
        /// </list>
        /// <seealso cref="NoContentResult"/> :<list type="bullet"/>
        /// <seealso cref="ProblemDetails"/> :
        /// <list type="bullet">
        /// <item><seealso cref="StatusCodes.Status500InternalServerError"/> : Si une erreur survient au niveau du code</item>
        /// </list>
        /// </returns>
        [HttpGet]
        [Route("api/discountedProducts?onlyDiscounted=1")]
        [ProducesResponseType(typeof(IEnumerable<DiscountedProduct>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllDiscountedAsync()
        {
            try
            {
                IEnumerable<DiscountedProduct> onlyDiscountedProducts =
                    await _service.GetAllDiscountedAsync(HttpContext.RequestAborted);
                return onlyDiscountedProducts.Any() ? Ok(onlyDiscountedProducts) : NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Applique une promotion à un produit
        /// </summary>
        /// <param name="productId">Identifiant du <seealso cref="Product"/> à remiser</param>
        /// <param name="offer"><seealso cref="Offer"/> à appliquer</param>
        /// <param name="forceReplace">Force à remplacer la promotion en cours si elle existe</param>
        /// <returns>
        /// <seealso cref="CreatedResult"/> :
        /// <list type="bullet">
        /// <item><seealso cref="DiscountedProduct"/> correspondant au <seealso cref="Product"/> remisé</item>
        /// </list>
        /// <seealso cref="ProblemDetails"/> :
        /// <list type="bullet">
        /// <item>Si l'offre n'est pas valide</item>
        /// <item>Si une offre est à cheval sur celle qu'on veut appliquer</item>
        /// <item>Si une erreur survient au niveau du code</item>
        /// </list>
        /// </returns>
        [HttpPost]
        [Route("api/discountedProducts/{productId}/applyOffer{forceReplace?}")]
        [ProducesResponseType(typeof(DiscountedProduct), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ApplyOfferAsync(
            [FromQuery] Guid productId,
            [FromBody] Offer offer,
            [FromQuery, Optional] bool forceReplace
        )
        {
            try
            {
                DiscountedProduct discountedProduct = await _service.ApplyOfferAsync(
                    productId,
                    offer,
                    forceReplace
                );
                return Created(string.Empty, discountedProduct);
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
