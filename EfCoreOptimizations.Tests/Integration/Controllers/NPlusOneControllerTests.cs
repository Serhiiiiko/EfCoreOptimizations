using EfCoreOptimizations.Controllers;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using EfCoreOptimizations.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Controllers;

public class NPlusOneControllerTests
{
    private AppDbContext _context = null!;
    private NPlusOneController _controller = null!;
    private Mock<ILogger<NPlusOneController>> _loggerMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<NPlusOneController>>();
        _controller = new NPlusOneController(_context, _loggerMock.Object);

        var fixture = new TestDbContextFixture();
        // Copy test data to our context
        await SeedTestData();
    }

    private async Task SeedTestData()
    {
        var fixture = new TestDbContextFixture();

        // Seed directly into our test context
        var categories = new[]
        {
            new EfCoreOptimizations.Models.Category { Id = 1, Name = "Electronics", Slug = "electronics", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.Categories.AddRangeAsync(categories);

        var products = new[]
        {
            new EfCoreOptimizations.Models.Product { Id = 1, Name = "iPhone 15", SKU = "SKU-IPHONE15", Price = 999.99m, CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.Products.AddRangeAsync(products);

        var customers = new[]
        {
            new EfCoreOptimizations.Models.Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", City = "New York", Country = "USA", IsActive = true, TotalOrders = 2, CreditLimit = 5000m, CreatedAt = DateTime.UtcNow },
            new EfCoreOptimizations.Models.Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", City = "London", Country = "UK", IsActive = true, TotalOrders = 1, CreditLimit = 10000m, CreatedAt = DateTime.UtcNow },
            new EfCoreOptimizations.Models.Customer { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob@test.com", City = "Toronto", Country = "Canada", IsActive = false, TotalOrders = 0, CreditLimit = 2000m, CreatedAt = DateTime.UtcNow }
        };
        await _context.Customers.AddRangeAsync(customers);

        var orders = new[]
        {
            new EfCoreOptimizations.Models.Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-30), Status = EfCoreOptimizations.Models.OrderStatus.Delivered, TotalAmount = 1029.98m, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new EfCoreOptimizations.Models.Order { Id = 2, OrderNumber = "ORD-002", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-15), Status = EfCoreOptimizations.Models.OrderStatus.Shipped, TotalAmount = 29.99m, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new EfCoreOptimizations.Models.Order { Id = 3, OrderNumber = "ORD-003", CustomerId = 2, OrderDate = DateTime.UtcNow.AddDays(-7), Status = EfCoreOptimizations.Models.OrderStatus.Processing, TotalAmount = 949.98m, CreatedAt = DateTime.UtcNow.AddDays(-7) }
        };
        await _context.Orders.AddRangeAsync(orders);

        var orderItems = new[]
        {
            new EfCoreOptimizations.Models.OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m, TotalPrice = 999.99m },
            new EfCoreOptimizations.Models.OrderItem { Id = 2, OrderId = 2, ProductId = 1, Quantity = 1, UnitPrice = 29.99m, TotalPrice = 29.99m },
            new EfCoreOptimizations.Models.OrderItem { Id = 3, OrderId = 3, ProductId = 1, Quantity = 1, UnitPrice = 949.98m, TotalPrice = 949.98m }
        };
        await _context.OrderItems.AddRangeAsync(orderItems);

        await _context.SaveChangesAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task GetCustomersWithOrdersBad_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersBad(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(2); // Only active customers
        await Assert.That(customers.All(c => c.FirstName != "Bob")).IsTrue(); // Bob is inactive
    }

    [Test]
    public async Task GetCustomersWithOrdersGood_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersGood(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(2); // Only active customers
    }

    [Test]
    public async Task GetCustomersWithOrdersBad_ShouldIncludeRecentOrders()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersBad(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        var johnCustomer = customers.FirstOrDefault(c => c.FirstName == "John");
        await Assert.That(johnCustomer).IsNotNull();
        await Assert.That(johnCustomer!.RecentOrders.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GetCustomersWithOrdersGood_ShouldIncludeRecentOrders()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersGood(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        var johnCustomer = customers.FirstOrDefault(c => c.FirstName == "John");
        await Assert.That(johnCustomer).IsNotNull();
        await Assert.That(johnCustomer!.RecentOrders.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task GetCustomersWithOrdersBad_WithTakeParameter_ShouldLimitResults()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersBad(take: 1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        await Assert.That(customers.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetCustomersWithOrdersGood_WithTakeParameter_ShouldLimitResults()
    {
        // Act
        var result = await _controller.GetCustomersWithOrdersGood(take: 1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerDetailDto>)okResult.Value!;

        await Assert.That(customers.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetOrdersWithItemsBad_ShouldReturnOrders()
    {
        // Act
        var result = await _controller.GetOrdersWithItemsBad(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderDetailDto>)okResult.Value!;

        await Assert.That(orders).IsNotNull();
        await Assert.That(orders.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetOrdersWithItemsGood_ShouldReturnOrders()
    {
        // Act
        var result = await _controller.GetOrdersWithItemsGood(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderDetailDto>)okResult.Value!;

        await Assert.That(orders).IsNotNull();
        await Assert.That(orders.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetOrdersWithItemsBad_ShouldIncludeOrderItems()
    {
        // Act
        var result = await _controller.GetOrdersWithItemsBad(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderDetailDto>)okResult.Value!;

        await Assert.That(orders.Any(o => o.Items.Count > 0)).IsTrue();
    }

    [Test]
    public async Task GetOrdersWithItemsGood_ShouldIncludeOrderItems()
    {
        // Act
        var result = await _controller.GetOrdersWithItemsGood(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderDetailDto>)okResult.Value!;

        await Assert.That(orders.Any(o => o.Items.Count > 0)).IsTrue();
    }

    [Test]
    public async Task GetOrdersWithItemsGood_ShouldIncludeCustomerName()
    {
        // Act
        var result = await _controller.GetOrdersWithItemsGood(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderDetailDto>)okResult.Value!;

        await Assert.That(orders.All(o => !string.IsNullOrEmpty(o.CustomerName))).IsTrue();
    }

    [Test]
    public async Task GetOrdersWithItems_BothMethods_ShouldReturnSameData()
    {
        // Act
        var badResult = await _controller.GetOrdersWithItemsBad(take: 10);
        var goodResult = await _controller.GetOrdersWithItemsGood(take: 10);

        // Assert
        var badOrders = (List<OrderDetailDto>)((OkObjectResult)badResult.Result!).Value!;
        var goodOrders = (List<OrderDetailDto>)((OkObjectResult)goodResult.Result!).Value!;

        await Assert.That(badOrders.Count).IsEqualTo(goodOrders.Count);
    }
}
