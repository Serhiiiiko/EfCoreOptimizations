namespace EfCoreOptimizations.Models;

/// <summary>
/// Address entity - demonstrates one-to-many relationships
/// </summary>
public class Address
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public AddressType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public Customer Customer { get; set; } = null!;
}

public enum AddressType
{
    Shipping = 0,
    Billing = 1,
    Both = 2
}