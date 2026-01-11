using EfCoreOptimizations.Controllers;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Controllers;

public class DatabaseControllerTests
{
    private AppDbContext _context = null!;
    private DatabaseController _controller = null!;
    private Mock<ILogger<DatabaseController>> _loggerMock = null!;
    private Mock<DataSeeder> _seederMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<DatabaseController>>();

        var seederLoggerMock = new Mock<ILogger<DataSeeder>>();
        var seeder = new DataSeeder(_context, seederLoggerMock.Object);

        _controller = new DatabaseController(_context, _loggerMock.Object, seeder);

        await SeedTestData();
    }

    private async Task SeedTestData()
    {
        var categories = new[]
        {
            new Category { Id = 1, Name = "Electronics", Slug = "electronics", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Category { Id = 2, Name = "Clothing", Slug = "clothing", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.Categories.AddRangeAsync(categories);

        var products = new[]
        {
            new Product { Id = 1, Name = "iPhone", SKU = "SKU-001", Price = 999.99m, CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Samsung", SKU = "SKU-002", Price = 899.99m, CategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "Inactive", SKU = "SKU-003", Price = 100m, CategoryId = 1, IsActive = false, CreatedAt = DateTime.UtcNow }
        };
        await _context.Products.AddRangeAsync(products);

        var customers = new[]
        {
            new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", Country = "USA", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", Country = "UK", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 3, FirstName = "Inactive", LastName = "User", Email = "inactive@test.com", Country = "USA", IsActive = false, CreatedAt = DateTime.UtcNow }
        };
        await _context.Customers.AddRangeAsync(customers);

        var addresses = new[]
        {
            new Address { Id = 1, CustomerId = 1, City = "New York", Country = "USA", CreatedAt = DateTime.UtcNow }
        };
        await _context.Addresses.AddRangeAsync(addresses);

        var orders = new[]
        {
            new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 1, Status = OrderStatus.Delivered, TotalAmount = 100m, OrderDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow },
            new Order { Id = 2, OrderNumber = "ORD-002", CustomerId = 1, Status = OrderStatus.Pending, TotalAmount = 200m, OrderDate = DateTime.UtcNow, CreatedAt = DateTime.UtcNow }
        };
        await _context.Orders.AddRangeAsync(orders);

        var orderItems = new[]
        {
            new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 100m, TotalPrice = 100m }
        };
        await _context.OrderItems.AddRangeAsync(orderItems);

        var reviews = new[]
        {
            new Review { Id = 1, ProductId = 1, CustomerId = 1, Rating = 5, Title = "Great", CreatedAt = DateTime.UtcNow },
            new Review { Id = 2, ProductId = 1, CustomerId = 2, Rating = 4, Title = "Good", CreatedAt = DateTime.UtcNow }
        };
        await _context.Reviews.AddRangeAsync(reviews);

        await _context.SaveChangesAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task GetStatistics_ShouldReturnCorrectCounts()
    {
        // Act
        var result = await _controller.GetStatistics();

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var stats = okResult.Value;

        await Assert.That(stats).IsNotNull();

        // Use reflection to check properties
        var type = stats!.GetType();

        var customersCount = (int)type.GetProperty("Customers")!.GetValue(stats)!;
        await Assert.That(customersCount).IsEqualTo(3);

        var activeCustomersCount = (int)type.GetProperty("ActiveCustomers")!.GetValue(stats)!;
        await Assert.That(activeCustomersCount).IsEqualTo(2);

        var productsCount = (int)type.GetProperty("Products")!.GetValue(stats)!;
        await Assert.That(productsCount).IsEqualTo(3);

        var activeProductsCount = (int)type.GetProperty("ActiveProducts")!.GetValue(stats)!;
        await Assert.That(activeProductsCount).IsEqualTo(2);
    }

    [Test]
    public async Task GetStatistics_ShouldIncludeOrdersCount()
    {
        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = (OkObjectResult)result;
        var stats = okResult.Value;
        var type = stats!.GetType();

        var ordersCount = (int)type.GetProperty("Orders")!.GetValue(stats)!;
        await Assert.That(ordersCount).IsEqualTo(2);
    }

    [Test]
    public async Task GetStatistics_ShouldIncludeReviewsCount()
    {
        // Act
        var result = await _controller.GetStatistics();

        // Assert
        var okResult = (OkObjectResult)result;
        var stats = okResult.Value;
        var type = stats!.GetType();

        var reviewsCount = (int)type.GetProperty("Reviews")!.GetValue(stats)!;
        await Assert.That(reviewsCount).IsEqualTo(2);
    }

    [Test]
    public async Task GetTableInfo_ShouldReturnTableInformation()
    {
        // Act
        var result = await _controller.GetTableInfo();

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var info = okResult.Value;

        await Assert.That(info).IsNotNull();

        var type = info!.GetType();
        var tables = type.GetProperty("Tables")!.GetValue(info);
        await Assert.That(tables).IsNotNull();
    }

    [Test]
    public async Task GetSampleData_ShouldReturnSamples()
    {
        // Act
        var result = await _controller.GetSampleData();

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var samples = okResult.Value;

        await Assert.That(samples).IsNotNull();
    }

    [Test]
    public async Task GetSampleData_ShouldContainCustomers()
    {
        // Act
        var result = await _controller.GetSampleData();

        // Assert
        var okResult = (OkObjectResult)result;
        var samples = okResult.Value;
        var type = samples!.GetType();

        var customers = type.GetProperty("Customers")!.GetValue(samples);
        await Assert.That(customers).IsNotNull();
    }

    [Test]
    public async Task GetSampleData_ShouldContainProducts()
    {
        // Act
        var result = await _controller.GetSampleData();

        // Assert
        var okResult = (OkObjectResult)result;
        var samples = okResult.Value;
        var type = samples!.GetType();

        var products = type.GetProperty("Products")!.GetValue(samples);
        await Assert.That(products).IsNotNull();
    }

    [Test]
    public async Task CheckHealth_ShouldReturnHealthyStatus()
    {
        // Act
        var result = await _controller.CheckHealth();

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result;
        var health = okResult.Value;

        await Assert.That(health).IsNotNull();

        var type = health!.GetType();
        var status = (string)type.GetProperty("status")!.GetValue(health)!;
        await Assert.That(status).IsEqualTo("Healthy");
    }

    [Test]
    public async Task CheckHealth_ShouldIndicateDatabaseConnected()
    {
        // Act
        var result = await _controller.CheckHealth();

        // Assert
        var okResult = (OkObjectResult)result;
        var health = okResult.Value;
        var type = health!.GetType();

        var connected = (bool)type.GetProperty("databaseConnected")!.GetValue(health)!;
        await Assert.That(connected).IsTrue();
    }

    [Test]
    public async Task CheckHealth_ShouldIndicateHasData()
    {
        // Act
        var result = await _controller.CheckHealth();

        // Assert
        var okResult = (OkObjectResult)result;
        var health = okResult.Value;
        var type = health!.GetType();

        var hasData = (bool)type.GetProperty("hasData")!.GetValue(health)!;
        await Assert.That(hasData).IsTrue();
    }

    [Test]
    public async Task ExecuteSql_WithEmptyQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ExecuteSql("");

        // Assert
        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task ExecuteSql_WithNullQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ExecuteSql(null!);

        // Assert
        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task ExecuteSql_WithNonSelectQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ExecuteSql("DELETE FROM Customers");

        // Assert
        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task ExecuteSql_WithUpdateQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ExecuteSql("UPDATE Customers SET FirstName = 'Test'");

        // Assert
        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }

    [Test]
    public async Task ExecuteSql_WithInsertQuery_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.ExecuteSql("INSERT INTO Customers VALUES (1, 'Test')");

        // Assert
        await Assert.That(result).IsTypeOf<BadRequestObjectResult>();
    }
}
