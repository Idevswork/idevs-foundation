using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using System.Text.Json.Nodes;

namespace Idevs.Foundation.Tests.Utilities;

/// <summary>
/// Test DbContext for repository testing
/// </summary>
public class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
{
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
/// Factory for creating test services and contexts
/// </summary>
public static class TestServiceFactory
{
    /// <summary>
    /// Creates a test DbContext with in-memory database
    /// </summary>
    public static TestDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        var context = new TestDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>
    /// Creates a test DbContext with seeded test data
    /// </summary>
    public static async Task<TestDbContext> CreateSeededTestDbContextAsync()
    {
        var context = CreateTestDbContext();
        await SeedTestDataAsync(context);
        return context;
    }

    /// <summary>
    /// Seeds the test database with sample data
    /// </summary>
    private static async Task SeedTestDataAsync(TestDbContext context)
    {
        // Create products without JSON metadata first
        var products = new[]
        {
            new TestProduct
            {
                Name = "Laptop Computer",
                Price = 999.99m,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-5)
            },
            new TestProduct
            {
                Name = "Wireless Mouse",
                Price = 29.99m,
                Category = "Electronics",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-8),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-3)
            },
            new TestProduct
            {
                Name = "Office Chair",
                Price = 199.99m,
                Category = "Furniture",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-6),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new TestProduct
            {
                Name = "Desk Lamp",
                Price = 49.99m,
                Category = "Furniture",
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-4),
                UpdatedAt = DateTimeOffset.UtcNow.AddDays(-2)
            },
            new TestProduct
            {
                Name = "Programming Book",
                Price = 39.99m,
                Category = "Books",
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        context.Products.AddRange(products);
        await context.SaveChangesAsync();

        // Add JSON metadata after initial save to avoid conversion issues
        var productsList = products.ToList();
        productsList[0].Metadata = JsonNode.Parse("""{"brand": "TechCorp", "warranty": "2 years"}""")?.AsObject();
        productsList[1].Metadata = JsonNode.Parse("""{"color": "black", "wireless": true}""")?.AsObject();
        productsList[2].Metadata = JsonNode.Parse("""{"material": "leather", "adjustable": true}""")?.AsObject();
        productsList[3].Metadata = JsonNode.Parse("""{"lightType": "LED", "dimmable": true}""")?.AsObject();
        productsList[4].Metadata = JsonNode.Parse("""{"author": "John Doe", "pages": 350}""")?.AsObject();

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a logger factory for testing
    /// </summary>
    public static ILoggerFactory CreateTestLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole()
                   .SetMinimumLevel(LogLevel.Debug);
        });
    }
}
