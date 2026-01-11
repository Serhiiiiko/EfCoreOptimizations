using EfCoreOptimizations.Controllers;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using EfCoreOptimizations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Controllers;

public class TrackingControllerTests
{
    private AppDbContext _context = null!;
    private TrackingController _controller = null!;
    private Mock<ILogger<TrackingController>> _loggerMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<TrackingController>>();
        _controller = new TrackingController(_context, _loggerMock.Object);

        await SeedTestData();
    }

    private async Task SeedTestData()
    {
        var categories = new[]
        {
            new Category { Id = 1, Name = "Electronics", Slug = "electronics", IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await _context.Categories.AddRangeAsync(categories);

        var products = new List<Product>();
        for (int i = 1; i <= 20; i++)
        {
            products.Add(new Product
            {
                Id = i,
                Name = $"Product {i}",
                SKU = $"SKU-{i:D5}",
                Price = 10m * i,
                StockQuantity = 100,
                CategoryId = 1,
                IsActive = true,
                AverageRating = 4.0m,
                ReviewCount = 5,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.Products.AddRangeAsync(products);

        var customers = new List<Customer>();
        for (int i = 1; i <= 10; i++)
        {
            customers.Add(new Customer
            {
                Id = i,
                FirstName = $"Customer{i}",
                LastName = $"Last{i}",
                Email = $"customer{i}@test.com",
                City = "City",
                Country = "Country",
                IsActive = true,
                TotalOrders = i,
                CreatedAt = DateTime.UtcNow
            });
        }
        await _context.Customers.AddRangeAsync(customers);

        var orders = new[]
        {
            new Order { Id = 1, OrderNumber = "ORD-001", CustomerId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 100m, CreatedAt = DateTime.UtcNow },
            new Order { Id = 2, OrderNumber = "ORD-002", CustomerId = 1, OrderDate = DateTime.UtcNow, TotalAmount = 200m, CreatedAt = DateTime.UtcNow }
        };
        await _context.Orders.AddRangeAsync(orders);

        await _context.SaveChangesAsync();
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task GetProductsWithTracking_ShouldReturnActiveProducts()
    {
        // Act
        var result = await _controller.GetProductsWithTracking(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products).IsNotNull();
        await Assert.That(products.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GetProductsNoTracking_ShouldReturnActiveProducts()
    {
        // Act
        var result = await _controller.GetProductsNoTracking(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products).IsNotNull();
        await Assert.That(products.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GetProductsNoTracking_ShouldIncludeCategoryName()
    {
        // Act
        var result = await _controller.GetProductsNoTracking(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.All(p => p.CategoryName == "Electronics")).IsTrue();
    }

    [Test]
    public async Task GetCustomersWithTracking_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersWithTracking(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GetCustomersNoTracking_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersNoTracking(take: 10);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GetCustomersNoTracking_ShouldHaveCorrectTotalOrders()
    {
        // Act
        var result = await _controller.GetCustomersNoTracking(take: 10);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        // TotalOrders is calculated from Orders.Count in the controller
        await Assert.That(customers.Any(c => c.TotalOrders > 0)).IsTrue();
    }

    [Test]
    public async Task UpdateProductPrice_WithValidId_ShouldUpdatePrice()
    {
        // Arrange
        var newPrice = 1500.00m;

        // Act
        var result = await _controller.UpdateProductPrice(1, newPrice);

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();

        // Verify the price was updated
        var updatedProduct = await _context.Products.FindAsync(1);
        await Assert.That(updatedProduct).IsNotNull();
        await Assert.That(updatedProduct!.Price).IsEqualTo(newPrice);
    }

    [Test]
    public async Task UpdateProductPrice_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.UpdateProductPrice(999, 100m);

        // Assert
        await Assert.That(result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task UpdateProductPrice_ShouldUpdateTimestamp()
    {
        // Arrange
        var newPrice = 2000.00m;
        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _controller.UpdateProductPrice(1, newPrice);

        // Assert
        var updatedProduct = await _context.Products.FindAsync(1);
        await Assert.That(updatedProduct!.UpdatedAt).IsNotNull();
        await Assert.That(updatedProduct.UpdatedAt!.Value).IsGreaterThanOrEqualTo(beforeUpdate);
    }

    [Test]
    public async Task UpdateProductPriceBulk_WithValidId_ShouldUpdatePrice()
    {
        // Arrange
        var newPrice = 2500.00m;

        // Act
        var result = await _controller.UpdateProductPriceBulk(2, newPrice);

        // Assert
        await Assert.That(result).IsTypeOf<OkObjectResult>();

        // Verify the price was updated
        var updatedProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 2);
        await Assert.That(updatedProduct).IsNotNull();
        await Assert.That(updatedProduct!.Price).IsEqualTo(newPrice);
    }

    [Test]
    public async Task UpdateProductPriceBulk_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.UpdateProductPriceBulk(999, 100m);

        // Assert
        await Assert.That(result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetProducts_BothMethods_ShouldReturnSameCount()
    {
        // Act
        var trackingResult = await _controller.GetProductsWithTracking(take: 15);
        var noTrackingResult = await _controller.GetProductsNoTracking(take: 15);

        // Assert
        var trackingProducts = (List<ProductListDto>)((OkObjectResult)trackingResult.Result!).Value!;
        var noTrackingProducts = (List<ProductListDto>)((OkObjectResult)noTrackingResult.Result!).Value!;

        await Assert.That(trackingProducts.Count).IsEqualTo(noTrackingProducts.Count);
    }

    [Test]
    public async Task UpdatePrice_BothMethods_ShouldProduceSameResult()
    {
        // Arrange
        var newPrice1 = 1111.11m;
        var newPrice2 = 2222.22m;

        // Act
        await _controller.UpdateProductPrice(1, newPrice1);
        await _controller.UpdateProductPriceBulk(2, newPrice2);

        // Assert
        var product1 = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == 1);
        var product2 = await _context.Products.AsNoTracking().FirstAsync(p => p.Id == 2);

        await Assert.That(product1.Price).IsEqualTo(newPrice1);
        await Assert.That(product2.Price).IsEqualTo(newPrice2);
    }
}
