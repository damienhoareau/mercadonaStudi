using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MimeDetective;
using System.ComponentModel.DataAnnotations;
using ValidationException = FluentValidation.ValidationException;

namespace Mercadona.Backend.Controllers
{
    /// <summary>
    /// Controlleur gérant des <seealso cref="Product"/>
    /// </summary>
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
        /// <response code="200">Si il existe au moins un produit.</response>
        /// <response code="204">Si il n'existe aucun produit.</response>
        /// <response code="500">Si une erreur survient au niveau du code.</response>
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
        /// Récupère le flux de donnée de l'image d'un produit
        /// </summary>
        /// <param name="productId">Identifiant du produit</param>
        /// <returns>Flux du produit</returns>
        /// <response code="200">Si le produit existe.</response>
        /// <response code="404">Si le produit n'existe pas.</response>
        /// <response code="500">Si une erreur survient au niveau du code.</response>
        [AllowAnonymous]
        [HttpGet]
        [Route("api/products/image")]
        [ResponseCache(Duration = int.MaxValue)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetImageAsync([FromQuery] [Required] Guid productId)
        {
            try
            {
                Stream? imageStream = await _service.GetImageAsync(
                    productId,
                    HttpContext.RequestAborted
                );
                if (imageStream != null)
                {
                    string? mimeType = _inspector
                        .Inspect(imageStream)
                        .First()
                        .Definition.File.MimeType;
                    imageStream.Seek(0, SeekOrigin.Begin);
                    return File(imageStream, mimeType ?? "application/octet-stream");
                }
                return NotFound();
            }
            catch (Exception ex)
            {
                return Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Ajoute un produit
        /// </summary>
        /// <param name="product">Produit à ajouter</param>
        /// <returns>Produit ajouté</returns>
        /// <response code="201">Si le produit a été ajouté.</response>
        /// <response code="400">Si les données ne sont pas valides.</response>
        /// <response code="500">Si une erreur survient au niveau du code.</response>
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
