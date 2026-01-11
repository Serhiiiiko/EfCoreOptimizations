using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class ReviewTests
{
    [Test]
    public async Task Review_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var review = new Review();

        // Assert
        await Assert.That(review.Id).IsEqualTo(0);
        await Assert.That(review.ProductId).IsEqualTo(0);
        await Assert.That(review.CustomerId).IsEqualTo(0);
        await Assert.That(review.Rating).IsEqualTo(0);
        await Assert.That(review.Title).IsEqualTo(string.Empty);
        await Assert.That(review.Comment).IsEqualTo(string.Empty);
        await Assert.That(review.IsVerifiedPurchase).IsFalse();
        await Assert.That(review.HelpfulCount).IsEqualTo(0);
        await Assert.That(review.UnhelpfulCount).IsEqualTo(0);
    }

    [Test]
    public async Task Review_UpdatedAt_ShouldBeNullable()
    {
        // Arrange
        var review = new Review();

        // Assert
        await Assert.That(review.UpdatedAt).IsNull();

        // Act
        review.UpdatedAt = DateTime.UtcNow;

        // Assert
        await Assert.That(review.UpdatedAt).IsNotNull();
    }

    [Test]
    public async Task Review_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow.AddDays(-7);
        var updatedAt = DateTime.UtcNow;

        var review = new Review
        {
            Id = 1,
            ProductId = 100,
            CustomerId = 50,
            Rating = 5,
            Title = "Excellent Product!",
            Comment = "This is the best product I have ever purchased.",
            IsVerifiedPurchase = true,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            HelpfulCount = 25,
            UnhelpfulCount = 2
        };

        // Assert
        await Assert.That(review.Id).IsEqualTo(1);
        await Assert.That(review.ProductId).IsEqualTo(100);
        await Assert.That(review.CustomerId).IsEqualTo(50);
        await Assert.That(review.Rating).IsEqualTo(5);
        await Assert.That(review.Title).IsEqualTo("Excellent Product!");
        await Assert.That(review.Comment).IsEqualTo("This is the best product I have ever purchased.");
        await Assert.That(review.IsVerifiedPurchase).IsTrue();
        await Assert.That(review.CreatedAt).IsEqualTo(createdAt);
        await Assert.That(review.UpdatedAt).IsEqualTo(updatedAt);
        await Assert.That(review.HelpfulCount).IsEqualTo(25);
        await Assert.That(review.UnhelpfulCount).IsEqualTo(2);
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    public async Task Review_Rating_ShouldAcceptValidValues(int rating)
    {
        // Arrange
        var review = new Review();

        // Act
        review.Rating = rating;

        // Assert
        await Assert.That(review.Rating).IsEqualTo(rating);
    }

    [Test]
    public async Task Review_HelpfulnessRatio_CanBeCalculated()
    {
        // Arrange
        var review = new Review
        {
            HelpfulCount = 80,
            UnhelpfulCount = 20
        };

        // Act
        var totalVotes = review.HelpfulCount + review.UnhelpfulCount;
        var helpfulnessRatio = totalVotes > 0 ? (double)review.HelpfulCount / totalVotes : 0;

        // Assert
        await Assert.That(totalVotes).IsEqualTo(100);
        await Assert.That(helpfulnessRatio).IsEqualTo(0.8);
    }

    [Test]
    public async Task Review_NavigationProperties_ShouldBeAssignable()
    {
        // Arrange
        var product = new Product { Id = 1, Name = "Test Product" };
        var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };

        var review = new Review
        {
            Id = 1,
            ProductId = 1,
            CustomerId = 1,
            Product = product,
            Customer = customer
        };

        // Assert
        await Assert.That(review.Product).IsNotNull();
        await Assert.That(review.Product.Name).IsEqualTo("Test Product");
        await Assert.That(review.Customer).IsNotNull();
        await Assert.That(review.Customer.FirstName).IsEqualTo("John");
    }
}
