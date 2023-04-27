using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeDetective;

namespace Mercadona.Backend.Controllers
{
    [Authorize]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;
        private readonly ContentInspector _inspector;

        /// <summary>
        /// Controlleur gérant des <seealso cref="Product"/>
        /// </summary>
        /// <param name="service">Service gérant des <seealso cref="Product"/></param>
        /// <param name="inspector">Service de récupération des types MIME</param>
        public ProductController(IProductService service, ContentInspector inspector)
        {
            _service = service;
            _inspector = inspector;
        }

        /// <summary>
        /// Recupère la liste des produits
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("api/products")]
        [ProducesResponseType(typeof(IEnumerable<Product>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllAsync()
        {
            try
            {
                IEnumerable<Product> products = await _service.GetAllAsync(
                    HttpContext.RequestAborted
                );
                return products.Any() ? Ok(products) : NoContent();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Récupère le flux des données de l'image d'un produit
        /// </summary>
        /// <param name="productId">Identifiant du <seealso cref="Product"/></param>
        /// <returns>Flux du produit</returns>
        [AllowAnonymous]
        [HttpGet]
        [Route("api/products/{productId}/image")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetImageAsync([FromQuery] Guid productId)
        {
            try
            {
                Stream? imageStream = await _service.GetImageAsync(
                    productId,
                    HttpContext.RequestAborted
                );
                return imageStream != null
                    ? File(
                        imageStream,
                        _inspector.Inspect(imageStream).First().Definition.File.MimeType
                            ?? "application/octet-stream"
                    )
                    : NotFound();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Ajoute un produit
        /// </summary>
        /// <param name="product"><seealso cref="Product"/> à ajouter</param>
        /// <returns><seealso cref="Product"/> ajouté</returns>
        [HttpPost]
        [Route("api/products")]
        [ProducesResponseType(typeof(Product), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddProductAsync([FromBody] Product product)
        {
            try
            {
                Product createdProduct = await _service.AddProductAsync(product);
                return Created(string.Empty, createdProduct);
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
