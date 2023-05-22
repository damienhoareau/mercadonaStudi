using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mercadona.Backend.Data
{
    /// <summary>
    /// Contexte de la base de données
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityDbContext" />
    public class ApplicationDbContext : IdentityDbContext
    {
        /// <summary>
        /// Initialise une nouvelle instance de la classe <see cref="ApplicationDbContext"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        { }

        /// <value>
        /// Collection de  promotions.
        /// </value>
        public DbSet<Offer> Offers { get; set; }

        /// <value>
        /// Collection de produits.
        /// </value>
        public DbSet<Product> Products { get; set; }

        /// <summary>
        /// Configure le schéma de la base de données.
        /// </summary>
        /// <param name="builder">Le constructeur utilisé pour construire les modèles pour ce contexte.</param>
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
            productBuilder
                .Property(_ => _.Image)
                .UsePropertyAccessMode(PropertyAccessMode.Property);
            productBuilder.Ignore(_ => _.ImageStream);
            productBuilder.Property(_ => _.Category).IsRequired();
            productBuilder.HasMany(_ => _.Offers).WithMany(_ => _.Products);
        }
    }
}
