using EfCoreOptimizations.Data;
using EfCoreOptimizations.Models;
using Microsoft.EntityFrameworkCore;

namespace EfCoreOptimizations.Tests.Fixtures;

public class TestDbContextFixture : IAsyncDisposable
{
    public AppDbContext Context { get; }

    public TestDbContextFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new AppDbContext(options, useIndexes: false);
    }

    public async Task SeedTestDataAsync()
    {
        // Categories
        var categories = new List<Category>
        {
            new() { Id = 1, Name = "Electronics", Slug = "electronics", Description = "Electronic devices", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Clothing", Slug = "clothing", Description = "Apparel", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, Name = "Books", Slug = "books", Description = "Books and magazines", IsActive = true, CreatedAt = DateTime.UtcNow },
            new() { Id = 4, Name = "Phones", Slug = "phones", Description = "Mobile phones", ParentCategoryId = 1, IsActive = true, CreatedAt = DateTime.UtcNow }
        };
        await Context.Categories.AddRangeAsync(categories);

        // Products
        var products = new List<Product>
        {
            new() { Id = 1, Name = "iPhone 15", SKU = "SKU-IPHONE15", Description = "Apple smartphone", Price = 999.99m, Cost = 700m, StockQuantity = 100, CategoryId = 4, IsActive = true, Manufacturer = "Apple", CreatedAt = DateTime.UtcNow, AverageRating = 4.5m, ReviewCount = 10 },
            new() { Id = 2, Name = "Samsung Galaxy S24", SKU = "SKU-GALAXY24", Description = "Samsung smartphone", Price = 899.99m, Cost = 600m, StockQuantity = 50, CategoryId = 4, IsActive = true, Manufacturer = "Samsung", CreatedAt = DateTime.UtcNow, AverageRating = 4.3m, ReviewCount = 8 },
            new() { Id = 3, Name = "T-Shirt", SKU = "SKU-TSHIRT01", Description = "Cotton t-shirt", Price = 29.99m, Cost = 10m, StockQuantity = 500, CategoryId = 2, IsActive = true, Manufacturer = "Generic", CreatedAt = DateTime.UtcNow, AverageRating = 4.0m, ReviewCount = 5 },
            new() { Id = 4, Name = "Programming Book", SKU = "SKU-BOOK001", Description = "Learn C#", Price = 49.99m, Cost = 20m, StockQuantity = 200, CategoryId = 3, IsActive = true, Manufacturer = "Publisher", CreatedAt = DateTime.UtcNow, AverageRating = 4.8m, ReviewCount = 15 },
            new() { Id = 5, Name = "Inactive Product", SKU = "SKU-INACTIVE", Description = "Not for sale", Price = 19.99m, Cost = 5m, StockQuantity = 0, CategoryId = 1, IsActive = false, Manufacturer = "Unknown", CreatedAt = DateTime.UtcNow, AverageRating = 0m, ReviewCount = 0 }
        };
        await Context.Products.AddRangeAsync(products);

        // Customers
        var customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john.doe@example.com", Phone = "123-456-7890", City = "New York", Country = "USA", DateOfBirth = new DateTime(1990, 1, 15), CreatedAt = DateTime.UtcNow.AddMonths(-6), IsActive = true, CreditLimit = 5000m, TotalOrders = 3 },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane.smith@example.com", Phone = "098-765-4321", City = "London", Country = "UK", DateOfBirth = new DateTime(1985, 5, 20), CreatedAt = DateTime.UtcNow.AddMonths(-3), IsActive = true, CreditLimit = 10000m, TotalOrders = 5 },
            new() { Id = 3, FirstName = "Bob", LastName = "Johnson", Email = "bob.johnson@example.com", Phone = "555-555-5555", City = "Toronto", Country = "Canada", DateOfBirth = new DateTime(1978, 12, 1), CreatedAt = DateTime.UtcNow.AddMonths(-12), IsActive = true, CreditLimit = 15000m, TotalOrders = 10 },
            new() { Id = 4, FirstName = "Alice", LastName = "Brown", Email = "alice.brown@example.com", Phone = "111-222-3333", City = "Sydney", Country = "Australia", DateOfBirth = new DateTime(1995, 7, 10), CreatedAt = DateTime.UtcNow.AddMonths(-1), IsActive = false, CreditLimit = 2000m, TotalOrders = 1 }
        };
        await Context.Customers.AddRangeAsync(customers);

        // Addresses
        var addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Street = "123 Main St", City = "New York", State = "NY", Country = "USA", PostalCode = "10001", IsDefault = true, Type = AddressType.Both, CreatedAt = DateTime.UtcNow },
            new() { Id = 2, CustomerId = 1, Street = "456 Work Ave", City = "New York", State = "NY", Country = "USA", PostalCode = "10002", IsDefault = false, Type = AddressType.Billing, CreatedAt = DateTime.UtcNow },
            new() { Id = 3, CustomerId = 2, Street = "789 London Rd", City = "London", State = "Greater London", Country = "UK", PostalCode = "SW1A 1AA", IsDefault = true, Type = AddressType.Shipping, CreatedAt = DateTime.UtcNow }
        };
        await Context.Addresses.AddRangeAsync(addresses);

        // Orders
        var orders = new List<Order>
        {
            new() { Id = 1, OrderNumber = "ORD-001", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-30), Status = OrderStatus.Delivered, TotalAmount = 1029.98m, ShippingCost = 10m, Tax = 102.99m, ShippingAddress = "123 Main St, NY", BillingAddress = "123 Main St, NY", CreatedAt = DateTime.UtcNow.AddDays(-30), ShippedDate = DateTime.UtcNow.AddDays(-25) },
            new() { Id = 2, OrderNumber = "ORD-002", CustomerId = 1, OrderDate = DateTime.UtcNow.AddDays(-15), Status = OrderStatus.Shipped, TotalAmount = 29.99m, ShippingCost = 5m, Tax = 3m, ShippingAddress = "123 Main St, NY", BillingAddress = "456 Work Ave, NY", CreatedAt = DateTime.UtcNow.AddDays(-15), ShippedDate = DateTime.UtcNow.AddDays(-12) },
            new() { Id = 3, OrderNumber = "ORD-003", CustomerId = 2, OrderDate = DateTime.UtcNow.AddDays(-7), Status = OrderStatus.Processing, TotalAmount = 949.98m, ShippingCost = 15m, Tax = 94.99m, ShippingAddress = "789 London Rd", BillingAddress = "789 London Rd", CreatedAt = DateTime.UtcNow.AddDays(-7) },
            new() { Id = 4, OrderNumber = "ORD-004", CustomerId = 3, OrderDate = DateTime.UtcNow.AddDays(-2), Status = OrderStatus.Pending, TotalAmount = 49.99m, ShippingCost = 8m, Tax = 5m, ShippingAddress = "Toronto Address", BillingAddress = "Toronto Address", CreatedAt = DateTime.UtcNow.AddDays(-2) }
        };
        await Context.Orders.AddRangeAsync(orders);

        // Order Items
        var orderItems = new List<OrderItem>
        {
            new() { Id = 1, OrderId = 1, ProductId = 1, Quantity = 1, UnitPrice = 999.99m, Discount = 0, TotalPrice = 999.99m },
            new() { Id = 2, OrderId = 1, ProductId = 3, Quantity = 1, UnitPrice = 29.99m, Discount = 0, TotalPrice = 29.99m },
            new() { Id = 3, OrderId = 2, ProductId = 3, Quantity = 1, UnitPrice = 29.99m, Discount = 0, TotalPrice = 29.99m },
            new() { Id = 4, OrderId = 3, ProductId = 2, Quantity = 1, UnitPrice = 899.99m, Discount = 0, TotalPrice = 899.99m },
            new() { Id = 5, OrderId = 3, ProductId = 4, Quantity = 1, UnitPrice = 49.99m, Discount = 0, TotalPrice = 49.99m },
            new() { Id = 6, OrderId = 4, ProductId = 4, Quantity = 1, UnitPrice = 49.99m, Discount = 0, TotalPrice = 49.99m }
        };
        await Context.OrderItems.AddRangeAsync(orderItems);

        // Reviews
        var reviews = new List<Review>
        {
            new() { Id = 1, ProductId = 1, CustomerId = 1, Rating = 5, Title = "Great phone!", Comment = "Best phone I ever had", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-20), HelpfulCount = 10, UnhelpfulCount = 1 },
            new() { Id = 2, ProductId = 1, CustomerId = 2, Rating = 4, Title = "Good but expensive", Comment = "Quality is great but price is high", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-15), HelpfulCount = 5, UnhelpfulCount = 0 },
            new() { Id = 3, ProductId = 2, CustomerId = 3, Rating = 5, Title = "Amazing!", Comment = "Love this phone", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-5), HelpfulCount = 3, UnhelpfulCount = 0 },
            new() { Id = 4, ProductId = 4, CustomerId = 1, Rating = 5, Title = "Great book", Comment = "Learned a lot", IsVerifiedPurchase = true, CreatedAt = DateTime.UtcNow.AddDays(-10), HelpfulCount = 20, UnhelpfulCount = 2 }
        };
        await Context.Reviews.AddRangeAsync(reviews);

        await Context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
    }
}
