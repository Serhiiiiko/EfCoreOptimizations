using Microsoft.EntityFrameworkCore;
using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Data;

/// <summary>
/// Main DbContext with configurable optimization scenarios
/// </summary>
public class AppDbContext : DbContext
{
    private readonly bool _useIndexes;

    public AppDbContext(DbContextOptions<AppDbContext> options, bool useIndexes = true) 
        : base(options)
    {
        _useIndexes = useIndexes;
    }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Address> Addresses => Set<Address>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureCustomer(modelBuilder);
        ConfigureOrder(modelBuilder);
        ConfigureOrderItem(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureReview(modelBuilder);
        ConfigureAddress(modelBuilder);
    }

    private void ConfigureCustomer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);

            if (_useIndexes)
            {
                // OPTIMIZATION: Index on frequently searched columns
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Country);
                entity.HasIndex(e => e.City);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.CreatedAt);
                
                // OPTIMIZATION: Composite index for common queries
                entity.HasIndex(e => new { e.Country, e.City, e.IsActive });
            }

            entity.HasMany(e => e.Orders)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Reviews)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Addresses)
                .WithOne(e => e.Customer)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureOrder(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(18, 2);
            entity.Property(e => e.Tax).HasPrecision(18, 2);
            entity.Property(e => e.ShippingAddress).HasMaxLength(500);
            entity.Property(e => e.BillingAddress).HasMaxLength(500);

            if (_useIndexes)
            {
                // OPTIMIZATION: Index on foreign key
                entity.HasIndex(e => e.CustomerId);
                
                // OPTIMIZATION: Index on frequently filtered columns
                entity.HasIndex(e => e.OrderDate);
                entity.HasIndex(e => e.Status);
                
                // OPTIMIZATION: Composite index for date range queries
                entity.HasIndex(e => new { e.OrderDate, e.Status });
                
                // OPTIMIZATION: Unique index on order number
                entity.HasIndex(e => e.OrderNumber).IsUnique();
            }

            entity.HasMany(e => e.OrderItems)
                .WithOne(e => e.Order)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureOrderItem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Discount).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

            if (_useIndexes)
            {
                // OPTIMIZATION: Index on foreign keys
                entity.HasIndex(e => e.OrderId);
                entity.HasIndex(e => e.ProductId);
                
                // OPTIMIZATION: Composite index for order-product lookups
                entity.HasIndex(e => new { e.OrderId, e.ProductId });
            }
        });
    }

    private void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.Property(e => e.Cost).HasPrecision(18, 2);
            entity.Property(e => e.Weight).HasPrecision(10, 2);
            entity.Property(e => e.Manufacturer).HasMaxLength(100);
            entity.Property(e => e.AverageRating).HasPrecision(3, 2);

            if (_useIndexes)
            {
                // OPTIMIZATION: Unique index on SKU
                entity.HasIndex(e => e.SKU).IsUnique();
                
                // OPTIMIZATION: Index on foreign key
                entity.HasIndex(e => e.CategoryId);
                
                // OPTIMIZATION: Indexes for filtering and sorting
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.IsFeatured);
                entity.HasIndex(e => e.Price);
                entity.HasIndex(e => e.AverageRating);
                
                // OPTIMIZATION: Composite index for product catalog queries
                entity.HasIndex(e => new { e.CategoryId, e.IsActive, e.Price });
                
                // OPTIMIZATION: Covering index for product list with rating
                entity.HasIndex(e => new { e.CategoryId, e.IsActive })
                    .IncludeProperties(e => new { e.Name, e.Price, e.AverageRating });
            }

            entity.HasOne(e => e.Category)
                .WithMany(e => e.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.OrderItems)
                .WithOne(e => e.Product)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Reviews)
                .WithOne(e => e.Product)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            if (_useIndexes)
            {
                // OPTIMIZATION: Unique index on slug for URL lookups
                entity.HasIndex(e => e.Slug).IsUnique();
                
                // OPTIMIZATION: Index on parent category for hierarchical queries
                entity.HasIndex(e => e.ParentCategoryId);
                
                // OPTIMIZATION: Index on active categories
                entity.HasIndex(e => e.IsActive);
            }

            // Self-referencing relationship
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.Comment).HasMaxLength(2000);

            if (_useIndexes)
            {
                // OPTIMIZATION: Index on foreign keys
                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.CustomerId);
                
                // OPTIMIZATION: Index on rating for filtering
                entity.HasIndex(e => e.Rating);
                
                // OPTIMIZATION: Index on verified purchases
                entity.HasIndex(e => e.IsVerifiedPurchase);
                
                // OPTIMIZATION: Composite index for product reviews by rating
                entity.HasIndex(e => new { e.ProductId, e.Rating, e.CreatedAt });
            }
        });
    }

    private void ConfigureAddress(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Street).HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);

            if (_useIndexes)
            {
                // OPTIMIZATION: Index on foreign key
                entity.HasIndex(e => e.CustomerId);
                
                // OPTIMIZATION: Index for default address lookup
                entity.HasIndex(e => new { e.CustomerId, e.IsDefault });
            }
        });
    }
}