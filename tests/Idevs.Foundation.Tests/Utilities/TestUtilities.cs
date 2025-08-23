using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.EntityFramework.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Tests.Utilities;

/// <summary>
/// Test entities for comprehensive testing scenarios
/// </summary>
public class TestProduct : SoftDeletableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public JsonObject? Metadata { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation properties for relationship testing
    public List<TestOrder> Orders { get; set; } = new();
}

public class TestOrder : AuditableEntity<Guid>
{
    public int ProductId { get; set; }
    public TestProduct Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Test DbContext with all necessary configurations
/// </summary>
public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<TestProduct> Products { get; set; }
    public DbSet<TestOrder> Orders { get; set; }

    private static string? JsonObjectToString(JsonObject? jsonObject)
    {
        return jsonObject?.ToJsonString();
    }

    private static JsonObject? StringToJsonObject(string? jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
            return null;

        try
        {
            var node = JsonNode.Parse(jsonString);
            return node?.AsObject();
        }
        catch
        {
            return null;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product configuration
        modelBuilder.Entity<TestProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Category)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Price)
                  .HasPrecision(18, 2);

            // JSON column configuration with value converter for InMemory compatibility
            entity.Property(e => e.Metadata)
                  .IsRequired(false)
                  .HasConversion(
                      v => JsonObjectToString(v),
                      v => StringToJsonObject(v));

            // Soft delete index
            entity.HasIndex(e => e.IsDeleted)
                  .HasDatabaseName("IX_Products_IsDeleted");
        });

        // Order configuration  
        modelBuilder.Entity<TestOrder>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerEmail)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(e => e.TotalAmount)
                  .HasPrecision(18, 2);

            entity.Property(e => e.Status)
                  .HasConversion<string>();

            // Foreign key relationship
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.Orders)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed test data
        modelBuilder.Entity<TestProduct>().HasData(
            new TestProduct 
            { 
                Id = 1, 
                Name = "Test Product 1", 
                Price = 99.99m, 
                Category = "Electronics",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            },
            new TestProduct 
            { 
                Id = 2, 
                Name = "Test Product 2", 
                Price = 199.99m, 
                Category = "Books",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        );
    }
}

/// <summary>
/// Utilities for creating test services and dependencies
/// </summary>
public static class TestServiceFactory
{
    /// <summary>
    /// Creates a test service collection with all necessary dependencies
    /// </summary>
    public static ServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();
        
        // Logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // DbContext with InMemory database
        services.AddDbContext<TestDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        return services;
    }

    /// <summary>
    /// Creates a configured test DbContext
    /// </summary>
    public static TestDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        
        return context;
    }

    /// <summary>
    /// Creates a test DbContext with seeded data
    /// </summary>
    public static async Task<TestDbContext> CreateSeededTestDbContextAsync()
    {
        var context = CreateTestDbContext();

        // Add additional test data
        var products = new List<TestProduct>
        {
            new() 
            { 
                Name = "Seeded Product 1", 
                Price = 29.99m, 
                Category = "Software",
                Metadata = JsonNode.Parse("""{"tags": ["popular", "new"], "rating": 4.5}""")?.AsObject()
            },
            new() 
            { 
                Name = "Seeded Product 2", 
                Price = 49.99m, 
                Category = "Hardware",
                Metadata = JsonNode.Parse("""{"tags": ["premium"], "rating": 4.8}""")?.AsObject()
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        return context;
    }
}

/// <summary>
/// Test data builders for consistent test entity creation
/// </summary>
public class TestProductBuilder
{
    private TestProduct _product = new();

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

    public TestProductBuilder AsDeleted()
    {
        _product.IsDeleted = true;
        _product.DeletedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public TestProductBuilder AsInactive()
    {
        _product.IsActive = false;
        return this;
    }

    public TestProduct Build() => _product;

    public static TestProductBuilder Create() => new();
}

public class TestOrderBuilder
{
    private TestOrder _order = new() { Id = Guid.NewGuid() };

    public TestOrderBuilder ForProduct(int productId)
    {
        _order.ProductId = productId;
        return this;
    }

    public TestOrderBuilder WithQuantity(int quantity)
    {
        _order.Quantity = quantity;
        return this;
    }

    public TestOrderBuilder WithTotal(decimal total)
    {
        _order.TotalAmount = total;
        return this;
    }

    public TestOrderBuilder ForCustomer(string email)
    {
        _order.CustomerEmail = email;
        return this;
    }

    public TestOrderBuilder WithStatus(OrderStatus status)
    {
        _order.Status = status;
        return this;
    }

    public TestOrder Build() => _order;

    public static TestOrderBuilder Create() => new();
}

/// <summary>
/// Assertion helpers for test validation
/// </summary>
public static class TestAssertions
{
    public static void AssertAuditPropertiesSet<T>(T entity) where T : IHasCreatedLog, IHasUpdatedLog
    {
        Assert.True(entity.CreatedAt > DateTimeOffset.MinValue, "CreatedAt should be set");
        Assert.True(entity.UpdatedAt > DateTimeOffset.MinValue, "UpdatedAt should be set");
        Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow, "CreatedAt should not be in the future");
        Assert.True(entity.UpdatedAt <= DateTimeOffset.UtcNow, "UpdatedAt should not be in the future");
    }

    public static void AssertSoftDeleted<T>(T entity) where T : IHasDeletedLog
    {
        Assert.True(entity.IsDeleted, "Entity should be marked as deleted");
        Assert.True(entity.DeletedAt.HasValue, "DeletedAt should be set");
        Assert.True(entity.DeletedAt <= DateTimeOffset.UtcNow, "DeletedAt should not be in the future");
    }

    public static void AssertNotDeleted<T>(T entity) where T : IHasDeletedLog
    {
        Assert.False(entity.IsDeleted, "Entity should not be marked as deleted");
        Assert.False(entity.DeletedAt.HasValue, "DeletedAt should not be set");
    }

    public static void AssertEntityEquals<TEntity, TId>(TEntity expected, TEntity actual) 
        where TEntity : IHasId<TId>
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.Id, actual.Id);
    }
}