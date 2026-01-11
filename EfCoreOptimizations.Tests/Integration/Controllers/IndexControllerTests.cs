using EfCoreOptimizations.Controllers;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using EfCoreOptimizations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Controllers;

public class IndexControllerTests
{
    private AppDbContext _context = null!;
    private IndexController _controller = null!;
    private Mock<ILogger<IndexController>> _loggerMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<IndexController>>();
        _controller = new IndexController(_context, _loggerMock.Object);

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
            new Product { Id = 1, Name = "iPhone 15", SKU = "SKU-001", Price = 999.99m, StockQuantity = 100, CategoryId = 1, IsActive = true, AverageRating = 4.5m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Samsung Galaxy", SKU = "SKU-002", Price = 899.99m, StockQuantity = 50, CategoryId = 1, IsActive = true, AverageRating = 4.3m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "MacBook Pro", SKU = "SKU-003", Price = 2499.99m, StockQuantity = 30, CategoryId = 1, IsActive = true, AverageRating = 4.8m, CreatedAt = DateTime.UtcNow },
            new Product { Id = 4, Name = "T-Shirt", SKU = "SKU-004", Price = 29.99m, StockQuantity = 500, CategoryId = 2, IsActive = true, AverageRating = 4.0m, CreatedAt = DateTime.UtcNow }
        };
        await _context.Products.AddRangeAsync(products);

        var customers = new[]
        {
            new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", City = "New York", Country = "USA", IsActive = true, TotalOrders = 5, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", City = "London", Country = "UK", IsActive = true, TotalOrders = 3, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@example.com", City = "Toronto", Country = "Canada", IsActive = true, TotalOrders = 2, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 4, FirstName = "Alice", LastName = "Brown", Email = "alice.brown@example.com", City = "New York", Country = "USA", IsActive = false, TotalOrders = 1, CreatedAt = DateTime.UtcNow }
        };
        await _context.Customers.AddRangeAsync(customers);

        var addresses = new[]
        {
            new Address { Id = 1, CustomerId = 1, Street = "123 Main St", City = "New York", State = "NY", Country = "USA", PostalCode = "10001", IsDefault = true, Type = AddressType.Both, CreatedAt = DateTime.UtcNow },
            new Address { Id = 2, CustomerId = 1, Street = "456 Work Ave", City = "New York", State = "NY", Country = "USA", PostalCode = "10002", IsDefault = false, Type = AddressType.Billing, CreatedAt = DateTime.UtcNow }
        };
        await _context.Addresses.AddRangeAsync(addresses);

        var orders = new[]
        {
            new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-30), Status = OrderStatus.Delivered, TotalAmount = 999.99m, CreatedAt = DateTime.UtcNow.AddDays(-30) },
            new Order { Id = 2, OrderNumber = "ORD-002", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-15), Status = OrderStatus.Shipped, TotalAmount = 29.99m, CreatedAt = DateTime.UtcNow.AddDays(-15) },
            new Order { Id = 3, OrderNumber = "ORD-003", CustomerId = 2, OrderDate = DateTime.UtcNow.AddDays(-7), Status = OrderStatus.Processing, TotalAmount = 899.99m, CreatedAt = DateTime.UtcNow.AddDays(-7) }
        };
        await _context.Orders.AddRangeAsync(orders);

        var orderItems = new[]
        {
            new OrderItem { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m, TotalPrice = 999.99m },
            new OrderItem { Id = 2, OrderId = 2, ProductId = 4, Quantity = 1, UnitPrice = 29.99m, TotalPrice = 29.99m },
            new OrderItem { Id = 3, OrderId = 3, ProductId = 2, Quantity = 1, UnitPrice = 899.99m, TotalPrice = 899.99m }
        };
        await _context.OrderItems.AddRangeAsync(orderItems);

        var reviews = new[]
        {
            new Review { Id = 1, ProductId = 1, CustomerId = 1, Rating = 5, Title = "Great!", Comment = "Excellent", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-20) }
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
    public async Task SearchByEmail_WithValidEmail_ShouldReturnCustomer()
    {
        // Act
        var result = await _controller.SearchByEmail("john.doe@example.com");

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customer = (CustomerDetailDto)okResult.Value!;

        await Assert.That(customer).IsNotNull();
        await Assert.That(customer.FirstName).IsEqualTo("John");
        await Assert.That(customer.Email).IsEqualTo("john.doe@example.com");
    }

    [Test]
    public async Task SearchByEmail_WithInvalidEmail_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.SearchByEmail("notexists@example.com");

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetCustomersByCityBad_ShouldReturnCustomers()
    {
        // Act
        var result = await _controller.GetCustomersByCityBad("New York");

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCustomersByCityGood_ShouldReturnCustomers()
    {
        // Act
        var result = await _controller.GetCustomersByCityGood("New York");

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCustomersByCityBad_CaseInsensitive_ShouldWork()
    {
        // Act
        var result = await _controller.GetCustomersByCityBad("new york");

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCustomersByLocation_WithCountryOnly_ShouldReturnCustomers()
    {
        // Act
        var result = await _controller.GetCustomersByLocation("USA");

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.All(c => c.Country == "USA")).IsTrue();
    }

    [Test]
    public async Task GetCustomersByLocation_WithCountryAndCity_ShouldReturnFilteredCustomers()
    {
        // Act
        var result = await _controller.GetCustomersByLocation("USA", "New York");

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
    }

    [Test]
    public async Task GetCustomersByLocation_WithIsActive_ShouldReturnFilteredCustomers()
    {
        // Act
        var result = await _controller.GetCustomersByLocation("USA", isActive: true);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        // Should exclude Alice (inactive)
        await Assert.That(customers.All(c => c.FullName != "Alice Brown")).IsTrue();
    }

    [Test]
    public async Task GetProductsByCategory_ShouldReturnProducts()
    {
        // Act
        var result = await _controller.GetProductsByCategory(categoryId: 1);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products).IsNotNull();
        await Assert.That(products.Count).IsEqualTo(3); // iPhone, Samsung, MacBook
    }

    [Test]
    public async Task GetProductsByCategory_WithPriceFilter_ShouldReturnFilteredProducts()
    {
        // Act
        var result = await _controller.GetProductsByCategory(categoryId: 1, minPrice: 900m);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.All(p => p.Price >= 900m)).IsTrue();
    }

    [Test]
    public async Task GetProductsByCategory_WithMaxPrice_ShouldReturnFilteredProducts()
    {
        // Act
        var result = await _controller.GetProductsByCategory(categoryId: 1, maxPrice: 1000m);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.All(p => p.Price <= 1000m)).IsTrue();
    }

    [Test]
    public async Task GetProductsByCategory_WithPagination_ShouldWork()
    {
        // Act
        var result = await _controller.GetProductsByCategory(categoryId: 1, skip: 1, take: 2);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetCustomerWithAllDataBad_WithValidId_ShouldReturnCustomer()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataBad(1);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customer = (CustomerFullDto)okResult.Value!;

        await Assert.That(customer).IsNotNull();
        await Assert.That(customer.FirstName).IsEqualTo("John");
    }

    [Test]
    public async Task GetCustomerWithAllDataBad_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataBad(999);

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetCustomerWithAllDataGood_WithValidId_ShouldReturnCustomer()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataGood(1);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customer = (CustomerFullDto)okResult.Value!;

        await Assert.That(customer).IsNotNull();
        await Assert.That(customer.FirstName).IsEqualTo("John");
    }

    [Test]
    public async Task GetCustomerWithAllDataGood_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataGood(999);

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetCustomerWithAllDataGood_ShouldIncludeOrders()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataGood(1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customer = (CustomerFullDto)okResult.Value!;

        await Assert.That(customer.Orders).IsNotNull();
        await Assert.That(customer.Orders.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetCustomerWithAllDataGood_ShouldIncludeAddresses()
    {
        // Act
        var result = await _controller.GetCustomerWithAllDataGood(1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customer = (CustomerFullDto)okResult.Value!;

        await Assert.That(customer.Addresses).IsNotNull();
        await Assert.That(customer.Addresses.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetOrdersByDateRange_ShouldReturnOrders()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-60);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetOrdersByDateRange(startDate, endDate);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderSummaryDto>)okResult.Value!;

        await Assert.That(orders).IsNotNull();
        await Assert.That(orders.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetOrdersByDateRange_WithNarrowRange_ShouldReturnFilteredOrders()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-10);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetOrdersByDateRange(startDate, endDate);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderSummaryDto>)okResult.Value!;

        await Assert.That(orders.All(o => o.OrderDate >= startDate && o.OrderDate <= endDate)).IsTrue();
    }

    [Test]
    public async Task GetOrdersByDateRange_WithPagination_ShouldWork()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-60);
        var endDate = DateTime.UtcNow;

        // Act
        var result = await _controller.GetOrdersByDateRange(startDate, endDate, skip: 1, take: 1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var orders = (List<OrderSummaryDto>)okResult.Value!;

        await Assert.That(orders.Count).IsLessThanOrEqualTo(1);
    }
}
