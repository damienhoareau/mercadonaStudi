using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercadona.Backend.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Offer> Offers { get; set; }
        public DbSet<Product> Products { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            EntityTypeBuilder<Offer> offerBuilder = builder.Entity<Offer>();
            offerBuilder.HasKey(_ => new { _.StartDate, _.EndDate, _.Percentage });
            offerBuilder.Property(_ => _.StartDate).IsRequired();
            offerBuilder.Property(_ => _.EndDate).IsRequired();
            offerBuilder.Property(_ => _.Percentage).IsRequired();

            EntityTypeBuilder<Product> productBuilder = builder.Entity<Product>();
            productBuilder.HasKey(_ => _.Id);
            productBuilder.Property(_ => _.Id).IsRequired();
            productBuilder.Property(_ => _.Label).IsRequired();
            productBuilder.Property(_ => _.Description).IsRequired();
            productBuilder.Property(_ => _.Price).IsRequired();
            productBuilder.Property(_ => _.Image).IsRequired();
            productBuilder.Property(_ => _.Category).IsRequired();
            productBuilder.HasMany(_ => _.Offers).WithMany(_ => _.Products);
        }
    }
}