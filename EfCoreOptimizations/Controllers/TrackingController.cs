using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using System.Diagnostics;

namespace EfCoreOptimizations.Controllers;

/// <summary>
/// Demonstrates change tracking overhead and AsNoTracking optimization
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(AppDbContext context, ILogger<TrackingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// BAD: Uses change tracking for read-only queries
    /// EF Core tracks all entities for changes, which adds overhead
    /// </summary>
    [HttpGet("bad/products")]
    public async Task<ActionResult<List<ProductListDto>>> GetProductsWithTracking(int take = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Default behavior - change tracking is ON
        var products = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Take(take)
            .ToListAsync();

        // Convert to DTOs (but tracking overhead already occurred)
        var result = products.Select(p => new ProductListDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Price = p.Price,
            CategoryName = "", // Not loaded to keep example simple
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            IsInStock = p.StockQuantity > 0
        }).ToList();

        stopwatch.Stop();
        _logger.LogWarning("Query WITH tracking executed in {Ms}ms - Unnecessary overhead for read-only query", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Uses AsNoTracking for read-only queries
    /// Better performance because EF Core doesn't track changes
    /// </summary>
    [HttpGet("good/products")]
    public async Task<ActionResult<List<ProductListDto>>> GetProductsNoTracking(int take = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // AsNoTracking - no change tracking overhead
        var result = await _context.Products
            .AsNoTracking() // ← This is the optimization!
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
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
        _logger.LogInformation("Query WITHOUT tracking executed in {Ms}ms - Optimized for read-only", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// BAD: Tracks 1000+ entities when we just need to read them
    /// </summary>
    [HttpGet("bad/customers")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersWithTracking(int take = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Tracking all 1000 customers
        var customers = await _context.Customers
            .Include(c => c.Orders)
            .Where(c => c.IsActive)
            .Take(take)
            .ToListAsync();

        var result = customers.Select(c => new CustomerListDto
        {
            Id = c.Id,
            FullName = $"{c.FirstName} {c.LastName}",
            Email = c.Email,
            Country = c.Country,
            TotalOrders = c.Orders.Count
        }).ToList();

        stopwatch.Stop();
        _logger.LogWarning("Tracked {Count} customers in {Ms}ms - Memory overhead for no reason", 
            customers.Count, stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: No tracking + projection = maximum performance
    /// </summary>
    [HttpGet("good/customers")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersNoTracking(int take = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // No tracking + projection = best performance
        var result = await _context.Customers
            .AsNoTracking()
            .Where(c => c.IsActive)
            .Take(take)
            .Select(c => new CustomerListDto
            {
                Id = c.Id,
                FullName = $"{c.FirstName} {c.LastName}",
                Email = c.Email,
                Country = c.Country,
                TotalOrders = c.Orders.Count
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("No tracking query executed in {Ms}ms - Minimal memory footprint", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// SCENARIO: When you NEED tracking - for updates
    /// </summary>
    [HttpPut("product/{id}/price")]
    public async Task<IActionResult> UpdateProductPrice(int id, [FromBody] decimal newPrice)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Here we NEED tracking because we're updating
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        product.Price = newPrice;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        stopwatch.Stop();
        _logger.LogInformation("Product updated with tracking in {Ms}ms", 
            stopwatch.ElapsedMilliseconds);

        return Ok(new { message = "Price updated", newPrice });
    }

    /// <summary>
    /// ALTERNATIVE: Update without tracking using ExecuteUpdate (EF Core 7+)
    /// Even better for simple updates
    /// </summary>
    [HttpPut("product/{id}/price-bulk")]
    public async Task<IActionResult> UpdateProductPriceBulk(int id, [FromBody] decimal newPrice)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // No tracking - direct SQL update
        var rowsAffected = await _context.Products
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Price, newPrice)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

        stopwatch.Stop();
        
        if (rowsAffected == 0)
            return NotFound();

        _logger.LogInformation("Product updated with ExecuteUpdate in {Ms}ms - No tracking needed", 
            stopwatch.ElapsedMilliseconds);

        return Ok(new { message = "Price updated (bulk)", newPrice });
    }
}