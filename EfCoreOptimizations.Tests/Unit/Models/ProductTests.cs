using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class ProductTests
{
    [Test]
    public async Task Product_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        await Assert.That(product.Id).IsEqualTo(0);
        await Assert.That(product.Name).IsEqualTo(string.Empty);
        await Assert.That(product.SKU).IsEqualTo(string.Empty);
        await Assert.That(product.Description).IsEqualTo(string.Empty);
        await Assert.That(product.Price).IsEqualTo(0m);
        await Assert.That(product.Cost).IsEqualTo(0m);
        await Assert.That(product.StockQuantity).IsEqualTo(0);
        await Assert.That(product.CategoryId).IsEqualTo(0);
        await Assert.That(product.IsActive).IsFalse();
        await Assert.That(product.IsFeatured).IsFalse();
        await Assert.That(product.Weight).IsEqualTo(0m);
        await Assert.That(product.Manufacturer).IsEqualTo(string.Empty);
        await Assert.That(product.ViewCount).IsEqualTo(0);
        await Assert.That(product.AverageRating).IsEqualTo(0m);
        await Assert.That(product.ReviewCount).IsEqualTo(0);
    }

    [Test]
    public async Task Product_NavigationProperties_ShouldBeInitializedAsEmptyCollections()
    {
        // Arrange & Act
        var product = new Product();

        // Assert
        await Assert.That(product.OrderItems).IsNotNull();
        await Assert.That(product.OrderItems.Count).IsEqualTo(0);
        await Assert.That(product.Reviews).IsNotNull();
        await Assert.That(product.Reviews.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Product_UpdatedAt_ShouldBeNullable()
    {
        // Arrange
        var product = new Product();

        // Assert
        await Assert.That(product.UpdatedAt).IsNull();

        // Act
        product.UpdatedAt = DateTime.UtcNow;

        // Assert
        await Assert.That(product.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task Product_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-30);
        var updatedAt = DateTime.UtcNow;

        var product = new Product
        {
            Id = 1,
            Name = "iPhone 15 Pro",
            SKU = "SKU-IPHONE15PRO",
            Description = "Latest Apple smartphone",
            Price = 1199.99m,
            Cost = 800m,
            StockQuantity = 500,
            CategoryId = 10,
            IsActive = true,
            IsFeatured = true,
            Weight = 0.221m,
            Manufacturer = "Apple",
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            ViewCount = 15000,
            AverageRating = 4.8m,
            ReviewCount = 250
        };

        // Assert
        await Assert.That(product.Id).IsEqualTo(1);
        await Assert.That(product.Name).IsEqualTo("iPhone 15 Pro");
        await Assert.That(product.SKU).IsEqualTo("SKU-IPHONE15PRO");
        await Assert.That(product.Description).IsEqualTo("Latest Apple smartphone");
        await Assert.That(product.Price).IsEqualTo(1199.99m);
        await Assert.That(product.Cost).IsEqualTo(800m);
        await Assert.That(product.StockQuantity).IsEqualTo(500);
        await Assert.That(product.CategoryId).IsEqualTo(10);
        await Assert.That(product.IsActive).IsTrue();
        await Assert.That(product.IsFeatured).IsTrue();
        await Assert.That(product.Weight).IsEqualTo(0.221m);
        await Assert.That(product.Manufacturer).IsEqualTo("Apple");
        await Assert.That(product.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(product.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(product.ViewCount).IsEqualTo(15000);
        await Assert.That(product.AverageRating).IsEqualTo(4.8m);
        await Assert.That(product.ReviewCount).IsEqualTo(250);
    }

    [Test]
    public async Task Product_AddReview_ShouldAddToCollection()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product" };
        var review = new Review { Id = 1, ProductId = 1, CustomerId = 1, Rating = 5 };

        // Act
        product.Reviews.Add(review);

        // Assert
        await Assert.That(product.Reviews.Count).IsEqualTo(1);
        await Assert.That(product.Reviews.First().Rating).IsEqualTo(5);
    }

    [Test]
    public async Task Product_ProfitMargin_CanBeCalculated()
    {
        // Arrange
        var product = new Product
        {
            Price = 100m,
            Cost = 60m
        };

        // Act
        var margin = product.Price - product.Cost;
        var marginPercent = (margin / product.Price) * 100;

        // Assert
        await Assert.That(margin).IsEqualTo(40m);
        await Assert.That(marginPercent).IsEqualTo(40m);
    }
}
