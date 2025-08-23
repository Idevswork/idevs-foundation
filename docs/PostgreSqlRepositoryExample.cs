using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Idevs.Foundation.EntityFramework.Repositories;
using Idevs.Foundation.Abstractions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Examples.PostgreSql;

/// <summary>
/// Example implementation of a PostgreSQL-specific repository with JSON query support.
/// This demonstrates how to properly implement JSON query methods for PostgreSQL databases.
/// </summary>
/// <typeparam name="T">The entity type that implements IHasId</typeparam>
/// <typeparam name="TId">The identifier type</typeparam>
public class PostgreSqlRepository<T, TId> : RepositoryBase<T, TId>
    where T : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    public PostgreSqlRepository(DbContext dbContext, ILogger<PostgreSqlRepository<T, TId>> logger)
        : base(dbContext, logger)
    {
    }

    /// <inheritdoc />
    public override async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            // Use PostgreSQL JSONB operators for efficient querying
            return await QueryNoTracking()
                .FromSqlRaw($@"
                    SELECT * FROM ""{typeof(T).Name}s"" 
                    WHERE ""{propertyName}"" @> '{{\""{key}\"": \""{value}\""}}' 
                    LIMIT 1")
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL JSON query failed for key {Key} and value {Value}", key, value);
            throw new InvalidOperationException(
                $"JSON query failed for PostgreSQL. Key: {key}, Value: {value}. " +
                $"Ensure the column exists and contains valid JSONB data.", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            return await Query()
                .Where(predicate)
                .Where(entity => EF.Functions.JsonExists(
                    EF.Property<JsonObject>(entity, propertyName), key))
                .Where(entity => EF.Functions.JsonValue(
                    EF.Property<JsonObject>(entity, propertyName), $"$.{key}") == value)
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL combined JSON query failed for key {Key} and value {Value}", key, value);
            throw new InvalidOperationException(
                $"Combined JSON query failed for PostgreSQL. Key: {key}, Value: {value}.", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            return await Query()
                .Where(predicate)
                .Where(entity => valueChoices.Contains(
                    EF.Functions.JsonValue(EF.Property<JsonObject>(entity, propertyName), $"$.{key}")))
                .FirstOrDefaultAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL JSON query with multiple values failed for key {Key}", key);
            throw new InvalidOperationException(
                $"JSON query with multiple values failed for PostgreSQL. Key: {key}.", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            // Use PostgreSQL JSONB containment operator for better performance
            return await QueryNoTracking()
                .FromSqlRaw($@"
                    SELECT * FROM ""{typeof(T).Name}s"" 
                    WHERE ""{propertyName}"" @> '{{\""{key}\"": \""{value}\""}}'")
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL JSON list query failed for key {Key} and value {Value}", key, value);
            throw new InvalidOperationException(
                $"JSON list query failed for PostgreSQL. Key: {key}, Value: {value}.", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            return await Query()
                .Where(predicate)
                .Where(entity => EF.Functions.JsonValue(
                    EF.Property<JsonObject>(entity, propertyName), $"$.{key}") == value)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL combined JSON list query failed for key {Key} and value {Value}", key, value);
            throw new InvalidOperationException(
                $"Combined JSON list query failed for PostgreSQL. Key: {key}, Value: {value}.", ex);
        }
    }

    /// <inheritdoc />
    public override async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var propertyName = GetJsonPropertyName(jsonPredicate);
            
            return await Query()
                .Where(predicate)
                .Where(entity => valueChoices.Contains(
                    EF.Functions.JsonValue(EF.Property<JsonObject>(entity, propertyName), $"$.{key}")))
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "PostgreSQL JSON list query with multiple values failed for key {Key}", key);
            throw new InvalidOperationException(
                $"JSON list query with multiple values failed for PostgreSQL. Key: {key}.", ex);
        }
    }

    /// <summary>
    /// Extracts the property name from a JSON property expression.
    /// </summary>
    /// <param name="jsonPredicate">The expression pointing to the JSON property.</param>
    /// <returns>The property name as a string.</returns>
    /// <exception cref="ArgumentException">Thrown when the expression is not a valid property access.</exception>
    private static string GetJsonPropertyName(Expression<Func<T, JsonObject?>> jsonPredicate)
    {
        if (jsonPredicate.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        throw new ArgumentException(
            "Expression must be a property access (e.g., p => p.JsonProperty)", 
            nameof(jsonPredicate));
    }
}

/// <summary>
/// Example entity demonstrating JSON column usage with PostgreSQL.
/// </summary>
public class ExampleProduct : IHasId<int>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    /// <summary>
    /// JSON/JSONB column containing flexible metadata.
    /// In PostgreSQL, this should be configured as JSONB for optimal performance.
    /// </summary>
    public JsonObject? Metadata { get; set; }
}

/// <summary>
/// Example DbContext configuration for PostgreSQL with JSON support.
/// </summary>
public class ExampleDbContext : DbContext
{
    public ExampleDbContext(DbContextOptions<ExampleDbContext> options) : base(options) { }

    public DbSet<ExampleProduct> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ExampleProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Price)
                  .HasPrecision(18, 2);

            // Configure JSON column as JSONB for PostgreSQL
            entity.Property(e => e.Metadata)
                  .HasColumnType("jsonb")
                  .IsRequired(false);
        });

        // Create indexes for better JSON query performance
        modelBuilder.Entity<ExampleProduct>()
            .HasIndex(e => e.Metadata)
            .HasDatabaseName("IX_Products_Metadata_Gin")
            .HasMethod("gin"); // PostgreSQL GIN index for JSONB
    }
}

/// <summary>
/// Example usage of the PostgreSQL repository with JSON queries.
/// </summary>
public class ExampleUsage
{
    private readonly PostgreSqlRepository<ExampleProduct, int> _repository;

    public ExampleUsage(PostgreSqlRepository<ExampleProduct, int> repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Example: Find products by category stored in JSON metadata.
    /// </summary>
    public async Task<ExampleProduct?> FindProductByCategoryAsync(string category)
    {
        return await _repository.FirstOrDefaultWithJsonQueryAsync(
            p => p.Metadata,
            "category",
            category);
    }

    /// <summary>
    /// Example: Find expensive products in electronics category.
    /// </summary>
    public async Task<List<ExampleProduct>> FindExpensiveElectronicsAsync()
    {
        return await _repository.GetByCriteriaWithJsonQueryAsync(
            p => p.Price > 1000m, // Regular predicate
            p => p.Metadata,      // JSON column
            "category",           // JSON key
            "electronics");       // JSON value
    }

    /// <summary>
    /// Example: Find products in multiple categories.
    /// </summary>
    public async Task<List<ExampleProduct>> FindProductsInCategoriesAsync(string[] categories)
    {
        return await _repository.GetByCriteriaWithJsonQueryAsync(
            p => p.Price > 0, // Basic filter
            p => p.Metadata,  // JSON column
            categories,       // Multiple values
            "category");      // JSON key
    }
}

/// <summary>
/// Example dependency injection configuration.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgreSqlRepositories(
        this IServiceCollection services, 
        string connectionString)
    {
        services.AddDbContext<ExampleDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<PostgreSqlRepository<ExampleProduct, int>>();
        
        return services;
    }
}