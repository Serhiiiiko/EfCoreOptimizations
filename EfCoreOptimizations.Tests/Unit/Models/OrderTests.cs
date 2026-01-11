using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class OrderTests
{
    [Test]
    public async Task Order_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        await Assert.That(order.Id).IsEqualTo(0);
        await Assert.That(order.OrderNumber).IsEqualTo(string.Empty);
        await Assert.That(order.CustomerId).IsEqualTo(0);
        await Assert.That(order.Status).IsEqualTo(OrderStatus.Pending);
        await Assert.That(order.TotalAmount).IsEqualTo(0m);
        await Assert.That(order.ShippingCost).IsEqualTo(0m);
        await Assert.That(order.Tax).IsEqualTo(0m);
        await Assert.That(order.ShippingAddress).IsEqualTo(string.Empty);
        await Assert.That(order.BillingAddress).IsEqualTo(string.Empty);
        await Assert.That(order.Notes).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Order_NavigationProperty_OrderItems_ShouldBeInitializedAsEmptyList()
    {
        // Arrange & Act
        var order = new Order();

        // Assert
        await Assert.That(order.OrderItems).IsNotNull();
        await Assert.That(order.OrderItems.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Order_ShippedDate_ShouldBeNullable()
    {
        // Arrange
        var order = new Order();

        // Assert
        await Assert.That(order.ShippedDate).IsNull();

        // Act
        order.ShippedDate = DateTime.UtcNow;

        // Assert
        await Assert.That(order.ShippedDate).IsNotNull();
    }

    [Test]
    public async Task Order_UpdatedAt_ShouldBeNullable()
    {
        // Arrange
        var order = new Order();

        // Assert
        await Assert.That(order.UpdatedAt).IsNull();
    }

    [Test]
    public async Task Order_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var orderDate = DateTime.UtcNow.AddDays(-5);
        var shippedDate = DateTime.UtcNow.AddDays(-2);

        var order = new Order
        {
            Id = 1,
            OrderNumber = "ORD-12345",
            CustomerId = 100,
            OrderDate = orderDate,
            ShippedDate = shippedDate,
            Status = OrderStatus.Shipped,
            TotalAmount = 1500.50m,
            ShippingCost = 25.00m,
            Tax = 150.05m,
            ShippingAddress = "123 Main St",
            BillingAddress = "456 Bill Ave",
            Notes = "Handle with care"
        };

        // Assert
        await Assert.That(order.Id).IsEqualTo(1);
        await Assert.That(order.OrderNumber).IsEqualTo("ORD-12345");
        await Assert.That(order.CustomerId).IsEqualTo(100);
        await Assert.That(order.OrderDate).IsEqualTo(orderDate);
        await Assert.That(order.ShippedDate).IsEqualTo(shippedDate);
        await Assert.That(order.Status).IsEqualTo(OrderStatus.Shipped);
        await Assert.That(order.TotalAmount).IsEqualTo(1500.50m);
        await Assert.That(order.ShippingCost).IsEqualTo(25.00m);
        await Assert.That(order.Tax).IsEqualTo(150.05m);
        await Assert.That(order.ShippingAddress).IsEqualTo("123 Main St");
        await Assert.That(order.BillingAddress).IsEqualTo("456 Bill Ave");
        await Assert.That(order.Notes).IsEqualTo("Handle with care");
    }

    [Test]
    public async Task Order_AddOrderItem_ShouldAddToCollection()
    {
        // Arrange
        var order = new Order { Id = 1, OrderNumber = "ORD-001" };
        var orderItem = new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 2 };

        // Act
        order.OrderItems.Add(orderItem);

        // Assert
        await Assert.That(order.OrderItems.Count).IsEqualTo(1);
        await Assert.That(order.OrderItems.First().Quantity).IsEqualTo(2);
    }

    [Test]
    [Arguments(OrderStatus.Pending)]
    [Arguments(OrderStatus.Processing)]
    [Arguments(OrderStatus.Shipped)]
    [Arguments(OrderStatus.Delivered)]
    [Arguments(OrderStatus.Cancelled)]
    [Arguments(OrderStatus.Refunded)]
    public async Task Order_AllStatusValues_ShouldBeAssignable(OrderStatus status)
    {
        // Arrange
        var order = new Order();

        // Act
        order.Status = status;

        // Assert
        await Assert.That(order.Status).IsEqualTo(status);
    }
}

public class OrderStatusTests
{
    [Test]
    public async Task OrderStatus_ShouldHaveCorrectValues()
    {
        // Assert
        await Assert.That((int)OrderStatus.Pending).IsEqualTo(0);
        await Assert.That((int)OrderStatus.Processing).IsEqualTo(1);
        await Assert.That((int)OrderStatus.Shipped).IsEqualTo(2);
        await Assert.That((int)OrderStatus.Delivered).IsEqualTo(3);
        await Assert.That((int)OrderStatus.Cancelled).IsEqualTo(4);
        await Assert.That((int)OrderStatus.Refunded).IsEqualTo(5);
    }

    [Test]
    public async Task OrderStatus_ShouldHave6Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<OrderStatus>();

        // Assert
        await Assert.That(values.Length).IsEqualTo(6);
    }
}
