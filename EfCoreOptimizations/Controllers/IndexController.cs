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
    public async Task<IActionResult> GetCustomerWithAllDataBad(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // This can cause cartesian explosion
        var customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.OrderItems)
            .Include(c => c.Reviews)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id);

        stopwatch.Stop();
        
        if (customer == null)
            return NotFound();

        _logger.LogWarning("Query with multiple includes executed in {Ms}ms - Possible cartesian explosion", 
            stopwatch.ElapsedMilliseconds);

        return Ok(customer);
    }

    /// <summary>
    /// GOOD: Split query to avoid cartesian explosion
    /// Uses AsSplitQuery to execute separate queries for each collection
    /// </summary>
    [HttpGet("good/customer-with-all-data/{id}")]
    public async Task<IActionResult> GetCustomerWithAllDataGood(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // AsSplitQuery executes separate queries for each Include
        var customer = await _context.Customers
            .AsSplitQuery() // ← This is the optimization!
            .Include(c => c.Orders.Take(10))
                .ThenInclude(o => o.OrderItems.Take(5))
            .Include(c => c.Reviews.Take(5))
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == id);

        stopwatch.Stop();
        
        if (customer == null)
            return NotFound();

        _logger.LogInformation("Split query executed in {Ms}ms - Multiple smaller queries instead of cartesian product", 
            stopwatch.ElapsedMilliseconds);

        return Ok(customer);
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