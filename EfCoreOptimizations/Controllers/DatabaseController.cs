using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using System.Diagnostics;

namespace EfCoreOptimizations.Controllers;

/// <summary>
/// Utility endpoints for database management and statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<DatabaseController> _logger;
    private readonly DataSeeder _seeder;

    public DatabaseController(AppDbContext context, ILogger<DatabaseController> logger, DataSeeder seeder)
    {
        _context = context;
        _logger = logger;
        _seeder = seeder;
    }

    /// <summary>
    /// Get database statistics
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStatistics()
    {
        var stopwatch = Stopwatch.StartNew();

        var stats = new
        {
            Customers = await _context.Customers.CountAsync(),
            ActiveCustomers = await _context.Customers.CountAsync(c => c.IsActive),
            Products = await _context.Products.CountAsync(),
            ActiveProducts = await _context.Products.CountAsync(p => p.IsActive),
            Categories = await _context.Categories.CountAsync(),
            Orders = await _context.Orders.CountAsync(),
            OrderItems = await _context.OrderItems.CountAsync(),
            Reviews = await _context.Reviews.CountAsync(),
            Addresses = await _context.Addresses.CountAsync(),
            DatabaseSizeInfo = "Check SQL Server for actual size",
            QueryTime = $"{stopwatch.ElapsedMilliseconds}ms"
        };

        _logger.LogInformation("Database statistics retrieved in {Ms}ms", stopwatch.ElapsedMilliseconds);

        return Ok(stats);
    }

    /// <summary>
    /// Get detailed table information with row counts
    /// </summary>
    [HttpGet("table-info")]
    public async Task<IActionResult> GetTableInfo()
    {
        var stopwatch = Stopwatch.StartNew();

        // Get row counts for each table
        var tableInfo = new Dictionary<string, object>
        {
            { "Customers", new { 
                Count = await _context.Customers.CountAsync(),
                ActiveCount = await _context.Customers.CountAsync(c => c.IsActive),
                SampleRecord = await _context.Customers.AsNoTracking().Take(1).Select(c => new { c.Id, c.Email, c.Country }).FirstOrDefaultAsync()
            }},
            { "Products", new { 
                Count = await _context.Products.CountAsync(),
                ActiveCount = await _context.Products.CountAsync(p => p.IsActive),
                SampleRecord = await _context.Products.AsNoTracking().Take(1).Select(p => new { p.Id, p.Name, p.SKU }).FirstOrDefaultAsync()
            }},
            { "Categories", new { 
                Count = await _context.Categories.CountAsync(),
                SampleRecord = await _context.Categories.AsNoTracking().Take(1).Select(c => new { c.Id, c.Name, c.Slug }).FirstOrDefaultAsync()
            }},
            { "Orders", new { 
                Count = await _context.Orders.CountAsync(),
                ByStatus = await _context.Orders
                    .GroupBy(o => o.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToListAsync(),
                SampleRecord = await _context.Orders.AsNoTracking().Take(1).Select(o => new { o.Id, o.OrderNumber, o.OrderDate }).FirstOrDefaultAsync()
            }},
            { "OrderItems", new { 
                Count = await _context.OrderItems.CountAsync(),
                SampleRecord = await _context.OrderItems.AsNoTracking().Take(1).Select(oi => new { oi.Id, oi.OrderId, oi.ProductId }).FirstOrDefaultAsync()
            }},
            { "Reviews", new { 
                Count = await _context.Reviews.CountAsync(),
                AverageRating = await _context.Reviews.AverageAsync(r => (double)r.Rating),
                SampleRecord = await _context.Reviews.AsNoTracking().Take(1).Select(r => new { r.Id, r.Rating, r.Title }).FirstOrDefaultAsync()
            }},
            { "Addresses", new { 
                Count = await _context.Addresses.CountAsync(),
                SampleRecord = await _context.Addresses.AsNoTracking().Take(1).Select(a => new { a.Id, a.City, a.Country }).FirstOrDefaultAsync()
            }}
        };

        stopwatch.Stop();

        return Ok(new
        {
            Tables = tableInfo,
            QueryTime = $"{stopwatch.ElapsedMilliseconds}ms"
        });
    }

    /// <summary>
    /// Reset and reseed the database (WARNING: Deletes all data)
    /// </summary>
    [HttpPost("reset-and-seed")]
    public async Task<IActionResult> ResetAndSeed(
        [FromQuery] int customerCount = 50000, 
        [FromQuery] int productsPerCategory = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogWarning("Starting database reset and reseed...");

        try
        {
            // Delete existing data
            _logger.LogInformation("Deleting existing data...");
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
            
            // Reseed
            _logger.LogInformation("Reseeding database with {CustomerCount} customers and {ProductCount} products...", 
                customerCount, productsPerCategory);
            await _seeder.SeedAsync(customerCount, productsPerCategory);

            stopwatch.Stop();
            _logger.LogInformation("Database reset and reseed completed in {Seconds}s", 
                stopwatch.Elapsed.TotalSeconds);

            return Ok(new
            {
                message = "Database reset and reseeded successfully",
                customerCount,
                productsPerCategory,
                totalTime = $"{stopwatch.Elapsed.TotalSeconds}s"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database reset and reseed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get sample data for testing
    /// </summary>
    [HttpGet("samples")]
    public async Task<IActionResult> GetSampleData()
    {
        var samples = new
        {
            Customers = await _context.Customers.AsNoTracking().Take(5).Select(c => new
            {
                c.Id,
                c.FirstName,
                c.LastName,
                c.Email,
                c.Country
            }).ToListAsync(),
            Products = await _context.Products.AsNoTracking().Take(5).Select(p => new
            {
                p.Id,
                p.Name,
                p.SKU,
                p.Price,
                CategoryName = p.Category.Name
            }).ToListAsync(),
            Categories = await _context.Categories.AsNoTracking().Take(10).Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug
            }).ToListAsync(),
            Orders = await _context.Orders.AsNoTracking().Take(5).Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.OrderDate,
                o.Status,
                CustomerName = $"{o.Customer.FirstName} {o.Customer.LastName}"
            }).ToListAsync()
        };

        return Ok(samples);
    }

    /// <summary>
    /// Check database health
    /// </summary>
    [HttpGet("health")]
    public async Task<IActionResult> CheckHealth()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            
            // Simple query to check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(503, new { status = "Unhealthy", message = "Cannot connect to database" });
            }

            // Check if database has data
            var hasData = await _context.Customers.AnyAsync();
            
            stopwatch.Stop();

            return Ok(new
            {
                status = "Healthy",
                databaseConnected = canConnect,
                hasData,
                responseTime = $"{stopwatch.ElapsedMilliseconds}ms"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return StatusCode(503, new { status = "Unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Execute a raw SQL query (for testing index performance)
    /// </summary>
    [HttpPost("execute-sql")]
    public async Task<IActionResult> ExecuteSql([FromBody] string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return BadRequest("SQL query is required");

        // Only allow SELECT statements for safety
        if (!sql.Trim().ToUpper().StartsWith("SELECT"))
            return BadRequest("Only SELECT queries are allowed");

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await _context.Database.SqlQueryRaw<dynamic>(sql).ToListAsync();
            stopwatch.Stop();

            return Ok(new
            {
                executionTime = $"{stopwatch.ElapsedMilliseconds}ms",
                rowCount = result.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SQL: {Sql}", sql);
            return StatusCode(500, new { error = ex.Message });
        }
    }
}