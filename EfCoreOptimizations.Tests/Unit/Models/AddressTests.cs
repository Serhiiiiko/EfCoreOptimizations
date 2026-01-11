using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class AddressTests
{
    [Test]
    public async Task Address_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var address = new Address();

        // Assert
        await Assert.That(address.Id).IsEqualTo(0);
        await Assert.That(address.CustomerId).IsEqualTo(0);
        await Assert.That(address.Street).IsEqualTo(string.Empty);
        await Assert.That(address.City).IsEqualTo(string.Empty);
        await Assert.That(address.State).IsEqualTo(string.Empty);
        await Assert.That(address.Country).IsEqualTo(string.Empty);
        await Assert.That(address.PostalCode).IsEqualTo(string.Empty);
        await Assert.That(address.IsDefault).IsFalse();
        await Assert.That(address.Type).IsEqualTo(AddressType.Shipping);
    }

    [Test]
    public async Task Address_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        var address = new Address
        {
            Id = 1,
            CustomerId = 100,
            Street = "123 Main Street",
            City = "New York",
            State = "NY",
            Country = "USA",
            PostalCode = "10001",
            IsDefault = true,
            Type = AddressType.Both,
            CreatedAt = createdAt
        };

        // Assert
        await Assert.That(address.Id).IsEqualTo(1);
        await Assert.That(address.CustomerId).IsEqualTo(100);
        await Assert.That(address.Street).IsEqualTo("123 Main Street");
        await Assert.That(address.City).IsEqualTo("New York");
        await Assert.That(address.State).IsEqualTo("NY");
        await Assert.That(address.Country).IsEqualTo("USA");
        await Assert.That(address.PostalCode).IsEqualTo("10001");
        await Assert.That(address.IsDefault).IsTrue();
        await Assert.That(address.Type).IsEqualTo(AddressType.Both);
        await Assert.That(address.CreatedAt).IsEqualTo(createdAt);
    }

    [Test]
    [Arguments(AddressType.Shipping)]
    [Arguments(AddressType.Billing)]
    [Arguments(AddressType.Both)]
    public async Task Address_AllTypeValues_ShouldBeAssignable(AddressType type)
    {
        // Arrange
        var address = new Address();

        // Act
        address.Type = type;

        // Assert
        await Assert.That(address.Type).IsEqualTo(type);
    }

    [Test]
    public async Task Address_NavigationProperty_Customer_ShouldBeAssignable()
    {
        // Arrange
        var customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe" };

        var address = new Address
        {
            Id = 1,
            CustomerId = 1,
            Customer = customer
        };

        // Assert
        await Assert.That(address.Customer).IsNotNull();
        await Assert.That(address.Customer.FirstName).IsEqualTo("John");
    }
}

public class AddressTypeTests
{
    [Test]
    public async Task AddressType_ShouldHaveCorrectValues()
    {
        // Assert
        await Assert.That((int)AddressType.Shipping).IsEqualTo(0);
        await Assert.That((int)AddressType.Billing).IsEqualTo(1);
        await Assert.That((int)AddressType.Both).IsEqualTo(2);
    }

    [Test]
    public async Task AddressType_ShouldHave3Values()
    {
        // Arrange & Act
        var values = Enum.GetValues<AddressType>();

        // Assert
        await Assert.That(values.Length).IsEqualTo(3);
    }
}
