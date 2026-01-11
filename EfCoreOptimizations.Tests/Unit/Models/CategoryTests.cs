using EfCoreOptimizations.Models;

namespace EfCoreOptimizations.Tests.Unit.Models;

public class CategoryTests
{
    [Test]
    public async Task Category_DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        await Assert.That(category.Id).IsEqualTo(0);
        await Assert.That(category.Name).IsEqualTo(string.Empty);
        await Assert.That(category.Slug).IsEqualTo(string.Empty);
        await Assert.That(category.Description).IsEqualTo(string.Empty);
        await Assert.That(category.ParentCategoryId).IsNull();
        await Assert.That(category.DisplayOrder).IsEqualTo(0);
        await Assert.That(category.IsActive).IsFalse();
    }

    [Test]
    public async Task Category_NavigationProperties_ShouldBeInitializedAsEmptyCollections()
    {
        // Arrange & Act
        var category = new Category();

        // Assert
        await Assert.That(category.SubCategories).IsNotNull();
        await Assert.That(category.SubCategories.Count).IsEqualTo(0);
        await Assert.That(category.Products).IsNotNull();
        await Assert.That(category.Products.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Category_ParentCategory_ShouldBeNullable()
    {
        // Arrange
        var category = new Category();

        // Assert
        await Assert.That(category.ParentCategory).IsNull();
        await Assert.That(category.ParentCategoryId).IsNull();
    }

    [Test]
    public async Task Category_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var createdAt = DateTime.UtcNow;

        var category = new Category
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics",
            Description = "Electronic devices and gadgets",
            ParentCategoryId = null,
            DisplayOrder = 1,
            IsActive = true,
            CreatedAt = createdAt
        };

        // Assert
        await Assert.That(category.Id).IsEqualTo(1);
        await Assert.That(category.Name).IsEqualTo("Electronics");
        await Assert.That(category.Slug).IsEqualTo("electronics");
        await Assert.That(category.Description).IsEqualTo("Electronic devices and gadgets");
        await Assert.That(category.ParentCategoryId).IsNull();
        await Assert.That(category.DisplayOrder).IsEqualTo(1);
        await Assert.That(category.IsActive).IsTrue();
        await Assert.That(category.CreatedAt).IsEqualTo(createdAt);
    }

    [Test]
    public async Task Category_SelfReferencing_ShouldWorkCorrectly()
    {
        // Arrange
        var parentCategory = new Category
        {
            Id = 1,
            Name = "Electronics",
            Slug = "electronics"
        };

        var subCategory = new Category
        {
            Id = 2,
            Name = "Phones",
            Slug = "phones",
            ParentCategoryId = 1,
            ParentCategory = parentCategory
        };

        // Act
        parentCategory.SubCategories.Add(subCategory);

        // Assert
        await Assert.That(parentCategory.SubCategories.Count).IsEqualTo(1);
        await Assert.That(parentCategory.SubCategories.First().Name).IsEqualTo("Phones");
        await Assert.That(subCategory.ParentCategory).IsNotNull();
        await Assert.That(subCategory.ParentCategory!.Name).IsEqualTo("Electronics");
        await Assert.That(subCategory.ParentCategoryId).IsEqualTo(1);
    }

    [Test]
    public async Task Category_AddProduct_ShouldAddToCollection()
    {
        // Arrange
        var category = new Category { Id = 1, Name = "Electronics" };
        var product = new Product { Id = 1, Name = "iPhone", CategoryId = 1 };

        // Act
        category.Products.Add(product);

        // Assert
        await Assert.That(category.Products.Count).IsEqualTo(1);
        await Assert.That(category.Products.First().Name).IsEqualTo("iPhone");
    }

    [Test]
    public async Task Category_HierarchyDepth_CanBeMultipleLevels()
    {
        // Arrange
        var root = new Category { Id = 1, Name = "Root" };
        var level1 = new Category { Id = 2, Name = "Level 1", ParentCategoryId = 1, ParentCategory = root };
        var level2 = new Category { Id = 3, Name = "Level 2", ParentCategoryId = 2, ParentCategory = level1 };

        root.SubCategories.Add(level1);
        level1.SubCategories.Add(level2);

        // Assert
        await Assert.That(root.SubCategories.Count).IsEqualTo(1);
        await Assert.That(root.SubCategories.First().SubCategories.Count).IsEqualTo(1);
        await Assert.That(level2.ParentCategory!.ParentCategory!.Name).IsEqualTo("Root");
    }
}
