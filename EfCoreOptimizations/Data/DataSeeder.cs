using Bogus;
using EfCoreOptimizations.Models;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace EfCoreOptimizations.Data;

/// <summary>
/// Fast bulk data seeder using EFCore.BulkExtensions and Bogus
/// </summary>
public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(int customerCount = 50000, int productsPerCategory = 10000)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting data seeding at {Time}", startTime);

        // Check if data already exists
        if (await _context.Customers.AnyAsync())
        {
            _logger.LogInformation("Database already contains data. Skipping seed.");
            return;
        }

        try
        {
            // Seed in specific order due to foreign key constraints
            await SeedCategories();
            await SeedProducts(productsPerCategory);
            await SeedCustomers(customerCount);
            await SeedAddresses();
            await SeedOrders();
            await SeedOrderItems();
            await SeedReviews();

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation("Data seeding completed in {Duration}. Total time: {TotalSeconds}s", 
                duration, duration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data seeding");
            throw;
        }
    }

    private async Task SeedCategories()
    {
        _logger.LogInformation("Seeding categories...");
        
        var categoryFaker = new Faker<Category>()
            .RuleFor(c => c.Name, f => f.Commerce.Department())
            .RuleFor(c => c.Slug, (f, c) => c.Name.ToLower().Replace(" ", "-").Replace("&", "and"))
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.DisplayOrder, f => f.IndexFaker)
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.9f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2))
            .RuleFor(c => c.ParentCategoryId, f => null);

        // Create main categories
        var mainCategories = categoryFaker.Generate(50);
        await _context.BulkInsertAsync(mainCategories);
        _logger.LogInformation("Inserted {Count} main categories", mainCategories.Count);

        // Create subcategories
        var subCategoryFaker = new Faker<Category>()
            .RuleFor(c => c.Name, f => f.Commerce.ProductName())
            .RuleFor(c => c.Slug, (f, c) => c.Name.ToLower().Replace(" ", "-"))
            .RuleFor(c => c.Description, f => f.Lorem.Sentence())
            .RuleFor(c => c.DisplayOrder, f => f.IndexFaker)
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.85f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2))
            .RuleFor(c => c.ParentCategoryId, f => f.PickRandom(mainCategories).Id);

        var subCategories = subCategoryFaker.Generate(150);
        await _context.BulkInsertAsync(subCategories);
        _logger.LogInformation("Inserted {Count} subcategories. Total categories: {Total}", 
            subCategories.Count, mainCategories.Count + subCategories.Count);
    }

    private async Task SeedProducts(int count)
    {
        _logger.LogInformation("Seeding {Count} products...", count);
        
        var categories = await _context.Categories.Select(c => c.Id).ToListAsync();
        var manufacturers = new[] { "Acme Corp", "TechPro", "GlobalGoods", "PremiumBrand", "ValueLine", "Elite Products" };

        var productFaker = new Faker<Product>()
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.SKU, f => $"SKU-{f.Random.AlphaNumeric(8).ToUpper()}")
            .RuleFor(p => p.Description, f => f.Lorem.Paragraph())
            .RuleFor(p => p.Price, f => f.Random.Decimal(5, 5000))
            .RuleFor(p => p.Cost, (f, p) => p.Price * f.Random.Decimal(0.3m, 0.7m))
            .RuleFor(p => p.StockQuantity, f => f.Random.Int(0, 1000))
            .RuleFor(p => p.CategoryId, f => f.PickRandom(categories))
            .RuleFor(p => p.IsActive, f => f.Random.Bool(0.9f))
            .RuleFor(p => p.IsFeatured, f => f.Random.Bool(0.1f))
            .RuleFor(p => p.Weight, f => f.Random.Decimal(0.1m, 50m))
            .RuleFor(p => p.Manufacturer, f => f.PickRandom(manufacturers))
            .RuleFor(p => p.CreatedAt, f => f.Date.Past(2))
            .RuleFor(p => p.UpdatedAt, f => f.Date.Recent(30))
            .RuleFor(p => p.ViewCount, f => f.Random.Int(0, 10000))
            .RuleFor(p => p.AverageRating, f => f.Random.Decimal(1, 5))
            .RuleFor(p => p.ReviewCount, f => f.Random.Int(0, 500));

        // Generate in batches for better memory management
        const int batchSize = 10000;
        var batches = (int)Math.Ceiling((double)count / batchSize);

        for (int i = 0; i < batches; i++)
        {
            var batchCount = Math.Min(batchSize, count - (i * batchSize));
            var products = productFaker.Generate(batchCount);
            await _context.BulkInsertAsync(products);
            _logger.LogInformation("Inserted product batch {Batch}/{Total} ({Count} products)", 
                i + 1, batches, batchCount);
        }

        _logger.LogInformation("Completed inserting {Count} products", count);
    }

    private async Task SeedCustomers(int count)
    {
        _logger.LogInformation("Seeding {Count} customers...", count);
        
        var countries = new[] { "USA", "UK", "Canada", "Germany", "France", "Australia", "Japan", "Brazil" };
        var cities = new[] { "New York", "London", "Toronto", "Berlin", "Paris", "Sydney", "Tokyo", "São Paulo" };

        var customerFaker = new Faker<Customer>()
            .RuleFor(c => c.FirstName, f => f.Name.FirstName())
            .RuleFor(c => c.LastName, f => f.Name.LastName())
            .RuleFor(c => c.Email, (f, c) => f.Internet.Email(c.FirstName, c.LastName))
            .RuleFor(c => c.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(c => c.City, f => f.PickRandom(cities))
            .RuleFor(c => c.Country, f => f.PickRandom(countries))
            .RuleFor(c => c.DateOfBirth, f => f.Date.Past(50, DateTime.Now.AddYears(-18)))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(3))
            .RuleFor(c => c.LastLoginAt, f => f.Date.Recent(30))
            .RuleFor(c => c.IsActive, f => f.Random.Bool(0.85f))
            .RuleFor(c => c.CreditLimit, f => f.Random.Decimal(1000, 50000))
            .RuleFor(c => c.TotalOrders, f => 0); // Will be updated after orders

        // Generate in batches
        const int batchSize = 10000;
        var batches = (int)Math.Ceiling((double)count / batchSize);

        for (int i = 0; i < batches; i++)
        {
            var batchCount = Math.Min(batchSize, count - (i * batchSize));
            var customers = customerFaker.Generate(batchCount);
            await _context.BulkInsertAsync(customers);
            _logger.LogInformation("Inserted customer batch {Batch}/{Total} ({Count} customers)", 
                i + 1, batches, batchCount);
        }

        _logger.LogInformation("Completed inserting {Count} customers", count);
    }

    private async Task SeedAddresses()
    {
        _logger.LogInformation("Seeding addresses...");
        
        var customerIds = await _context.Customers.Select(c => c.Id).ToListAsync();
        var countries = new[] { "USA", "UK", "Canada", "Germany", "France", "Australia", "Japan", "Brazil" };

        var addressFaker = new Faker<Address>()
            .RuleFor(a => a.CustomerId, f => f.PickRandom(customerIds))
            .RuleFor(a => a.Street, f => f.Address.StreetAddress())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.State, f => f.Address.State())
            .RuleFor(a => a.Country, f => f.PickRandom(countries))
            .RuleFor(a => a.PostalCode, f => f.Address.ZipCode())
            .RuleFor(a => a.IsDefault, f => f.Random.Bool(0.3f))
            .RuleFor(a => a.Type, f => f.PickRandom<AddressType>())
            .RuleFor(a => a.CreatedAt, f => f.Date.Past(2));

        // Each customer gets 1-3 addresses
        var addresses = new List<Address>();
        foreach (var customerId in customerIds.Take(30000)) // Not all customers have addresses
        {
            var addressCount = new Random().Next(1, 4);
            for (int i = 0; i < addressCount; i++)
            {
                var address = addressFaker.Generate();
                address.CustomerId = customerId;
                address.IsDefault = i == 0; // First address is default
                addresses.Add(address);
            }

            if (addresses.Count >= 10000)
            {
                await _context.BulkInsertAsync(addresses);
                _logger.LogInformation("Inserted address batch ({Count} addresses)", addresses.Count);
                addresses.Clear();
            }
        }

        if (addresses.Any())
        {
            await _context.BulkInsertAsync(addresses);
            _logger.LogInformation("Inserted final address batch ({Count} addresses)", addresses.Count);
        }
    }

    private async Task SeedOrders()
    {
        _logger.LogInformation("Seeding orders...");
        
        var customerIds = await _context.Customers.Select(c => c.Id).ToListAsync();

        var orderFaker = new Faker<Order>()
            .RuleFor(o => o.OrderNumber, f => $"ORD-{f.Random.AlphaNumeric(10).ToUpper()}")
            .RuleFor(o => o.CustomerId, f => f.PickRandom(customerIds))
            .RuleFor(o => o.OrderDate, f => f.Date.Past(2))
            .RuleFor(o => o.ShippedDate, (f, o) => f.Random.Bool(0.7f) ? f.Date.Between(o.OrderDate, DateTime.Now) : null)
            .RuleFor(o => o.Status, f => f.PickRandom<OrderStatus>())
            .RuleFor(o => o.TotalAmount, f => f.Random.Decimal(10, 5000))
            .RuleFor(o => o.ShippingCost, f => f.Random.Decimal(5, 50))
            .RuleFor(o => o.Tax, (f, o) => o.TotalAmount * 0.1m)
            .RuleFor(o => o.ShippingAddress, f => f.Address.FullAddress())
            .RuleFor(o => o.BillingAddress, f => f.Address.FullAddress())
            .RuleFor(o => o.Notes, f => f.Lorem.Sentence())
            .RuleFor(o => o.CreatedAt, (f, o) => o.OrderDate)
            .RuleFor(o => o.UpdatedAt, (f, o) => o.ShippedDate);

        // Generate orders - average 3 orders per customer (roughly 150k orders for 50k customers)
        var orderCount = customerIds.Count * 3;
        const int batchSize = 10000;
        var batches = (int)Math.Ceiling((double)orderCount / batchSize);

        for (int i = 0; i < batches; i++)
        {
            var batchCount = Math.Min(batchSize, orderCount - (i * batchSize));
            var orders = orderFaker.Generate(batchCount);
            await _context.BulkInsertAsync(orders);
            _logger.LogInformation("Inserted order batch {Batch}/{Total} ({Count} orders)", 
                i + 1, batches, batchCount);
        }

        _logger.LogInformation("Completed inserting {Count} orders", orderCount);
    }

    private async Task SeedOrderItems()
    {
        _logger.LogInformation("Seeding order items...");
        
        var orders = await _context.Orders.Select(o => new { o.Id, o.TotalAmount }).ToListAsync();
        var productIds = await _context.Products.Select(p => new { p.Id, p.Price }).ToListAsync();

        var orderItems = new List<OrderItem>();

        foreach (var order in orders)
        {
            var itemCount = new Random().Next(1, 8); // 1-7 items per order
            
            for (int i = 0; i < itemCount; i++)
            {
                var product = productIds[new Random().Next(productIds.Count)];
                var quantity = new Random().Next(1, 5);
                var discount = new Random().Next(0, 20) / 100m;
                
                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    Discount = discount,
                    TotalPrice = product.Price * quantity * (1 - discount)
                };
                
                orderItems.Add(orderItem);
            }

            if (orderItems.Count >= 10000)
            {
                await _context.BulkInsertAsync(orderItems);
                _logger.LogInformation("Inserted order item batch ({Count} items)", orderItems.Count);
                orderItems.Clear();
            }
        }

        if (orderItems.Any())
        {
            await _context.BulkInsertAsync(orderItems);
            _logger.LogInformation("Inserted final order item batch ({Count} items)", orderItems.Count);
        }
    }

    private async Task SeedReviews()
    {
        _logger.LogInformation("Seeding reviews...");
        
        var productIds = await _context.Products.Where(p => p.IsActive).Select(p => p.Id).ToListAsync();
        var customerIds = await _context.Customers.Where(c => c.IsActive).Select(c => c.Id).ToListAsync();

        var reviewFaker = new Faker<Review>()
            .RuleFor(r => r.ProductId, f => f.PickRandom(productIds))
            .RuleFor(r => r.CustomerId, f => f.PickRandom(customerIds))
            .RuleFor(r => r.Rating, f => f.Random.Int(1, 5))
            .RuleFor(r => r.Title, f => f.Lorem.Sentence(3, 5))
            .RuleFor(r => r.Comment, f => f.Lorem.Paragraph())
            .RuleFor(r => r.IsVerifiedPurchase, f => f.Random.Bool(0.7f))
            .RuleFor(r => r.CreatedAt, f => f.Date.Past(1))
            .RuleFor(r => r.UpdatedAt, f => f.Random.Bool(0.2f) ? f.Date.Recent(30) : null)
            .RuleFor(r => r.HelpfulCount, f => f.Random.Int(0, 100))
            .RuleFor(r => r.UnhelpfulCount, f => f.Random.Int(0, 20));

        // Generate reviews - roughly 30% of products have reviews
        var reviewCount = productIds.Count * 5; // Average 5 reviews per reviewed product
        const int batchSize = 10000;
        var batches = (int)Math.Ceiling((double)reviewCount / batchSize);

        for (int i = 0; i < batches; i++)
        {
            var batchCount = Math.Min(batchSize, reviewCount - (i * batchSize));
            var reviews = reviewFaker.Generate(batchCount);
            await _context.BulkInsertAsync(reviews);
            _logger.LogInformation("Inserted review batch {Batch}/{Total} ({Count} reviews)", 
                i + 1, batches, batchCount);
        }

        _logger.LogInformation("Completed inserting {Count} reviews", reviewCount);
    }
}