using EfCoreOptimizations.Data;
using EfCoreOptimizations.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace EfCoreOptimizations.Tests.Integration.Data;

public class DataSeederTests
{
    private AppDbContext _context = null!;
    private DataSeeder _seeder = null!;
    private Mock<ILogger<DataSeeder>> _loggerMock = null!;

    [Before(Test)]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _loggerMock = new Mock<ILogger<DataSeeder>>();
        _seeder = new DataSeeder(_context, _loggerMock.Object);
    }

    [After(Test)]
    public async Task TearDown()
    {
        await _context.DisposeAsync();
    }

    [Test]
    public async Task SeedAsync_ShouldCreateCategories()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var categoriesCount = await _context.Categories.CountAsync();
        await Assert.That(categoriesCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateMainAndSubCategories()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var mainCategories = await _context.Categories.CountAsync(c => c.ParentCategoryId == null);
        var subCategories = await _context.Categories.CountAsync(c => c.ParentCategoryId != null);

        await Assert.That(mainCategories).IsGreaterThan(0);
        await Assert.That(subCategories).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateProducts()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var productsCount = await _context.Products.CountAsync();
        await Assert.That(productsCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_ProductsShouldHaveValidCategories()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var productsWithInvalidCategory = await _context.Products
            .Where(p => !_context.Categories.Any(c => c.Id == p.CategoryId))
            .CountAsync();

        await Assert.That(productsWithInvalidCategory).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateCustomers()
    {
        // Arrange
        var expectedCount = 10;

        // Act
        await _seeder.SeedAsync(customerCount: expectedCount, productsPerCategory: 5);

        // Assert
        var customersCount = await _context.Customers.CountAsync();
        await Assert.That(customersCount).IsEqualTo(expectedCount);
    }

    [Test]
    public async Task SeedAsync_CustomersShouldHaveUniqueEmails()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 50, productsPerCategory: 5);

        // Assert
        var totalCustomers = await _context.Customers.CountAsync();
        var uniqueEmails = await _context.Customers.Select(c => c.Email).Distinct().CountAsync();

        await Assert.That(uniqueEmails).IsEqualTo(totalCustomers);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateOrders()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var ordersCount = await _context.Orders.CountAsync();
        await Assert.That(ordersCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_OrdersShouldHaveValidCustomers()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var ordersWithInvalidCustomer = await _context.Orders
            .Where(o => !_context.Customers.Any(c => c.Id == o.CustomerId))
            .CountAsync();

        await Assert.That(ordersWithInvalidCustomer).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateOrderItems()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var orderItemsCount = await _context.OrderItems.CountAsync();
        await Assert.That(orderItemsCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_OrderItemsShouldHaveValidOrders()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var itemsWithInvalidOrder = await _context.OrderItems
            .Where(oi => !_context.Orders.Any(o => o.Id == oi.OrderId))
            .CountAsync();

        await Assert.That(itemsWithInvalidOrder).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_OrderItemsShouldHaveValidProducts()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var itemsWithInvalidProduct = await _context.OrderItems
            .Where(oi => !_context.Products.Any(p => p.Id == oi.ProductId))
            .CountAsync();

        await Assert.That(itemsWithInvalidProduct).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateReviews()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var reviewsCount = await _context.Reviews.CountAsync();
        await Assert.That(reviewsCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_ReviewsShouldHaveValidProducts()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var reviewsWithInvalidProduct = await _context.Reviews
            .Where(r => !_context.Products.Any(p => p.Id == r.ProductId))
            .CountAsync();

        await Assert.That(reviewsWithInvalidProduct).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ReviewsShouldHaveValidCustomers()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var reviewsWithInvalidCustomer = await _context.Reviews
            .Where(r => !_context.Customers.Any(c => c.Id == r.CustomerId))
            .CountAsync();

        await Assert.That(reviewsWithInvalidCustomer).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ReviewsShouldHaveValidRatings()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var invalidRatings = await _context.Reviews
            .Where(r => r.Rating < 1 || r.Rating > 5)
            .CountAsync();

        await Assert.That(invalidRatings).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ShouldCreateAddresses()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 50, productsPerCategory: 5);

        // Assert
        var addressesCount = await _context.Addresses.CountAsync();
        await Assert.That(addressesCount).IsGreaterThan(0);
    }

    [Test]
    public async Task SeedAsync_AddressesShouldHaveValidCustomers()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 50, productsPerCategory: 5);

        // Assert
        var addressesWithInvalidCustomer = await _context.Addresses
            .Where(a => !_context.Customers.Any(c => c.Id == a.CustomerId))
            .CountAsync();

        await Assert.That(addressesWithInvalidCustomer).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_WhenDataAlreadyExists_ShouldSkipSeeding()
    {
        // Arrange
        await _context.Customers.AddAsync(new Customer
        {
            FirstName = "Existing",
            LastName = "Customer",
            Email = "existing@test.com"
        });
        await _context.SaveChangesAsync();

        var initialCount = await _context.Customers.CountAsync();

        // Act
        await _seeder.SeedAsync(customerCount: 100, productsPerCategory: 5);

        // Assert
        var afterSeedCount = await _context.Customers.CountAsync();
        await Assert.That(afterSeedCount).IsEqualTo(initialCount);
    }

    [Test]
    public async Task SeedAsync_ProductsShouldHaveValidPrices()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var invalidPrices = await _context.Products
            .Where(p => p.Price <= 0)
            .CountAsync();

        await Assert.That(invalidPrices).IsEqualTo(0);
    }

    [Test]
    public async Task SeedAsync_ProductsShouldHaveUniqueSKUs()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 10);

        // Assert
        var totalProducts = await _context.Products.CountAsync();
        var uniqueSKUs = await _context.Products.Select(p => p.SKU).Distinct().CountAsync();

        await Assert.That(uniqueSKUs).IsEqualTo(totalProducts);
    }

    [Test]
    public async Task SeedAsync_OrdersShouldHaveUniqueOrderNumbers()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 20, productsPerCategory: 5);

        // Assert
        var totalOrders = await _context.Orders.CountAsync();
        var uniqueOrderNumbers = await _context.Orders.Select(o => o.OrderNumber).Distinct().CountAsync();

        await Assert.That(uniqueOrderNumbers).IsEqualTo(totalOrders);
    }

    [Test]
    public async Task SeedAsync_CategoriesShouldHaveUniqueSlugs()
    {
        // Act
        await _seeder.SeedAsync(customerCount: 10, productsPerCategory: 5);

        // Assert
        var totalCategories = await _context.Categories.CountAsync();
        var uniqueSlugs = await _context.Categories.Select(c => c.Slug).Distinct().CountAsync();

        await Assert.That(uniqueSlugs).IsEqualTo(totalCategories);
    }
}
