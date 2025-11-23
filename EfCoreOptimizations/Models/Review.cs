namespace EfCoreOptimizations.Models;

/// <summary>
/// Review entity - demonstrates text search and filtering scenarios
/// </summary>
public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int CustomerId { get; set; }
    public int Rating { get; set; } // 1-5
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool IsVerifiedPurchase { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int HelpfulCount { get; set; }
    public int UnhelpfulCount { get; set; }
    
    // Navigation properties
    public Product Product { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}