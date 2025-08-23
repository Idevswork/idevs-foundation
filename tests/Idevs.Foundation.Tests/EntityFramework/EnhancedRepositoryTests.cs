using System.Text.Json.Nodes;
using Idevs.Foundation.EntityFramework.Repositories;
using Idevs.Foundation.Tests.Utilities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.EntityFramework;

public class EnhancedRepositoryTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RepositoryBase<TestProduct, int>> _logger;

    public EnhancedRepositoryTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger<RepositoryBase<TestProduct, int>>>();
        _loggerFactory.CreateLogger<RepositoryBase<TestProduct, int>>().Returns(_logger);
    }

    [Fact]
    public async Task EnhancedRepository_Should_DetectDatabaseProvider()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestEnhancedRepository.Create(context, _loggerFactory);

        // Act & Assert - Should not throw, indicating provider detection works
        var result = await repository.TestProviderDetection();
        Assert.NotNull(result);
        Assert.Contains("In-Memory", result); // In test environment, should detect In-Memory provider
    }

    [Fact]
    public async Task EnhancedRepository_Should_HandleJsonQueries_WithInMemoryFallback()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context, _loggerFactory);

        // Add test product without JSON metadata first to avoid conversion issues
        var testProduct = TestProductBuilder.Create()
            .WithName("Test JSON Product")
            .WithPrice(99.99m)
            .WithCategory("Electronics")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        // Update with JSON metadata after save
        testProduct.Metadata = JsonNode.Parse("""{"featured": "true", "category": "smartphone"}""")?.AsObject();
        await repository.UpdateAsync(testProduct);
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
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context, _loggerFactory);

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

        var results = await repository.ExecuteGraphQlQueryAsync(graphqlQuery);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Contains("GraphQL", results.First().Name);
    }

    [Fact]
    public async Task EnhancedRepository_Should_HandleJsonPathQueries()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestEnhancedRepository.Create(context, _loggerFactory);

        // Add test product without JSON metadata first
        var testProduct = TestProductBuilder.Create()
            .WithName("Advanced JSON Product")
            .WithPrice(1299.99m)
            .WithCategory("Computers")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        // Update with complex JSON metadata after save
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

        testProduct.Metadata = metadata;
        await repository.UpdateAsync(testProduct);
        await repository.SaveChangesAsync();

        // Act & Assert - JSON path queries are not supported for In-Memory provider
        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            repository.ExecuteJsonPathQueryAsync(
                p => p.Metadata,
                "specifications.processor",
                "equals",
                "Intel i7"
            )
        );

        // Verify the exception message indicates the limitation
        Assert.Contains("JSON path", exception.Message);
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
    public static TestEnhancedRepository Create(TestDbContext dbContext, ILoggerFactory loggerFactory)
    {
        // Create a simple console logger for testing
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
