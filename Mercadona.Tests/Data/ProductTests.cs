using Mercadona.Backend.Data;
using Shouldly;

namespace Mercadona.Tests.Data
{
    public class ProductTests
    {
        [Fact]
        public void Product_ContructWithGuid()
        {
            // Arrange
            Guid expectedId = Guid.NewGuid();

            // Act
            Product product =
                new(expectedId)
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Assert
            product.Id.ShouldBe(expectedId);
        }

        [Fact]
        public void Product_ContructWithoutGuid()
        {
            // Arrange
            Guid anId = Guid.NewGuid();

            // Act
            Product product =
                new()
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Assert
            product.Id.ShouldNotBe(anId);
        }

        [Fact]
        public void Product_SetImageStream()
        {
            // Arrange
            string filePath = "./Resources/validImage.jpeg";
            long expectedFileSize = new FileInfo(filePath).Length;

            // Act
            Product product =
                new(() => File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };

            // Assert
            product.Image.LongLength.ShouldBe(expectedFileSize);
        }

        [Fact]
        public void Product_SetImage()
        {
            // Arrange
            string filePath = "./Resources/validImage.jpeg";
            long expectedFileSize = new FileInfo(filePath).Length;
            using FileStream fileStream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite
            );
            using MemoryStream expectedStream = new();
            fileStream.CopyTo(expectedStream);
            fileStream.Seek(0, SeekOrigin.Begin);

            // Act
            Product product =
                new()
                {
                    Label = "Mon produit",
                    Description = "Un produit",
                    Price = 1M,
                    Category = "Surgelé"
                };
            product.Image = expectedStream.ToArray();

            // Assert
            product.ImageStream.Length.ShouldBe(expectedFileSize);
        }
    }
}
