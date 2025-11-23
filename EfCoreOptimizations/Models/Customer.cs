namespace EfCoreOptimizations.Models;

/// <summary>
/// Customer entity - demonstrates various optimization scenarios
/// </summary>
public class Customer
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; // Will have index scenarios
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty; // Will have index scenarios
    public DateTime DateOfBirth { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public decimal CreditLimit { get; set; }
    public int TotalOrders { get; set; } // Denormalized field for testing
    
    // Navigation properties - for N+1 testing
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}