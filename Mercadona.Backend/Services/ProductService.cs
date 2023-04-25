﻿using FluentValidation;
using Mercadona.Backend.Data;
using Mercadona.Backend.Services.Interfaces;
using Mercadona.Backend.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercadona.Backend.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ProductValidator _productValidator;

        public ProductService(ApplicationDbContext dbContext, ProductValidator productValidator)
        {
            _dbContext = dbContext;
            _productValidator = productValidator;
        }

        public async Task<IEnumerable<Product>> GetAllAsync(
            CancellationToken cancellationToken = default
        )
        {
            return await _dbContext.Products
                .AsNoTracking()
                .OrderBy(_ => _.Label)
                .ToListAsync(cancellationToken);
        }

        public async Task<Product> AddProductAsync(Product product)
        {
            await _productValidator.ValidateAndThrowAsync(product);

            EntityEntry<Product> result = await _dbContext.Products.AddAsync(product);

            await _dbContext.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<Stream?> GetImageAsync(
            Guid productId,
            CancellationToken cancellationToken = default
        )
        {
            Product? product = await _dbContext.Products
                .AsNoTracking()
                .SingleOrDefaultAsync(_ => _.Id == productId, cancellationToken);

            return product?.ImageStream;
        }
    }
}
