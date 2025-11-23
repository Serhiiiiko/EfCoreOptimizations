using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Data;
using EfCoreOptimizations.DTOs;
using System.Diagnostics;

namespace EfCoreOptimizations.Controllers;

/// <summary>
/// Demonstrates N+1 query problems and solutions
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NPlusOneController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<NPlusOneController> _logger;

    public NPlusOneController(AppDbContext context, ILogger<NPlusOneController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// BAD: N+1 query problem - loads customers then queries orders for each customer
    /// This will generate 1 query for customers + N queries for orders (one per customer)
    /// </summary>
    [HttpGet("bad/customers-with-orders")]
    public async Task<ActionResult<List<CustomerDetailDto>>> GetCustomersWithOrdersBad(int take = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Query 1: Load customers
        var customers = await _context.Customers
            .Where(c => c.IsActive)
            .Take(take)
            .ToListAsync();

        var result = new List<CustomerDetailDto>();

        // N+1 Problem: This loops through each customer and queries orders separately
        foreach (var customer in customers)
        {
            // Query N: One query per customer!
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customer.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
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

            result.Add(new CustomerDetailDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Country = customer.Country,
                City = customer.City,
                CreditLimit = customer.CreditLimit,
                TotalOrders = customer.TotalOrders,
                RecentOrders = orders
            });
        }

        stopwatch.Stop();
        _logger.LogWarning("N+1 Query executed in {Ms}ms - Generated {Count} database queries", 
            stopwatch.ElapsedMilliseconds, customers.Count + 1);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Eager loading with Include - loads everything in 1-2 queries
    /// </summary>
    [HttpGet("good/customers-with-orders")]
    public async Task<ActionResult<List<CustomerDetailDto>>> GetCustomersWithOrdersGood(int take = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Single query with JOIN - much more efficient!
        var result = await _context.Customers
            .Where(c => c.IsActive)
            .Take(take)
            .Select(c => new CustomerDetailDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Country = c.Country,
                City = c.City,
                CreditLimit = c.CreditLimit,
                TotalOrders = c.TotalOrders,
                RecentOrders = c.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new OrderSummaryDto
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        Status = o.Status.ToString(),
                        TotalAmount = o.TotalAmount,
                        ItemCount = o.OrderItems.Count
                    })
                    .ToList()
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Optimized query executed in {Ms}ms - Generated 1-2 database queries", 
            stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// BAD: Loads full order entities with all navigation properties loaded separately
    /// </summary>
    [HttpGet("bad/orders-with-items")]
    public async Task<ActionResult<List<OrderDetailDto>>> GetOrdersWithItemsBad(int take = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Load orders
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .Take(take)
            .ToListAsync();

        var result = new List<OrderDetailDto>();

        // N+1: Loading items for each order separately
        foreach (var order in orders)
        {
            // Separate query for each order's items
            var items = await _context.OrderItems
                .Include(oi => oi.Product)
                .Where(oi => oi.OrderId == order.Id)
                .Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductName = oi.Product.Name,
                    SKU = oi.Product.SKU,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                })
                .ToListAsync();

            result.Add(new OrderDetailDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                OrderDate = order.OrderDate,
                Status = order.Status.ToString(),
                TotalAmount = order.TotalAmount,
                CustomerName = $"{order.Customer.FirstName} {order.Customer.LastName}",
                Items = items
            });
        }

        stopwatch.Stop();
        _logger.LogWarning("N+1 Query executed in {Ms}ms", stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }

    /// <summary>
    /// GOOD: Single query with proper projection
    /// </summary>
    [HttpGet("good/orders-with-items")]
    public async Task<ActionResult<List<OrderDetailDto>>> GetOrdersWithItemsGood(int take = 20)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Single query with all needed data
        var result = await _context.Orders
            .OrderByDescending(o => o.OrderDate)
            .Take(take)
            .Select(o => new OrderDetailDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                CustomerName = $"{o.Customer.FirstName} {o.Customer.LastName}",
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductName = oi.Product.Name,
                    SKU = oi.Product.SKU,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    TotalPrice = oi.TotalPrice
                }).ToList()
            })
            .ToListAsync();

        stopwatch.Stop();
        _logger.LogInformation("Optimized query executed in {Ms}ms", stopwatch.ElapsedMilliseconds);

        return Ok(result);
    }
}