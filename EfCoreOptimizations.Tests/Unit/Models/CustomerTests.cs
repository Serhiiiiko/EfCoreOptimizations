using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class CustomerTests
{
    [Test]
    public async Task Customer_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var customer = new Customer();

        // Assert
        await Assert.That(customer.Id).IsEqualTo(0);
        await Assert.That(customer.FirstName).IsEqualTo(string.Empty);
        await Assert.That(customer.LastName).IsEqualTo(string.Empty);
        await Assert.That(customer.Email).IsEqualTo(string.Empty);
        await Assert.That(customer.Phone).IsEqualTo(string.Empty);
        await Assert.That(customer.City).IsEqualTo(string.Empty);
        await Assert.That(customer.Country).IsEqualTo(string.Empty);
        await Assert.That(customer.IsActive).IsFalse();
        await Assert.That(customer.CreditLimit).IsEqualTo(0m);
        await Assert.That(customer.TotalOrders).IsEqualTo(0);
    }

    [Test]
    public async Task Customer_NavigationProperties_ShouldBeInitializedAsEmptyCollections()
    {
        // Arrange & Act
        var customer = new Customer();

        // Assert
        await Assert.That(customer.Orders).IsNotNull();
        await Assert.That(customer.Orders.Count).IsEqualTo(0);
        await Assert.That(customer.Reviews).IsNotNull();
        await Assert.That(customer.Reviews.Count).IsEqualTo(0);
        await Assert.That(customer.Addresses).IsNotNull();
        await Assert.That(customer.Addresses.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Customer_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var customer = new Customer
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "123-456-7890",
            City = "New York",
            Country = "USA",
            DateOfBirth = new DateTime(1990, 1, 15),
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            IsActive = true,
            CreditLimit = 5000m,
            TotalOrders = 10
        };

        // Assert
        await Assert.That(customer.Id).IsEqualTo(1);
        await Assert.That(customer.FirstName).IsEqualTo("John");
        await Assert.That(customer.LastName).IsEqualTo("Doe");
        await Assert.That(customer.Email).IsEqualTo("john.doe@example.com");
        await Assert.That(customer.Phone).IsEqualTo("123-456-7890");
        await Assert.That(customer.City).IsEqualTo("New York");
        await Assert.That(customer.Country).IsEqualTo("USA");
        await Assert.That(customer.IsActive).IsTrue();
        await Assert.That(customer.CreditLimit).IsEqualTo(5000m);
        await Assert.That(customer.TotalOrders).IsEqualTo(10);
    }

    [Test]
    public async Task Customer_LastLoginAt_ShouldBeNullable()
    {
        // Arrange
        var customer = new Customer();

        // Assert
        await Assert.That(customer.LastLoginAt).IsNull();

        // Act
        customer.LastLoginAt = DateTime.UtcNow;

        // Assert
        await Assert.That(customer.LastLoginAt).IsNotNull();
    }

    [Test]
    public async Task Customer_AddOrder_ShouldAddToCollection()
    {
        // Arrange
        var customer = new Customer { Id = 1, FirstName = "Test", LastName = "User" };
        var order = new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 1 };

        // Act
        customer.Orders.Add(order);

        // Assert
        await Assert.That(customer.Orders.Count).IsEqualTo(1);
        await Assert.That(customer.Orders.First().OrderNumber).IsEqualTo("ORD-001");
    }
}
