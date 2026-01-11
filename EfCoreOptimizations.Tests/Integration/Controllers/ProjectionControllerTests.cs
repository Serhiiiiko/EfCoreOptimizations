using EfCoreOptimizations.Controllers;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using EfCoreOptimizations.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Controllers;

public class ProjectionControllerTests
{
    private AppDbContext _context = null!;
    private ProjectionController _controller = null!;
    private Mock<ILogger<ProjectionController>> _loggerMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<ProjectionController>>();
        _controller = new ProjectionController(_context, _loggerMock.Object);

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
            new Product { Id = 1, Name = "iPhone 15", SKU = "SKU-IPHONE15", Description = "Apple smartphone", Price = 999.99m, StockQuantity = 100, CategoryId = 1, IsActive = true, Manufacturer = "Apple", AverageRating = 4.5m, ReviewCount = 10, CreatedAt = DateTime.UtcNow },
            new Product { Id = 2, Name = "Samsung Galaxy", SKU = "SKU-GALAXY", Description = "Samsung smartphone", Price = 899.99m, StockQuantity = 50, CategoryId = 1, IsActive = true, Manufacturer = "Samsung", AverageRating = 4.3m, ReviewCount = 8, CreatedAt = DateTime.UtcNow },
            new Product { Id = 3, Name = "T-Shirt", SKU = "SKU-TSHIRT", Description = "Cotton t-shirt", Price = 29.99m, StockQuantity = 500, CategoryId = 2, IsActive = true, Manufacturer = "Generic", AverageRating = 4.0m, ReviewCount = 5, CreatedAt = DateTime.UtcNow },
            new Product { Id = 4, Name = "Inactive Product", SKU = "SKU-INACTIVE", Description = "Not for sale", Price = 19.99m, StockQuantity = 0, CategoryId = 1, IsActive = false, CreatedAt = DateTime.UtcNow }
        };
        await _context.Products.AddRangeAsync(products);

        var customers = new[]
        {
            new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@test.com", City = "New York", Country = "USA", IsActive = true, TotalOrders = 5, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@test.com", City = "London", Country = "UK", IsActive = true, TotalOrders = 3, CreatedAt = DateTime.UtcNow },
            new Customer { Id = 3, FirstName = "Inactive", LastName = "User", Email = "inactive@test.com", City = "Paris", Country = "France", IsActive = false, TotalOrders = 0, CreatedAt = DateTime.UtcNow }
        };
        await _context.Customers.AddRangeAsync(customers);

        var reviews = new[]
        {
            new Review { Id = 1, ProductId = 1, CustomerId = 1, Rating = 5, Title = "Great!", Comment = "Excellent product", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-10) },
            new Review { Id = 2, ProductId = 1, CustomerId = 2, Rating = 4, Title = "Good", Comment = "Nice phone", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
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
    public async Task GetProductListBad_ShouldReturnActiveProducts()
    {
        // Act
        var result = await _controller.GetProductListBad(skip: 0, take: 50);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products).IsNotNull();
        await Assert.That(products.Count).IsEqualTo(3); // Only active products
        await Assert.That(products.All(p => p.Name != "Inactive Product")).IsTrue();
    }

    [Test]
    public async Task GetProductListGood_ShouldReturnActiveProducts()
    {
        // Act
        var result = await _controller.GetProductListGood(skip: 0, take: 50);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products).IsNotNull();
        await Assert.That(products.Count).IsEqualTo(3); // Only active products
    }

    [Test]
    public async Task GetProductListGood_ShouldIncludeCategoryName()
    {
        // Act
        var result = await _controller.GetProductListGood(skip: 0, take: 50);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.All(p => !string.IsNullOrEmpty(p.CategoryName))).IsTrue();
        await Assert.That(products.Any(p => p.CategoryName == "Electronics")).IsTrue();
    }

    [Test]
    public async Task GetProductListGood_ShouldSetIsInStockCorrectly()
    {
        // Act
        var result = await _controller.GetProductListGood(skip: 0, take: 50);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        var iphone = products.FirstOrDefault(p => p.Name == "iPhone 15");
        await Assert.That(iphone).IsNotNull();
        await Assert.That(iphone!.IsInStock).IsTrue();
    }

    [Test]
    public async Task GetProductListGood_WithPagination_ShouldWork()
    {
        // Act
        var result = await _controller.GetProductListGood(skip: 1, take: 2);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var products = (List<ProductListDto>)okResult.Value!;

        await Assert.That(products.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetCustomersBad_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersBad(take: 100);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<Customer>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(2); // Only active customers
    }

    [Test]
    public async Task GetCustomersGood_ShouldReturnActiveCustomers()
    {
        // Act
        var result = await _controller.GetCustomersGood(take: 100);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        await Assert.That(customers).IsNotNull();
        await Assert.That(customers.Count).IsEqualTo(2); // Only active customers
    }

    [Test]
    public async Task GetCustomersGood_ShouldHaveFullName()
    {
        // Act
        var result = await _controller.GetCustomersGood(take: 100);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var customers = (List<CustomerListDto>)okResult.Value!;

        var john = customers.FirstOrDefault(c => c.FullName.Contains("John"));
        await Assert.That(john).IsNotNull();
        await Assert.That(john!.FullName).IsEqualTo("John Doe");
    }

    [Test]
    public async Task GetProductWithReviewsBad_WithValidId_ShouldReturnProduct()
    {
        // Act
        var result = await _controller.GetProductWithReviewsBad(1);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var product = (ProductDetailDto)okResult.Value!;

        await Assert.That(product).IsNotNull();
        await Assert.That(product.Name).IsEqualTo("iPhone 15");
    }

    [Test]
    public async Task GetProductWithReviewsBad_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetProductWithReviewsBad(999);

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetProductWithReviewsGood_WithValidId_ShouldReturnProduct()
    {
        // Act
        var result = await _controller.GetProductWithReviewsGood(1);

        // Assert
        await Assert.That(result.Result).IsTypeOf<OkObjectResult>();
        var okResult = (OkObjectResult)result.Result!;
        var product = (ProductDetailDto)okResult.Value!;

        await Assert.That(product).IsNotNull();
        await Assert.That(product.Name).IsEqualTo("iPhone 15");
    }

    [Test]
    public async Task GetProductWithReviewsGood_WithInvalidId_ShouldReturnNotFound()
    {
        // Act
        var result = await _controller.GetProductWithReviewsGood(999);

        // Assert
        await Assert.That(result.Result).IsTypeOf<NotFoundResult>();
    }

    [Test]
    public async Task GetProductWithReviewsGood_ShouldIncludeReviews()
    {
        // Act
        var result = await _controller.GetProductWithReviewsGood(1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var product = (ProductDetailDto)okResult.Value!;

        await Assert.That(product.RecentReviews).IsNotNull();
        await Assert.That(product.RecentReviews.Count).IsGreaterThan(0);
    }

    [Test]
    public async Task GetProductWithReviewsGood_ReviewsShouldHaveCustomerName()
    {
        // Act
        var result = await _controller.GetProductWithReviewsGood(1);

        // Assert
        var okResult = (OkObjectResult)result.Result!;
        var product = (ProductDetailDto)okResult.Value!;

        await Assert.That(product.RecentReviews.All(r => !string.IsNullOrEmpty(r.CustomerName))).IsTrue();
    }

    [Test]
    public async Task GetProductList_BothMethods_ShouldReturnSameCount()
    {
        // Act
        var badResult = await _controller.GetProductListBad(skip: 0, take: 50);
        var goodResult = await _controller.GetProductListGood(skip: 0, take: 50);

        // Assert
        var badProducts = (List<ProductListDto>)((OkObjectResult)badResult.Result!).Value!;
        var goodProducts = (List<ProductListDto>)((OkObjectResult)goodResult.Result!).Value!;

        await Assert.That(badProducts.Count).IsEqualTo(goodProducts.Count);
    }
}
