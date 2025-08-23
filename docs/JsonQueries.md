# JSON Query Implementation Guide

The Idevs.Foundation framework provides JSON query abstractions that require database provider-specific implementations. This guide shows how to implement JSON query functionality for different database providers.

## Overview

JSON query methods in `IRepositoryBase<T, TId>` allow you to query entities based on JSON column values. Since JSON support varies significantly between database providers, these methods require custom implementation in your repository classes.

## Why Implementation is Required

The base `RepositoryBase<T, TId>` class throws `NotSupportedException` for JSON query methods because:

- **Provider Differences**: Each database has unique JSON syntax and functions
- **Performance**: Provider-specific optimizations are crucial for JSON queries
- **Type Safety**: Implementation details vary between databases

## Supported Database Providers

### PostgreSQL (Recommended for JSON)

PostgreSQL offers the most robust JSON support with JSONB columns and operators.

#### Implementation Example

```csharp
public class PostgreSqlProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public PostgreSqlProductRepository(DbContext context, ILogger<PostgreSqlProductRepository> logger) 
        : base(context, logger) { }

    public override async Task<Product?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<Product, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        // Use PostgreSQL JSONB operators
        return await Query()
            .Where(p => EF.Functions.JsonExists(
                EF.Property<JsonObject>(p, jsonPredicate.GetPropertyName()), 
                key))
            .Where(p => EF.Functions.JsonValue(
                EF.Property<JsonObject>(p, jsonPredicate.GetPropertyName()), 
                $"$.{key}") == value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<Product>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<Product, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(p => EF.Functions.JsonValue(
                EF.Property<JsonObject>(p, jsonPredicate.GetPropertyName()), 
                $"$.{key}") == value)
            .ToListAsync(cancellationToken);
    }
}
```

#### Entity Configuration

```csharp
public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public JsonObject? Metadata { get; set; } // JSON column
}

// In DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        entity.Property(e => e.Metadata)
              .HasColumnType("jsonb"); // PostgreSQL JSONB
    });
}
```

### SQL Server

SQL Server provides JSON functions starting from SQL Server 2016.

#### Implementation Example

```csharp
public class SqlServerProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public SqlServerProductRepository(DbContext context, ILogger<SqlServerProductRepository> logger) 
        : base(context, logger) { }

    public override async Task<Product?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<Product, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(p => EF.Functions.JsonValue(
                EF.Property<string>(p, jsonPredicate.GetPropertyName()), 
                $"$.{key}") == value)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public override async Task<List<Product>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<Product, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        return await Query()
            .Where(p => EF.Functions.JsonValue(
                EF.Property<string>(p, jsonPredicate.GetPropertyName()), 
                $"$.{key}") == value)
            .ToListAsync(cancellationToken);
    }
}
```

#### Entity Configuration

```csharp
// In DbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        entity.Property(e => e.Metadata)
              .HasColumnType("nvarchar(max)"); // JSON stored as string
    });
}
```

### SQLite

SQLite has built-in JSON functions since version 3.38.0.

#### Implementation Example

```csharp
public class SqliteProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public SqliteProductRepository(DbContext context, ILogger<SqliteProductRepository> logger) 
        : base(context, logger) { }

    public override async Task<Product?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<Product, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        // Use SQLite JSON_EXTRACT function
        return await Query()
            .FromSqlRaw(@"
                SELECT * FROM Products 
                WHERE JSON_EXTRACT(Metadata, '$.{0}') = {1}
                LIMIT 1", key, value)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
```

## Advanced Implementation Patterns

### Generic Provider Factory

Create a factory to select the appropriate repository implementation:

```csharp
public interface IRepositoryFactory
{
    IRepositoryBase<T, TId> CreateRepository<T, TId>() 
        where T : class, IHasId<TId>;
}

public class RepositoryFactory : IRepositoryFactory
{
    private readonly DbContext _context;
    private readonly IServiceProvider _serviceProvider;

    public RepositoryFactory(DbContext context, IServiceProvider serviceProvider)
    {
        _context = context;
        _serviceProvider = serviceProvider;
    }

    public IRepositoryBase<T, TId> CreateRepository<T, TId>() 
        where T : class, IHasId<TId>
    {
        var providerName = _context.Database.ProviderName?.ToLowerInvariant();
        
        return providerName switch
        {
            string p when p.Contains("npgsql") => 
                _serviceProvider.GetRequiredService<PostgreSqlRepository<T, TId>>(),
            string p when p.Contains("sqlserver") => 
                _serviceProvider.GetRequiredService<SqlServerRepository<T, TId>>(),
            string p when p.Contains("sqlite") => 
                _serviceProvider.GetRequiredService<SqliteRepository<T, TId>>(),
            _ => throw new NotSupportedException($"JSON queries not supported for provider: {providerName}")
        };
    }
}
```

### Extension Method Helpers

Create extension methods to simplify JSON property access:

```csharp
public static class ExpressionExtensions
{
    public static string GetPropertyName<T>(this Expression<Func<T, JsonObject?>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        
        throw new ArgumentException("Expression must be a property access", nameof(expression));
    }
}
```

## Testing JSON Queries

### Unit Testing

```csharp
[Fact]
public async Task FirstOrDefaultWithJsonQueryAsync_WithPostgreSql_ReturnsCorrectEntity()
{
    // Arrange
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
    
    using var context = new TestDbContext(options);
    var logger = Mock.Of<ILogger<PostgreSqlProductRepository>>();
    var repository = new PostgreSqlProductRepository(context, logger);

    var product = new Product
    {
        Name = "Test Product",
        Metadata = JsonObject.Parse("""{"category": "electronics", "color": "blue"}""")
    };
    
    await repository.AddAsync(product);
    await repository.SaveChangesAsync();

    // Act
    var result = await repository.FirstOrDefaultWithJsonQueryAsync(
        p => p.Metadata,
        "category",
        "electronics");

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test Product", result.Name);
}
```

### Integration Testing

```csharp
[Fact]
public async Task JsonQuery_IntegrationTest_WithRealDatabase()
{
    // Use TestContainers for real database testing
    var container = new PostgreSqlBuilder()
        .WithDatabase("testdb")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    await container.StartAsync();

    var connectionString = container.GetConnectionString();
    
    // Test with real PostgreSQL instance
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseNpgsql(connectionString)
        .Options;
    
    // ... test implementation
}
```

## Performance Considerations

### Indexing JSON Columns

#### PostgreSQL
```sql
-- Create GIN index for JSONB column
CREATE INDEX idx_product_metadata_gin ON products USING GIN (metadata);

-- Create specific path index
CREATE INDEX idx_product_category ON products USING GIN ((metadata->'category'));
```

#### SQL Server
```sql
-- Create computed column and index
ALTER TABLE Products ADD CategoryComputed AS JSON_VALUE(Metadata, '$.category');
CREATE INDEX idx_product_category ON Products (CategoryComputed);
```

### Query Optimization

```csharp
// Use compiled queries for frequently executed JSON queries
private static readonly Func<DbContext, string, IAsyncEnumerable<Product>> CompiledJsonQuery =
    EF.CompileAsyncQuery((DbContext context, string categoryValue) =>
        context.Set<Product>()
            .Where(p => EF.Functions.JsonValue(p.Metadata, "$.category") == categoryValue));

public async Task<List<Product>> GetProductsByCategoryOptimized(string category)
{
    var results = new List<Product>();
    await foreach (var product in CompiledJsonQuery(_dbContext, category))
    {
        results.Add(product);
    }
    return results;
}
```

## Error Handling

```csharp
public override async Task<Product?> FirstOrDefaultWithJsonQueryAsync(
    Expression<Func<Product, JsonObject?>> jsonPredicate,
    string key,
    string value,
    CancellationToken cancellationToken = default)
{
    try
    {
        return await Query()
            .Where(p => EF.Functions.JsonValue(
                EF.Property<JsonObject>(p, jsonPredicate.GetPropertyName()), 
                $"$.{key}") == value)
            .FirstOrDefaultAsync(cancellationToken);
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("JSON"))
    {
        _logger.LogError(ex, "JSON query failed for key {Key} and value {Value}", key, value);
        throw new InvalidOperationException(
            $"JSON query failed. Ensure the JSON column exists and contains valid JSON. Key: {key}, Value: {value}", 
            ex);
    }
}
```

## Migration Guide

### From Silent Failures

If you're upgrading from a version that returned empty results:

1. **Expect Exceptions**: JSON query methods now throw `NotSupportedException`
2. **Implement Provider-Specific Logic**: Follow the examples above
3. **Update Tests**: Replace assertions for empty results with exception handling
4. **Add Error Handling**: Implement try-catch blocks where JSON queries are used

### Gradual Implementation

You can implement JSON query methods incrementally:

```csharp
public override async Task<Product?> FirstOrDefaultWithJsonQueryAsync(
    Expression<Func<Product, JsonObject?>> jsonPredicate,
    string key,
    string value,
    CancellationToken cancellationToken = default)
{
    // Implement only the methods you need
    return await base.FirstOrDefaultWithJsonQueryAsync(jsonPredicate, key, value, cancellationToken);
}

public override async Task<List<Product>> GetByCriteriaWithJsonQueryAsync(
    Expression<Func<Product, JsonObject?>> jsonPredicate,
    string key,
    string value,
    CancellationToken cancellationToken = default)
{
    // This method remains unimplemented - will throw NotSupportedException
    throw new NotImplementedException("This specific JSON query method is not yet implemented for this repository.");
}
```

## Best Practices

1. **Provider Detection**: Always detect the database provider for appropriate implementation
2. **Index JSON Columns**: Create appropriate indexes for JSON query performance
3. **Validate JSON Structure**: Ensure JSON columns contain expected structure
4. **Error Handling**: Provide meaningful error messages for JSON parsing failures
5. **Testing**: Test with real database instances, not just in-memory providers
6. **Documentation**: Document expected JSON schema for your entities

## Troubleshooting

### Common Issues

1. **"Function not supported"**: Ensure your database provider version supports JSON functions
2. **Performance Issues**: Add appropriate indexes on JSON columns
3. **Type Conversion Errors**: Verify JSON structure matches expected schema
4. **Provider Not Detected**: Check that the correct Entity Framework provider package is installed

### Debug Information

The framework logs detailed information when JSON query methods are called:

```
[Error] JSON query operation 'FirstOrDefaultWithJsonQueryAsync' was called but not implemented. 
Database provider detected: PostgreSQL. Repository type: ProductRepository
```

This information helps identify which methods need implementation and for which database provider.