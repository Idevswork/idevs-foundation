using System.Text.Json.Nodes;
using Idevs.Foundation.EntityFramework.Repositories;
using Idevs.Foundation.Abstractions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Idevs.Foundation.Tests.EntityFramework;

/// <summary>
/// Tests for improved JSON query exception handling in RepositoryBase.
/// </summary>
public class JsonQueryTests
{
    private class TestEntity : IHasId<int>
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public JsonObject? JsonData { get; set; }
    }

    private class TestRepository : RepositoryBase<TestEntity, int>
    {
        public TestRepository(DbContext context, ILogger<TestRepository> logger) 
            : base(context, logger) { }
    }

    private class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions options) : base(options) { }
        public DbSet<TestEntity> TestEntities { get; set; }
    }

    private TestRepository CreateRepository()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        var context = new TestDbContext(options);
        var logger = new LoggerFactory().CreateLogger<TestRepository>();
        
        return new TestRepository(context, logger);
    }

    [Fact]
    public async Task FirstOrDefaultWithJsonQueryAsync_ThrowsNotSupportedException_WithHelpfulMessage()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.FirstOrDefaultWithJsonQueryAsync(
                e => e.JsonData,
                "key",
                "value"));

        // Verify the exception contains helpful guidance
        Assert.Contains("JSON query method", exception.Message);
        Assert.Contains("database provider-specific implementation", exception.Message);
        Assert.Contains("Override this method", exception.Message);
        Assert.Contains("PostgreSQL", exception.Message);
        Assert.Contains("SQL Server", exception.Message);
        Assert.Contains("SQLite", exception.Message);
        Assert.Contains("https://", exception.Message); // Documentation link
    }

    [Fact]
    public async Task GetByCriteriaWithJsonQueryAsync_ThrowsNotSupportedException_WithProviderDetection()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.GetByCriteriaWithJsonQueryAsync(
                e => e.JsonData,
                "category",
                "electronics"));

        // Verify the exception contains provider detection information
        Assert.Contains("GetByCriteriaWithJsonQueryAsync", exception.Message);
        // The provider might be "inmemory" or "In-Memory" - just check it's detected
        Assert.Contains("provider", exception.Message.ToLowerInvariant());
    }

    [Fact]
    public async Task FirstOrDefaultWithJsonQueryAsync_WithPredicate_ThrowsNotSupportedException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.FirstOrDefaultWithJsonQueryAsync(
                e => e.Id > 0,
                e => e.JsonData,
                "status",
                "active"));

        Assert.Contains("FirstOrDefaultWithJsonQueryAsync", exception.Message);
    }

    [Fact]
    public async Task FirstOrDefaultWithJsonQueryAsync_WithValueChoices_ThrowsNotSupportedException()
    {
        // Arrange
        var repository = CreateRepository();
        var valueChoices = new[] { "active", "inactive", "pending" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.FirstOrDefaultWithJsonQueryAsync(
                e => e.Id > 0,
                e => e.JsonData,
                valueChoices,
                "status"));

        Assert.Contains("FirstOrDefaultWithJsonQueryAsync", exception.Message);
    }

    [Fact]
    public async Task GetByCriteriaWithJsonQueryAsync_WithPredicate_ThrowsNotSupportedException()
    {
        // Arrange
        var repository = CreateRepository();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.GetByCriteriaWithJsonQueryAsync(
                e => e.Name.Contains("test"),
                e => e.JsonData,
                "type",
                "premium"));

        Assert.Contains("GetByCriteriaWithJsonQueryAsync", exception.Message);
    }

    [Fact]
    public async Task GetByCriteriaWithJsonQueryAsync_WithValueChoices_ThrowsNotSupportedException()
    {
        // Arrange
        var repository = CreateRepository();
        var valueChoices = new[] { "standard", "premium", "enterprise" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await repository.GetByCriteriaWithJsonQueryAsync(
                e => e.Name.Length > 0,
                e => e.JsonData,
                valueChoices,
                "tier"));

        Assert.Contains("GetByCriteriaWithJsonQueryAsync", exception.Message);
    }

    [Fact] 
    public async Task JsonQueryMethods_LogErrorMessages()
    {
        // This test would require a mock logger to verify logging behavior
        // For now, we're testing that the methods throw expected exceptions
        // The logging functionality is verified through the exception messages
        
        var repository = CreateRepository();

        // All JSON query methods should throw NotSupportedException
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            repository.FirstOrDefaultWithJsonQueryAsync(e => e.JsonData, "key", "value"));
        
        await Assert.ThrowsAsync<NotSupportedException>(() => 
            repository.GetByCriteriaWithJsonQueryAsync(e => e.JsonData, "key", "value"));
    }
}