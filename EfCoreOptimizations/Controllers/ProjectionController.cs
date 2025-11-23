using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using EfCoreOptimizations.Models;
using System.Diagnostics;

namespace EfCoreOptimizations.Controllers;

/// <summary>
/// Demonstrates projection vs full entity loading
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProjectionController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ProjectionController> _logger;

    public ProjectionController(AppDbContext context, ILogger<ProjectionController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// BAD: Loads full Product entities with all properties even though we only need a few
    /// This transfers unnecessary data from database to application
    /// </summary>
    [HttpGet("bad/product-list")]
    public async Task<ActionResult<List<ProductListDto>>> GetProductListBad(int skip = 0, int take = 50)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Loads ALL columns from Products table even though we only use a few
        var products = await _context.Products
            .Include(p => p.Category) // Loads ALL category columns too
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        // Manual mapping after loading everything
        var result = products.Select(p => new ProductListDto
        {
            Id = p.Id,
            Name = p.Name,
            SKU = p.SKU,
            Price = p.Price,
            CategoryName = p.Category.Name,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            IsInStock = p.StockQuantity > 0
        }).ToList();

        stopwatch.Stop();
        _logger.LogWarning("Full entity loading executed in {Ms}ms - Loaded ALL columns unnecessarily", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Projects only needed columns using Select
    /// Database only sends the exact data we need
    /// </summary>
    [HttpGet("good/product-list")]
    public async Task<ActionResult<List<ProductListDto>>> GetProductListGood(int skip = 0, int take = 50)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Projects only the columns we actually need
        var result = await _context.Products
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Skip(skip)
            .Take(take)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Price = p.Price,
                CategoryName = p.Category.Name, // Only gets Category.Name
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                IsInStock = p.StockQuantity > 0
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Projection query executed in {Ms}ms - Only loaded needed columns", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// BAD: Loads full customer entities and returns them directly
    /// Exposes all data including sensitive fields, no DTO
    /// </summary>
    [HttpGet("bad/customers")]
    public async Task<ActionResult<List<Customer>>> GetCustomersBad(int take = 100)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Returns full entities - bad for API responses
        var customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .Take(take)
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogWarning("Full entity query executed in {Ms}ms - Exposes all data", 
            stopwatch.ElapsedMilliseconds);

        return Ok(customers);
    }

    /// <summary>
    /// GOOD: Projects to lightweight DTO with only needed fields
    /// Better performance and security
    /// </summary>
    [HttpGet("good/customers")]
    public async Task<ActionResult<List<CustomerListDto>>> GetCustomersGood(int take = 100)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Projects to DTO with only needed fields
        var result = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .Take(take)
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
        _logger.LogInformation("Projection query executed in {Ms}ms - Lightweight DTO", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// BAD: Loads product with ALL reviews, then filters in memory
    /// </summary>
    [HttpGet("bad/product-with-reviews/{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProductWithReviewsBad(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Loads product with ALL reviews
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.Customer)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        // Filtering happens in memory after loading everything
        var recentReviews = product.Reviews
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase
            })
            .ToList();

        var result = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            SKU = product.SKU,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryName = product.Category.Name,
            Manufacturer = product.Manufacturer,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount,
            RecentReviews = recentReviews
        };

        stopwatch.Stop();
        _logger.LogWarning("Loaded ALL reviews then filtered in memory - executed in {Ms}ms", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Projects only what's needed, filters at database level
    /// </summary>
    [HttpGet("good/product-with-reviews/{id}")]
    public async Task<ActionResult<ProductDetailDto>> GetProductWithReviewsGood(int id)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Single query, filters reviews at database level
        var result = await _context.Products
            .Where(p => p.Id == id)
            .Select(p => new ProductDetailDto
            {
                Id = p.Id,
                Name = p.Name,
                SKU = p.SKU,
                Description = p.Description,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                CategoryName = p.Category.Name,
                Manufacturer = p.Manufacturer,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                RecentReviews = p.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new ReviewDto
                    {
                        Id = r.Id,
                        CustomerName = $"{r.Customer.FirstName} {r.Customer.LastName}",
                        Rating = r.Rating,
                        Title = r.Title,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt,
                        IsVerifiedPurchase = r.IsVerifiedPurchase
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (result == null)
            return NotFound();

        stopwatch.Stop();
        _logger.LogInformation("Projection with server-side filtering executed in {Ms}ms", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }
}