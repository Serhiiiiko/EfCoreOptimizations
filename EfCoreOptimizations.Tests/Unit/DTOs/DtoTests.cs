using EfCoreOptimizations.DTOs;

namespace EfCoreOptimizations.Tests.Unit.DTOs;

public class CustomerListDtoTests
{
    [Test]
    public async Task CustomerListDto_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var dto = new CustomerListDto();

        // Assert
        await Assert.That(dto.Id).IsEqualTo(0);
        await Assert.That(dto.FullName).IsEqualTo(string.Empty);
        await Assert.That(dto.Email).IsEqualTo(string.Empty);
        await Assert.That(dto.Country).IsEqualTo(string.Empty);
        await Assert.That(dto.TotalOrders).IsEqualTo(0);
    }

    [Test]
    public async Task CustomerListDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var dto = new CustomerListDto
        {
            Id = 1,
            FullName = "John Doe",
            Email = "john.doe@example.com",
            Country = "USA",
            TotalOrders = 10
        };

        // Assert
        await Assert.That(dto.Id).IsEqualTo(1);
        await Assert.That(dto.FullName).IsEqualTo("John Doe");
        await Assert.That(dto.Email).IsEqualTo("john.doe@example.com");
        await Assert.That(dto.Country).IsEqualTo("USA");
        await Assert.That(dto.TotalOrders).IsEqualTo(10);
    }
}

public class CustomerDetailDtoTests
{
    [Test]
    public async Task CustomerDetailDto_RecentOrders_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var dto = new CustomerDetailDto();

        // Assert
        await Assert.That(dto.RecentOrders).IsNotNull();
        await Assert.That(dto.RecentOrders.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CustomerDetailDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var orders = new List<OrderSummaryDto>
        {
            new() { Id = 1, OrderNumber = "ORD-001", TotalAmount = 100m }
        };

        var dto = new CustomerDetailDto
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Country = "USA",
            City = "New York",
            CreditLimit = 5000m,
            TotalOrders = 5,
            RecentOrders = orders
        };

        // Assert
        await Assert.That(dto.FirstName).IsEqualTo("John");
        await Assert.That(dto.LastName).IsEqualTo("Doe");
        await Assert.That(dto.RecentOrders.Count).IsEqualTo(1);
    }
}

public class OrderSummaryDtoTests
{
    [Test]
    public async Task OrderSummaryDto_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var dto = new OrderSummaryDto();

        // Assert
        await Assert.That(dto.Id).IsEqualTo(0);
        await Assert.That(dto.OrderNumber).IsEqualTo(string.Empty);
        await Assert.That(dto.Status).IsEqualTo(string.Empty);
        await Assert.That(dto.TotalAmount).IsEqualTo(0m);
        await Assert.That(dto.ItemCount).IsEqualTo(0);
    }

    [Test]
    public async Task OrderSummaryDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var orderDate = DateTime.UtcNow;

        var dto = new OrderSummaryDto
        {
            Id = 1,
            OrderNumber = "ORD-12345",
            OrderDate = orderDate,
            Status = "Delivered",
            TotalAmount = 299.99m,
            ItemCount = 3
        };

        // Assert
        await Assert.That(dto.Id).IsEqualTo(1);
        await Assert.That(dto.OrderNumber).IsEqualTo("ORD-12345");
        await Assert.That(dto.OrderDate).IsEqualTo(orderDate);
        await Assert.That(dto.Status).IsEqualTo("Delivered");
        await Assert.That(dto.TotalAmount).IsEqualTo(299.99m);
        await Assert.That(dto.ItemCount).IsEqualTo(3);
    }
}

public class OrderDetailDtoTests
{
    [Test]
    public async Task OrderDetailDto_Items_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var dto = new OrderDetailDto();

        // Assert
        await Assert.That(dto.Items).IsNotNull();
        await Assert.That(dto.Items.Count).IsEqualTo(0);
    }

    [Test]
    public async Task OrderDetailDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var items = new List<OrderItemDto>
        {
            new() { Id = 1, ProductName = "iPhone", Quantity = 1, UnitPrice = 999.99m }
        };

        var dto = new OrderDetailDto
        {
            Id = 1,
            OrderNumber = "ORD-001",
            Status = "Processing",
            CustomerName = "John Doe",
            TotalAmount = 999.99m,
            Items = items
        };

        // Assert
        await Assert.That(dto.CustomerName).IsEqualTo("John Doe");
        await Assert.That(dto.Items.Count).IsEqualTo(1);
        await Assert.That(dto.Items[0].ProductName).IsEqualTo("iPhone");
    }
}

public class ProductListDtoTests
{
    [Test]
    public async Task ProductListDto_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var dto = new ProductListDto();

        // Assert
        await Assert.That(dto.Id).IsEqualTo(0);
        await Assert.That(dto.Name).IsEqualTo(string.Empty);
        await Assert.That(dto.SKU).IsEqualTo(string.Empty);
        await Assert.That(dto.Price).IsEqualTo(0m);
        await Assert.That(dto.CategoryName).IsEqualTo(string.Empty);
        await Assert.That(dto.AverageRating).IsEqualTo(0m);
        await Assert.That(dto.ReviewCount).IsEqualTo(0);
        await Assert.That(dto.IsInStock).IsFalse();
    }

    [Test]
    public async Task ProductListDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var dto = new ProductListDto
        {
            Id = 1,
            Name = "iPhone 15",
            SKU = "SKU-IPHONE15",
            Price = 999.99m,
            CategoryName = "Phones",
            AverageRating = 4.5m,
            ReviewCount = 100,
            IsInStock = true
        };

        // Assert
        await Assert.That(dto.Name).IsEqualTo("iPhone 15");
        await Assert.That(dto.IsInStock).IsTrue();
        await Assert.That(dto.AverageRating).IsEqualTo(4.5m);
    }
}

public class ProductDetailDtoTests
{
    [Test]
    public async Task ProductDetailDto_RecentReviews_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var dto = new ProductDetailDto();

        // Assert
        await Assert.That(dto.RecentReviews).IsNotNull();
        await Assert.That(dto.RecentReviews.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ProductDetailDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var reviews = new List<ReviewDto>
        {
            new() { Id = 1, Rating = 5, Title = "Great!", CustomerName = "John" }
        };

        var dto = new ProductDetailDto
        {
            Id = 1,
            Name = "iPhone 15",
            SKU = "SKU-001",
            Description = "Latest iPhone",
            Price = 999.99m,
            StockQuantity = 100,
            CategoryName = "Phones",
            Manufacturer = "Apple",
            AverageRating = 4.8m,
            ReviewCount = 50,
            RecentReviews = reviews
        };

        // Assert
        await Assert.That(dto.Manufacturer).IsEqualTo("Apple");
        await Assert.That(dto.RecentReviews.Count).IsEqualTo(1);
    }
}

public class ReviewDtoTests
{
    [Test]
    public async Task ReviewDto_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var dto = new ReviewDto();

        // Assert
        await Assert.That(dto.Id).IsEqualTo(0);
        await Assert.That(dto.CustomerName).IsEqualTo(string.Empty);
        await Assert.That(dto.Rating).IsEqualTo(0);
        await Assert.That(dto.Title).IsEqualTo(string.Empty);
        await Assert.That(dto.Comment).IsEqualTo(string.Empty);
        await Assert.That(dto.IsVerifiedPurchase).IsFalse();
    }

    [Test]
    public async Task ReviewDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        var dto = new ReviewDto
        {
            Id = 1,
            CustomerName = "John Doe",
            Rating = 5,
            Title = "Excellent!",
            Comment = "Best product ever",
            CreatedAt = createdAt,
            IsVerifiedPurchase = true
        };

        // Assert
        await Assert.That(dto.CustomerName).IsEqualTo("John Doe");
        await Assert.That(dto.Rating).IsEqualTo(5);
        await Assert.That(dto.IsVerifiedPurchase).IsTrue();
    }
}

public class CustomerFullDtoTests
{
    [Test]
    public async Task CustomerFullDto_Collections_ShouldBeInitializedAsEmptyLists()
    {
        // Arrange & Act
        var dto = new CustomerFullDto();

        // Assert
        await Assert.That(dto.Orders).IsNotNull();
        await Assert.That(dto.Orders.Count).IsEqualTo(0);
        await Assert.That(dto.Reviews).IsNotNull();
        await Assert.That(dto.Reviews.Count).IsEqualTo(0);
        await Assert.That(dto.Addresses).IsNotNull();
        await Assert.That(dto.Addresses.Count).IsEqualTo(0);
    }
}

public class AddressDtoTests
{
    [Test]
    public async Task AddressDto_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var dto = new AddressDto();

        // Assert
        await Assert.That(dto.Id).IsEqualTo(0);
        await Assert.That(dto.Street).IsEqualTo(string.Empty);
        await Assert.That(dto.City).IsEqualTo(string.Empty);
        await Assert.That(dto.State).IsEqualTo(string.Empty);
        await Assert.That(dto.Country).IsEqualTo(string.Empty);
        await Assert.That(dto.PostalCode).IsEqualTo(string.Empty);
        await Assert.That(dto.IsDefault).IsFalse();
        await Assert.That(dto.Type).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task AddressDto_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var dto = new AddressDto
        {
            Id = 1,
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            Country = "USA",
            PostalCode = "10001",
            IsDefault = true,
            Type = "Shipping"
        };

        // Assert
        await Assert.That(dto.Street).IsEqualTo("123 Main St");
        await Assert.That(dto.IsDefault).IsTrue();
        await Assert.That(dto.Type).IsEqualTo("Shipping");
    }
}
