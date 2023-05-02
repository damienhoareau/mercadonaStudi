using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercadona.Backend.Services
{
    /// <summary>
    /// Service permettant d'inter-agir avec des <seealso cref="Product"/>
    /// </summary>
    /// <seealso cref="Mercadona.Backend.Services.Interfaces.IProductService" />
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IValidator<Product> _productValidator;

        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="ProductService"/>.
        /// </summary>
        /// <param name="dbContext">Le contexte de la base de donnée.</param>
        /// <param name="productValidator">Le validateur de produit.</param>
        public ProductService(ApplicationDbContext dbContext, IValidator<Product> productValidator)
        {
            _dbContext = dbContext;
            _productValidator = productValidator;
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<Product>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _dbContext.Products
                .AsNoTracking()
                .OrderBy(_ => _.Label)
                .Select(
                    _ =>
                        new Product
                        {
                            Id = _.Id,
                            Label = _.Label,
                            Description = _.Description,
                            Price = _.Price,
                            Category = _.Category
                        }
                )
                .ToListAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Stream?> GetImageAsync(
            Guid productId,
            CancellationToken cancellationToken = default
        )
        {
            byte[]? data = await _dbContext.Products
                .Where(_ => _.Id == productId)
                .Select(_ => _.Image)
                .SingleOrDefaultAsync(cancellationToken);

            return data == null ? null : new MemoryStream(data);
        }

        /// <inheritdoc/>
        public async Task<Product> AddProductAsync(Product product)
        {
            await _productValidator.ValidateAndThrowAsync(product);

            EntityEntry<Product> result = await _dbContext.Products.AddAsync(product);

            await _dbContext.SaveChangesAsync();

            return result.Entity;
        }
    }
}
