using System.Text.Json.Nodes;
using Idevs.Foundation.EntityFramework.Entities;

namespace Idevs.Foundation.Tests.Utilities;

/// <summary>
/// Test entity for repository testing
/// </summary>
public class TestProduct : SoftDeletableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public JsonObject? Metadata { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties for relationship testing
    public List<TestOrder> Orders { get; set; } = [];
}

/// <summary>
/// Builder pattern for creating test products
/// </summary>
public class TestProductBuilder
{
    private readonly TestProduct _product = new();

    public static TestProductBuilder Create() => new();

    public TestProductBuilder WithName(string name)
    {
        _product.Name = name;
        return this;
    }

    public TestProductBuilder WithPrice(decimal price)
    {
        _product.Price = price;
        return this;
    }

    public TestProductBuilder WithCategory(string category)
    {
        _product.Category = category;
        return this;
    }

    public TestProductBuilder WithMetadata(JsonObject metadata)
    {
        _product.Metadata = metadata;
        return this;
    }

    public TestProductBuilder WithActive(bool isActive)
    {
        _product.IsActive = isActive;
        return this;
    }

    public TestProduct Build() => _product;
}
