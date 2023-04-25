using FluentAssertions;
using Mercadona.Backend.Data;

namespace Mercadona.Tests.Data
{
    public class ProductTests
    {
        [Fact]
        public void Product_ContructWithGuid_ShouldKeepGivenGuid()
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
            product.Id.Should().Be(expectedId);
        }

        [Fact]
        public void Product_ContructWithoutGuid_ShouldGenerateNewGuid()
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
            product.Id.Should().NotBe(anId);
        }

        [Fact]
        public void Product_SetImageStream_ShouldSyncImageField()
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
            product.Image.LongLength.Should().Be(expectedFileSize);
        }

        [Fact]
        public void Product_SetImage_ShouldSyncImageStreamField()
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
                    Category = "Surgelé",
                    Image = expectedStream.ToArray()
                };

            // Assert
            product.ImageStream.Length.Should().Be(expectedFileSize);
        }
    }
}
