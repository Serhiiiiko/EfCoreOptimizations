using EfCoreOptimizations.Data;
using EfCoreOptimizations.Models;
using EfCoreOptimizations.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace EfCoreOptimizations.Tests.Integration.Data;

public class AppDbContextTests
{
    [Test]
    public async Task AppDbContext_WithInMemoryDatabase_ShouldCreateSuccessfully()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Act
        await using var context = new AppDbContext(options);

        // Assert
        await Assert.That(context).IsNotNull();
        await Assert.That(context.Customers).IsNotNull();
        await Assert.That(context.Products).IsNotNull();
        await Assert.That(context.Orders).IsNotNull();
    }

    [Test]
    public async Task AppDbContext_DbSets_ShouldBeAccessible()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        // Assert
        await Assert.That(context.Customers).IsNotNull();
        await Assert.That(context.Orders).IsNotNull();
        await Assert.That(context.OrderItems).IsNotNull();
        await Assert.That(context.Products).IsNotNull();
        await Assert.That(context.Categories).IsNotNull();
        await Assert.That(context.Reviews).IsNotNull();
        await Assert.That(context.Addresses).IsNotNull();
    }

    [Test]
    public async Task AppDbContext_AddCustomer_ShouldPersist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var customer = new Customer
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            IsActive = true
        };

        // Act
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        // Assert
        var savedCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Email == "test@example.com");
        await Assert.That(savedCustomer).IsNotNull();
        await Assert.That(savedCustomer!.FirstName).IsEqualTo("Test");
    }

    [Test]
    public async Task AppDbContext_AddProduct_ShouldPersist()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var category = new Category { Name = "Electronics", Slug = "electronics", IsActive = true };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Test Product",
            SKU = "SKU-TEST001",
            Price = 99.99m,
            CategoryId = category.Id,
            IsActive = true
        };

        // Act
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Assert
        var savedProduct = await context.Products.FirstOrDefaultAsync(p => p.SKU == "SKU-TEST001");
        await Assert.That(savedProduct).IsNotNull();
        await Assert.That(savedProduct!.Name).IsEqualTo("Test Product");
        await Assert.That(savedProduct.CategoryId).IsEqualTo(category.Id);
    }

    [Test]
    public async Task AppDbContext_CustomerOrderRelationship_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var customer = new Customer
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            IsActive = true
        };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var order = new Order
        {
            OrderNumber = "ORD-001",
            CustomerId = customer.Id,
            Status = OrderStatus.Pending,
            TotalAmount = 100m
        };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        // Act
        var customerWithOrders = await context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == customer.Id);

        // Assert
        await Assert.That(customerWithOrders).IsNotNull();
        await Assert.That(customerWithOrders!.Orders.Count).IsEqualTo(1);
        await Assert.That(customerWithOrders.Orders.First().OrderNumber).IsEqualTo("ORD-001");
    }

    [Test]
    public async Task AppDbContext_ProductCategoryRelationship_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var category = new Category { Name = "Electronics", Slug = "electronics", IsActive = true };
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "iPhone",
            SKU = "SKU-IPHONE",
            Price = 999.99m,
            CategoryId = category.Id,
            IsActive = true
        };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        // Act
        var productWithCategory = await context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        // Assert
        await Assert.That(productWithCategory).IsNotNull();
        await Assert.That(productWithCategory!.Category).IsNotNull();
        await Assert.That(productWithCategory.Category.Name).IsEqualTo("Electronics");
    }

    [Test]
    public async Task AppDbContext_OrderWithItems_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var customer = new Customer { FirstName = "Test", LastName = "User", Email = "test@test.com" };
        var category = new Category { Name = "Test", Slug = "test" };
        await context.Customers.AddAsync(customer);
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Product", SKU = "SKU-001", Price = 50m, CategoryId = category.Id };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        var order = new Order { OrderNumber = "ORD-001", CustomerId = customer.Id, TotalAmount = 100m };
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        var orderItem = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 2,
            UnitPrice = 50m,
            TotalPrice = 100m
        };
        await context.OrderItems.AddAsync(orderItem);
        await context.SaveChangesAsync();

        // Act
        var orderWithItems = await context.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        // Assert
        await Assert.That(orderWithItems).IsNotNull();
        await Assert.That(orderWithItems!.OrderItems.Count).IsEqualTo(1);
        await Assert.That(orderWithItems.OrderItems.First().Product.Name).IsEqualTo("Product");
    }

    [Test]
    public async Task AppDbContext_CategoryHierarchy_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var parentCategory = new Category { Name = "Electronics", Slug = "electronics" };
        await context.Categories.AddAsync(parentCategory);
        await context.SaveChangesAsync();

        var childCategory = new Category
        {
            Name = "Phones",
            Slug = "phones",
            ParentCategoryId = parentCategory.Id
        };
        await context.Categories.AddAsync(childCategory);
        await context.SaveChangesAsync();

        // Act
        var parentWithChildren = await context.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == parentCategory.Id);

        // Assert
        await Assert.That(parentWithChildren).IsNotNull();
        await Assert.That(parentWithChildren!.SubCategories.Count).IsEqualTo(1);
        await Assert.That(parentWithChildren.SubCategories.First().Name).IsEqualTo("Phones");
    }

    [Test]
    public async Task AppDbContext_ReviewRelationships_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var customer = new Customer { FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        var category = new Category { Name = "Test", Slug = "test" };
        await context.Customers.AddAsync(customer);
        await context.Categories.AddAsync(category);
        await context.SaveChangesAsync();

        var product = new Product { Name = "Test Product", SKU = "SKU-001", CategoryId = category.Id };
        await context.Products.AddAsync(product);
        await context.SaveChangesAsync();

        var review = new Review
        {
            ProductId = product.Id,
            CustomerId = customer.Id,
            Rating = 5,
            Title = "Great!",
            Comment = "Excellent product"
        };
        await context.Reviews.AddAsync(review);
        await context.SaveChangesAsync();

        // Act
        var productWithReviews = await context.Products
            .Include(p => p.Reviews)
            .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        // Assert
        await Assert.That(productWithReviews).IsNotNull();
        await Assert.That(productWithReviews!.Reviews.Count).IsEqualTo(1);
        await Assert.That(productWithReviews.Reviews.First().Customer.FirstName).IsEqualTo("John");
    }

    [Test]
    public async Task AppDbContext_AddressRelationship_ShouldWork()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var customer = new Customer { FirstName = "John", LastName = "Doe", Email = "john@test.com" };
        await context.Customers.AddAsync(customer);
        await context.SaveChangesAsync();

        var address = new Address
        {
            CustomerId = customer.Id,
            Street = "123 Main St",
            City = "New York",
            State = "NY",
            Country = "USA",
            PostalCode = "10001",
            IsDefault = true,
            Type = AddressType.Both
        };
        await context.Addresses.AddAsync(address);
        await context.SaveChangesAsync();

        // Act
        var customerWithAddresses = await context.Customers
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == customer.Id);

        // Assert
        await Assert.That(customerWithAddresses).IsNotNull();
        await Assert.That(customerWithAddresses!.Addresses.Count).IsEqualTo(1);
        await Assert.That(customerWithAddresses.Addresses.First().City).IsEqualTo("New York");
    }
}
