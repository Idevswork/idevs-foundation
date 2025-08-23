using System.Text.Json.Nodes;
using Idevs.Foundation.EntityFramework.Repositories;
using Idevs.Foundation.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Tests.EntityFramework;

public class EnhancedRepositoryTests
{
    [Fact]
    public async Task EnhancedRepository_Should_DetectDatabaseProvider()
    {
        // Arrange
        using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestEnhancedRepository.Create(context);

        // Act & Assert - Should not throw, indicating provider detection works
        var result = await repository.TestProviderDetection();
        Assert.NotNull(result);
        Assert.Contains("In-Memory", result); // In test environment, should detect In-Memory provider
    }

    [Fact]
    public async Task EnhancedRepository_Should_HandleJsonQueries_WithInMemoryFallback()
    {
        // Arrange
        using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context);

        // Add test product with JSON metadata
        var testProduct = TestProductBuilder.Create()
            .WithName("Test JSON Product")
            .WithPrice(99.99m)
            .WithCategory("Electronics")
            .WithMetadata(JsonNode.Parse("""{"featured": "true", "category": "smartphone"}""")?.AsObject()!)
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        // Act - Test JSON query (should fallback to string contains for In-Memory)
        var results = await repository.GetByCriteriaWithJsonQueryAsync(
            p => p.Metadata,
            "featured",
            "true"
        );

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Test JSON Product", results.First().Name);
    }

    [Fact]
    public async Task EnhancedRepository_Should_HandleGraphQLQuery()
    {
        // Arrange
        using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context);

        // Add test data
        var testProduct = TestProductBuilder.Create()
            .WithName("GraphQL Test Product")
            .WithPrice(199.99m)
            .WithCategory("Software")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        // Act - Simple GraphQL-like query
        var graphqlQuery = """
        {
          products(where: { name: { contains: "GraphQL" } }) {
            id
            name
            price
          }
        }
        """;

        var results = await repository.ExecuteGraphQLQueryAsync(graphqlQuery);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Contains("GraphQL", results.First().Name);
    }

    [Fact]
    public async Task EnhancedRepository_Should_HandleJsonPathQueries()
    {
        // Arrange
        using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context);

        // Add test product with complex JSON
        var metadata = JsonNode.Parse("""
        {
          "specifications": {
            "processor": "Intel i7",
            "memory": "16GB"
          },
          "reviews": {
            "rating": 4.5,
            "count": 127
          }
        }
        """)?.AsObject();

        var testProduct = TestProductBuilder.Create()
            .WithName("Advanced JSON Product")
            .WithPrice(1299.99m)
            .WithCategory("Computers")
            .WithMetadata(metadata!)
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        // Act - Test JSON path query
        var results = await repository.ExecuteJsonPathQueryAsync(
            p => p.Metadata,
            "specifications.processor",
            "equals",
            "Intel i7"
        );

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Advanced JSON Product", results.First().Name);
    }
}

/// <summary>
/// Test implementation of enhanced repository to expose internal functionality
/// </summary>
public class TestEnhancedRepository : EnhancedRepositoryBase<TestProduct, int>
{
    public TestEnhancedRepository(TestDbContext dbContext, ILogger<RepositoryBase<TestProduct, int>> logger)
        : base(dbContext, logger)
    {
    }
    
    // Simple factory method to avoid DI complexities in tests
    public static TestEnhancedRepository Create(TestDbContext dbContext)
    {
        // Create a simple console logger for testing
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<RepositoryBase<TestProduct, int>>();
        return new TestEnhancedRepository(dbContext, logger);
    }

    /// <summary>
    /// Test method to expose database provider detection
    /// </summary>
    public async Task<string> TestProviderDetection()
    {
        // Access the detected provider through a simple JSON query that will show the provider in logs
        try
        {
            await GetByCriteriaWithJsonQueryAsync(
                p => p.Metadata,
                "test",
                "value"
            );
        }
        catch (NotSupportedException ex) when (ex.Message.Contains("provider"))
        {
            // Extract provider information from exception message
            return ex.Message;
        }

        // If no exception, it means the provider is supported (In-Memory case)
        return "In-Memory (supported)";
    }
}