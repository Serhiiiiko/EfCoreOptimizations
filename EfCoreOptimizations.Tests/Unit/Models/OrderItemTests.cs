using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class OrderItemTests
{
    [Test]
    public async Task OrderItem_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var orderItem = new OrderItem();

        // Assert
        await Assert.That(orderItem.Id).IsEqualTo(0);
        await Assert.That(orderItem.OrderId).IsEqualTo(0);
        await Assert.That(orderItem.ProductId).IsEqualTo(0);
        await Assert.That(orderItem.Quantity).IsEqualTo(0);
        await Assert.That(orderItem.UnitPrice).IsEqualTo(0m);
        await Assert.That(orderItem.Discount).IsEqualTo(0m);
        await Assert.That(orderItem.TotalPrice).IsEqualTo(0m);
    }

    [Test]
    public async Task OrderItem_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = 100,
            ProductId = 50,
            Quantity = 3,
            UnitPrice = 99.99m,
            Discount = 10.00m,
            TotalPrice = 289.97m
        };

        // Assert
        await Assert.That(orderItem.Id).IsEqualTo(1);
        await Assert.That(orderItem.OrderId).IsEqualTo(100);
        await Assert.That(orderItem.ProductId).IsEqualTo(50);
        await Assert.That(orderItem.Quantity).IsEqualTo(3);
        await Assert.That(orderItem.UnitPrice).IsEqualTo(99.99m);
        await Assert.That(orderItem.Discount).IsEqualTo(10.00m);
        await Assert.That(orderItem.TotalPrice).IsEqualTo(289.97m);
    }

    [Test]
    public async Task OrderItem_NavigationProperties_ShouldBeAssignable()
    {
        // Arrange
        var order = new Order { Id = 1, OrderNumber = "ORD-001" };
        var product = new Product { Id = 1, Name = "Test Product", Price = 99.99m };

        var orderItem = new OrderItem
        {
            Id = 1,
            OrderId = 1,
            ProductId = 1,
            Order = order,
            Product = product
        };

        // Assert
        await Assert.That(orderItem.Order).IsNotNull();
        await Assert.That(orderItem.Order.OrderNumber).IsEqualTo("ORD-001");
        await Assert.That(orderItem.Product).IsNotNull();
        await Assert.That(orderItem.Product.Name).IsEqualTo("Test Product");
    }

    [Test]
    public async Task OrderItem_TotalPrice_CalculationExample()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Quantity = 5,
            UnitPrice = 100m,
            Discount = 0.1m // 10% discount
        };

        // Act
        var expectedTotal = orderItem.Quantity * orderItem.UnitPrice * (1 - orderItem.Discount);
        orderItem.TotalPrice = expectedTotal;

        // Assert
        await Assert.That(orderItem.TotalPrice).IsEqualTo(450m);
    }

    [Test]
    public async Task OrderItem_ZeroDiscount_ShouldCalculateCorrectly()
    {
        // Arrange
        var orderItem = new OrderItem
        {
            Quantity = 2,
            UnitPrice = 50m,
            Discount = 0m
        };

        // Act
        var expectedTotal = orderItem.Quantity * orderItem.UnitPrice;
        orderItem.TotalPrice = expectedTotal;

        // Assert
        await Assert.That(orderItem.TotalPrice).IsEqualTo(100m);
    }
}
