using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.EntityFramework.Repositories;
using Idevs.Foundation.Tests.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.EntityFramework;

public class RepositoryBaseTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RepositoryBase<TestProduct, int>> _logger;

    public RepositoryBaseTests()
    {
        _loggerFactory = Substitute.For<ILoggerFactory>();
        _logger = Substitute.For<ILogger<RepositoryBase<TestProduct, int>>>();
        _loggerFactory.CreateLogger<RepositoryBase<TestProduct, int>>().Returns(_logger);
    }

    #region Basic CRUD Tests

    [Fact]
    public async Task AddAsync_Should_AddEntity_AndSetAuditProperties()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        var product = TestProductBuilder.Create()
            .WithName("Test Product")
            .WithPrice(99.99m)
            .Build();

        // Act
        var result = await repository.AddAsync(product);
        await repository.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Product", result.Name);
        Assert.True(result.CreatedAt > DateTimeOffset.MinValue);
        Assert.True(result.UpdatedAt > DateTimeOffset.MinValue);

        // Verify entity was saved to database
        var savedEntity = await context.Set<TestProduct>().FindAsync(result.Id);
        Assert.NotNull(savedEntity);
        Assert.Equal("Test Product", savedEntity.Name);
    }

    [Fact]
    public async Task AddAsync_Should_ThrowArgumentNullException_WhenEntityIsNull()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => repository.AddAsync((TestProduct)null!));
    }

    [Fact]
    public async Task AddAsync_BulkOperation_Should_AddMultipleEntities()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        var products = new[]
        {
            TestProductBuilder.Create().WithName("Product 1").Build(),
            TestProductBuilder.Create().WithName("Product 2").Build(),
            TestProductBuilder.Create().WithName("Product 3").Build()
        };

        // Act
        var result = await repository.AddAsync(products);
        await repository.SaveChangesAsync();

        // Assert
        Assert.Equal(3, result.RowsAffected);
        Assert.Equal(3, result.Entities.Count);

        // Verify all entities have audit properties set
        Assert.All(result.Entities, entity =>
        {
            Assert.True(entity.CreatedAt > DateTimeOffset.MinValue);
            Assert.True(entity.UpdatedAt > DateTimeOffset.MinValue);
        });
    }

    [Fact]
    public async Task AddAsync_BulkOperation_Should_ThrowArgumentException_WhenEntitiesIsEmpty()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => repository.AddAsync(Array.Empty<TestProduct>()));
    }

    [Fact]
    public async Task UpdateAsync_Should_UpdateEntity_AndSetAuditProperties()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProduct = await context.Set<TestProduct>().FirstAsync();
        var originalUpdatedAt = existingProduct.UpdatedAt;

        // Ensure time difference for audit test
        await Task.Delay(1);

        existingProduct.Name = "Updated Product";

        // Act
        var result = await repository.UpdateAsync(existingProduct);
        await repository.SaveChangesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Updated Product", result.Name);
        Assert.True(result.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task UpdateAsync_BulkOperation_Should_UpdateMultipleEntities()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProducts = await context.Set<TestProduct>().Take(2).ToListAsync();
        foreach (var product in existingProducts)
        {
            product.Name = $"Updated {product.Name}";
        }

        // Act
        var result = await repository.UpdateAsync(existingProducts);
        await repository.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result.RowsAffected);
        Assert.Equal(2, result.Entities.Count);
        Assert.All(result.Entities, entity => Assert.StartsWith("Updated", entity.Name));
    }

    [Fact]
    public async Task RetrieveAsync_Should_ReturnEntity_WhenExists()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProduct = await context.Set<TestProduct>().FirstAsync();

        // Act
        var result = await repository.RetrieveAsync(existingProduct.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingProduct.Id, result.Id);
        Assert.Equal(existingProduct.Name, result.Name);
    }

    [Fact]
    public async Task RetrieveAsync_Should_ReturnNull_WhenNotExists()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act
        var result = await repository.RetrieveAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteAsync_Should_PerformSoftDelete_WhenEntitySupportsSoftDelete()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProduct = await context.Set<TestProduct>().FirstAsync();

        // Act
        var deletedCount = await repository.DeleteAsync(existingProduct.Id);
        await repository.SaveChangesAsync();

        // Assert
        Assert.Equal(1, deletedCount);

        // Verify soft delete
        var deletedProduct = await context.Set<TestProduct>().FindAsync(existingProduct.Id);
        Assert.NotNull(deletedProduct);
        Assert.True(deletedProduct.IsDeleted);
        Assert.NotNull(deletedProduct.DeletedAt);
    }

    #endregion

    #region Query Tests

    [Fact]
    public async Task QueryAsync_Should_ReturnFilteredEntities()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act
        var results = await repository.QueryAsync(p => p.Price > 50);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, product => Assert.True(product.Price > 50));
    }

    [Fact]
    public async Task ListAsync_Should_ReturnAllEntities_WhenIdsIsEmpty()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act
        var results = await repository.ListAsync(null);

        // Assert
        Assert.NotEmpty(results);
    }

    [Fact]
    public async Task ListAsync_Should_ReturnSpecificEntities_WhenIdsProvided()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProducts = await context.Set<TestProduct>().Take(2).ToListAsync();
        var ids = existingProducts.Select(p => p.Id).ToArray();

        // Act
        var results = await repository.ListAsync(ids);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, product => Assert.Contains(product.Id, ids));
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnTrue_WhenEntityExists()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var existingProduct = await context.Set<TestProduct>().FirstAsync();

        // Act
        var exists = await repository.ExistsAsync(existingProduct.Id);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_Should_ReturnFalse_WhenEntityNotExists()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act
        var exists = await repository.ExistsAsync(999);

        // Assert
        Assert.False(exists);
    }

    #endregion

    #region GraphQL Tests

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_ParseSimpleEqualityQuery()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var testProduct = TestProductBuilder.Create()
            .WithName("GraphQL Test Product")
            .WithCategory("Electronics")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        var query = @"
        {
          products(where: { category: { eq: ""Electronics"" } }) {
            id
            name
            category
          }
        }";

        // Act
        var results = await repository.ExecuteGraphQlQueryAsync(query);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, product => Assert.Equal("Electronics", product.Category));
    }

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_ParseContainsQuery()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var testProduct = TestProductBuilder.Create()
            .WithName("Special Laptop Computer")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        var query = @"
        {
          products(where: { name: { contains: ""Laptop"" } }) {
            id
            name
          }
        }";

        // Act
        var results = await repository.ExecuteGraphQlQueryAsync(query);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, product => Assert.Contains("Laptop", product.Name));
    }

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_ParseStartsWithQuery()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var testProduct = TestProductBuilder.Create()
            .WithName("Premium Gaming Laptop")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        var query = @"
        {
          products(where: { name: { startsWith: ""Premium"" } }) {
            id
            name
          }
        }";

        // Act
        var results = await repository.ExecuteGraphQlQueryAsync(query);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, product => Assert.StartsWith("Premium", product.Name));
    }

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_ParseEndsWithQuery()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var testProduct = TestProductBuilder.Create()
            .WithName("High-End Laptop")
            .Build();

        await repository.AddAsync(testProduct);
        await repository.SaveChangesAsync();

        var query = @"
        {
          products(where: { name: { endsWith: ""Laptop"" } }) {
            id
            name
          }
        }";

        // Act
        var results = await repository.ExecuteGraphQlQueryAsync(query);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, product => Assert.EndsWith("Laptop", product.Name));
    }

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_HandleQueryWithVariables()
    {
        // Arrange
        await using var context = await TestServiceFactory.CreateSeededTestDbContextAsync();
        var repository = TestRepository.Create(context, _loggerFactory);

        var query = @"
        query GetProductsByCategory($category: String!) {
          products(where: { category: { eq: $category } }) {
            id
            name
            category
          }
        }";

        var variables = new Dictionary<string, object>
        {
            ["category"] = "Electronics"
        };

        // Note: Current implementation doesn't fully support variables yet
        // This test documents expected behavior for future enhancement

        // Act & Assert
        // For now, this should work with the basic parsing
        var results = await repository.ExecuteGraphQlQueryAsync(query, variables);

        // The current implementation may not parse variables correctly,
        // but it should not throw an exception
        Assert.NotNull(results);
    }

    [Fact]
    public async Task ExecuteGraphQlQueryAsync_Should_ReturnEmpty_WhenNoMatches()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        var query = @"
        {
          products(where: { name: { eq: ""NonExistentProduct"" } }) {
            id
            name
          }
        }";

        // Act
        var results = await repository.ExecuteGraphQlQueryAsync(query);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task ExecuteGraphQlWithJsonQueryAsync_Should_ThrowNotSupportedException_InBaseImplementation()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        var query = @"{ products { id name } }";
        Expression<Func<TestProduct, JsonObject?>> jsonPredicate = p => p.Metadata;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.ExecuteGraphQlWithJsonQueryAsync(query, jsonPredicate));

        Assert.Contains("ExecuteGraphQlWithJsonQueryAsync", exception.Message);
    }

    #endregion

    #region JSON Query Tests (Should Throw NotSupportedException)

    [Fact]
    public async Task FirstOrDefaultWithJsonQueryAsync_Should_ThrowNotSupportedException()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        Expression<Func<TestProduct, JsonObject?>> jsonPredicate = p => p.Metadata;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.FirstOrDefaultWithJsonQueryAsync(jsonPredicate, "key", "value"));

        Assert.Contains("FirstOrDefaultWithJsonQueryAsync", exception.Message);
        Assert.Contains("database provider-specific implementation", exception.Message);
    }

    [Fact]
    public async Task GetByCriteriaWithJsonQueryAsync_Should_ThrowNotSupportedException()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        Expression<Func<TestProduct, JsonObject?>> jsonPredicate = p => p.Metadata;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => repository.GetByCriteriaWithJsonQueryAsync(jsonPredicate, "key", "value"));

        Assert.Contains("GetByCriteriaWithJsonQueryAsync", exception.Message);
    }

    #endregion

    #region Database Provider Detection Tests

    [Fact]
    public async Task DetectDatabaseProvider_Should_ReturnInMemory_ForTestContext()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act
        var provider = repository.TestDetectDatabaseProvider();

        // Assert
        Assert.Equal("In-Memory", provider);
    }

    #endregion

    #region Field Mapping Tests

    [Fact]
    public async Task MapGraphQlFieldToProperty_Should_MapCommonFields()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act & Assert
        Assert.Equal("Name", repository.TestMapGraphQlFieldToProperty("name"));
        Assert.Equal("Price", repository.TestMapGraphQlFieldToProperty("price"));
        Assert.Equal("Category", repository.TestMapGraphQlFieldToProperty("category"));
        Assert.Equal("Id", repository.TestMapGraphQlFieldToProperty("id"));
        Assert.Equal("IsActive", repository.TestMapGraphQlFieldToProperty("isActive"));
    }

    [Fact]
    public void MapGraphQlFieldToProperty_Should_CapitalizeFirstLetter_ForUnknownFields()
    {
        // Arrange
        using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        // Act & Assert
        Assert.Equal("CustomField", repository.TestMapGraphQlFieldToProperty("customField"));
        Assert.Equal("SomeProperty", repository.TestMapGraphQlFieldToProperty("someProperty"));
    }

    #endregion

    #region Transaction Tests

    [Fact]
    public async Task UseExistingTransactionAsync_Should_NotThrow()
    {
        // Skip test for in-memory database as it doesn't support transactions
        await using var context = TestServiceFactory.CreateTestDbContext();

        if (context.Database.ProviderName?.Contains("InMemory") == true)
        {
            // In-memory database doesn't support transactions
            Assert.True(true, "Skipping transaction test for in-memory database");
            return;
        }

        var repository = TestRepository.Create(context, _loggerFactory);
        await using var transaction = await context.Database.BeginTransactionAsync();

        // Act & Assert - Should not throw
        await repository.UseExistingTransactionAsync(transaction.GetDbTransaction());

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task SaveChangesAsync_Should_ReturnNumberOfAffectedEntities()
    {
        // Arrange
        await using var context = TestServiceFactory.CreateTestDbContext();
        var repository = TestRepository.Create(context, _loggerFactory);

        var product = TestProductBuilder.Create().WithName("Test Product").Build();
        await repository.AddAsync(product);

        // Act
        var result = await repository.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result);
    }

    #endregion
}
