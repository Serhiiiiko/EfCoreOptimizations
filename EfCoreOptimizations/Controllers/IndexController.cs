using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using System.Diagnostics;

namespace EfCoreOptimizations.Controllers;

/// <summary>
/// Demonstrates index usage and query splitting scenarios
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class IndexController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<IndexController> _logger;

    public IndexController(AppDbContext context, ILogger<IndexController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// BAD: Search by email without index (if indexes disabled)
    /// This will cause a table scan
    /// </summary>
    [HttpGet("search/by-email")]
    public async Task<ActionResult<CustomerDetailDto>> SearchByEmail(string email)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Email == email);

        stopwatch.Stop();
        
        if (customer == null)
            return NotFound();

        _logger.LogInformation("Email search executed in {Ms}ms - Check execution plan for index usage", 
            stopwatch.ElapsedMilliseconds);

        return Ok(new CustomerDetailDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Country = customer.Country,
            City = customer.City,
            CreditLimit = customer.CreditLimit,
            TotalOrders = customer.TotalOrders
        });
    }

    /// <summary>
    /// BAD: Non-sargable query - using functions on indexed column
    /// This prevents index usage even if index exists
    /// </summary>
    [HttpGet("bad/customers-by-city")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersByCityBad(string city)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // BAD: Using ToLower() on column prevents index usage
        var result = await _context.Customers
            .AsNoTracking()
            .Where(c => c.City.ToLower() == city.ToLower())
            .Take(100)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email,
                Country = c.Country,
                TotalOrders = c.TotalOrders
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogWarning("Non-sargable query executed in {Ms}ms - Index cannot be used", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Sargable query - can use index
    /// </summary>
    [HttpGet("good/customers-by-city")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersByCityGood(string city)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // GOOD: No functions on indexed column
        var result = await _context.Customers
            .AsNoTracking()
            .Where(c => c.City == city)
            .Take(100)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email,
                Country = c.Country,
                TotalOrders = c.TotalOrders
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Sargable query executed in {Ms}ms - Index can be used", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Demonstrates composite index usage
    /// Works best with index on (Country, City, IsActive)
    /// </summary>
    [HttpGet("customers-by-location")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersByLocation(
        string country, string? city = null, bool? isActive = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var query = _context.Customers
            .AsNoTracking()
            .Where(c => c.Country == country);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(c => c.City == city);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var result = await query
            .Take(100)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email,
                Country = c.Country,
                TotalOrders = c.TotalOrders
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Composite index query executed in {Ms}ms", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Demonstrates filtering and sorting optimization
    /// </summary>
    [HttpGet("products-by-category")]
    public async Task<ActionResult<List<ProductListDto>>> GetProductsByCategory(
        int categoryId, decimal? minPrice = null, decimal? maxPrice = null, int skip = 0, int take = 50)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId && p.IsActive);

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        // Composite index (CategoryId, IsActive, Price) helps here
        var result = await query
            .OrderBy(p => p.Price)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.Price,
                CategoryName = p.Category.Name,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                IsInStock = p.StockQuantity > 0
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Filtered query executed in {Ms}ms using composite index", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// BAD: Cartesian explosion with multiple collections
    /// Loading multiple collections can cause cartesian product
    /// </summary>
    [HttpGet("bad/customer-with-all-data/{id}")]
    public async Task<ActionResult<CustomerFullDto>> GetCustomerWithAllDataBad(int id)
    {
        var stopwatch = Stopwatch.StartNew();

        // This can cause cartesian explosion
        var customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
            .Include(c => c.Reviews)
                .ThenInclude(r => r.Product)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id);

        stopwatch.Stop();

        if (customer == null)
            return NotFound();

        _logger.LogWarning("Query with multiple includes executed in {Ms}ms - Possible cartesian explosion",
            stopwatch.ElapsedMilliseconds);

        // Manual mapping to DTO (inefficient - data already loaded)
        var result = new CustomerFullDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            Phone = customer.Phone,
            City = customer.City,
            Country = customer.Country,
            DateOfBirth = customer.DateOfBirth,
            CreatedAt = customer.CreatedAt,
            LastLoginAt = customer.LastLoginAt,
            IsActive = customer.IsActive,
            CreditLimit = customer.CreditLimit,
            TotalOrders = customer.TotalOrders,
            Orders = customer.Orders.Select(o => new OrderWithItemsDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                ShippedDate = o.ShippedDate,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                ShippingCost = o.ShippingCost,
                Tax = o.Tax,
                ShippingAddress = o.ShippingAddress,
                BillingAddress = o.BillingAddress,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductName = oi.Product.Name,
                    SKU = oi.Product.SKU,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            }).ToList(),
            Reviews = customer.Reviews.Select(r => new CustomerReviewDto
            {
                Id = r.Id,
                ProductName = r.Product.Name,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            }).ToList(),
            Addresses = customer.Addresses.Select(a => new AddressDto
            {
                Id = a.Id,
                Street = a.Street,
                City = a.City,
                State = a.State,
                Country = a.Country,
                PostalCode = a.PostalCode,
                IsDefault = a.IsDefault,
                Type = a.Type.ToString()
            }).ToList()
        };

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Split query to avoid cartesian explosion + projection to DTO
    /// Uses AsSplitQuery to execute separate queries for each collection
    /// </summary>
    [HttpGet("good/customer-with-all-data/{id}")]
    public async Task<ActionResult<CustomerFullDto>> GetCustomerWithAllDataGood(int id)
    {
        var stopwatch = Stopwatch.StartNew();

        // Best approach: projection directly to DTO
        var result = await _context.Customers
            .AsNoTracking()
            .AsSplitQuery()
            .Where(c => c.Id == id)
            .Select(c => new CustomerFullDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Phone = c.Phone,
                City = c.City,
                Country = c.Country,
                DateOfBirth = c.DateOfBirth,
                CreatedAt = c.CreatedAt,
                LastLoginAt = c.LastLoginAt,
                IsActive = c.IsActive,
                CreditLimit = c.CreditLimit,
                TotalOrders = c.TotalOrders,
                Orders = c.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new OrderWithItemsDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        ShippedDate = o.ShippedDate,
                        Status = o.Status.ToString(),
                        TotalAmount = o.TotalAmount,
                        ShippingCost = o.ShippingCost,
                        Tax = o.Tax,
                        ShippingAddress = o.ShippingAddress,
                        BillingAddress = o.BillingAddress,
                        Items = o.OrderItems.Take(5).Select(oi => new OrderItemDto
                        {
                            Id = oi.Id,
                            ProductName = oi.Product.Name,
                            SKU = oi.Product.SKU,
                            Quantity = oi.Quantity,
                            UnitPrice = oi.UnitPrice,
                            TotalPrice = oi.TotalPrice
                        }).ToList()
                    }).ToList(),
                Reviews = c.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new CustomerReviewDto
                    {
                        Id = r.Id,
                        ProductName = r.Product.Name,
                        Rating = r.Rating,
                        Title = r.Title,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsVerifiedPurchase = r.IsVerifiedPurchase
                    }).ToList(),
                Addresses = c.Addresses.Select(a => new AddressDto
                {
                    Id = a.Id,
                    Street = a.Street,
                    City = a.City,
                    State = a.State,
                    Country = a.Country,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault,
                    Type = a.Type.ToString()
                }).ToList()
            })
            .FirstOrDefaultAsync();

        stopwatch.Stop();

        if (result == null)
            return NotFound();

        _logger.LogInformation("Split query with projection executed in {Ms}ms - Optimal approach",
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// Date range query optimization
    /// </summary>
    [HttpGet("orders-by-date")]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetOrdersByDateRange(
        DateTime startDate, DateTime endDate, int skip = 0, int take = 100)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Index on OrderDate helps here
        var result = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
            .OrderByDescending(o => o.OrderDate)
            .Skip(skip)
            .Take(take)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                ItemCount = o.OrderItems.Count
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Date range query executed in {Ms}ms", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }
}