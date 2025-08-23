# Enhanced Repository with JSON Query and GraphQL Support

The Idevs.Foundation library now includes enhanced repository capabilities with comprehensive JSON query support and GraphQL integration across major database providers.

## Overview

The enhanced repository system provides:

- **Database-agnostic JSON querying** with automatic provider detection
- **GraphQL query support** with automatic translation to database-specific operations
- **Optimized implementations** for PostgreSQL, SQL Server, MySQL, and SQLite
- **Backward compatibility** with existing repository implementations
- **Advanced JSON path operations** and aggregation support

## Supported Database Providers

| Database | JSON Functions | GraphQL Support | Advanced Features |
|----------|----------------|-----------------|-------------------|
| PostgreSQL | ✅ JSONB operators (`#>>`, `@>`) | ✅ Full support | ✅ Full-text search, aggregation |
| SQL Server | ✅ JSON_VALUE, JSON_QUERY | ✅ Full support | ✅ Full-text search, aggregation |
| MySQL | ✅ JSON_EXTRACT, JSON_SEARCH | ✅ Full support | ✅ JSON path queries, aggregation |
| SQLite | ✅ JSON_EXTRACT functions | ✅ Basic support | ✅ JSON path queries |
| In-Memory | ⚠️ Fallback to string contains | ⚠️ Limited support | ❌ Testing only |

## Getting Started

### 1. Basic Usage

Inherit from `EnhancedRepositoryBase` instead of `RepositoryBase`:

```csharp
public class ProductRepository : EnhancedRepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext dbContext, ILogger<RepositoryBase<Product, int>> logger)
        : base(dbContext, logger)
    {
    }
}
```

### 2. Entity Setup

Ensure your entities have JSON properties:

```csharp
public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public JsonObject? Metadata { get; set; } // JSON column for flexible data
}
```

### 3. DbContext Configuration

Configure JSON columns properly in your DbContext:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>(entity =>
    {
        // For PostgreSQL - use JSONB
        entity.Property(e => e.Metadata)
              .HasColumnType("jsonb");
              
        // For SQL Server - use NVARCHAR(MAX)
        entity.Property(e => e.Metadata)
              .HasColumnType("nvarchar(max)");
              
        // For MySQL - use JSON
        entity.Property(e => e.Metadata)
              .HasColumnType("json");
              
        // For SQLite - use TEXT
        entity.Property(e => e.Metadata)
              .HasColumnType("text");
    });
}
```

## JSON Query Examples

### Basic JSON Querying

```csharp
// Find products with specific metadata
var products = await repository.GetByCriteriaWithJsonQueryAsync(
    p => p.Metadata,
    "category",
    "electronics"
);

// Find first product matching criteria
var product = await repository.FirstOrDefaultWithJsonQueryAsync(
    p => p.Metadata,
    "featured",
    "true"
);
```

### Advanced JSON Path Queries

```csharp
// Query nested JSON paths
var premiumProducts = await repository.ExecuteJsonPathQueryAsync(
    p => p.Metadata,
    "pricing.tier",
    "equals",
    "premium"
);

// Check for existence of JSON paths
var productsWithRatings = await repository.ExecuteJsonPathQueryAsync(
    p => p.Metadata,
    "reviews.rating",
    "exists"
);

// Complex nested queries
var technologyBooks = await repository.ExecuteJsonPathQueryAsync(
    p => p.Metadata,
    "categories[0].name",
    "contains",
    "Technology"
);
```

## GraphQL Integration

### Simple GraphQL Queries

```csharp
// Basic GraphQL query
var query = @"
{
  products(where: { name: { contains: ""laptop"" } }) {
    id
    name
    price
  }
}";

var results = await repository.ExecuteGraphQLQueryAsync(query);
```

### Advanced GraphQL with Variables

```csharp
var query = @"
query GetProductsByCategory($category: String!) {
  products(where: { 
    metadata: { 
      json(path: ""category"", value: $category) 
    } 
  }) {
    id
    name
    metadata
  }
}";

var variables = new Dictionary<string, object>
{
    ["category"] = "electronics"
};

var results = await repository.ExecuteGraphQLQueryAsync(query, variables);
```

### GraphQL with JSON Filtering

```csharp
var query = @"
{
  products(where: { 
    price: { gt: 100 },
    metadata: { 
      json(path: ""featured"", value: ""true"")
    }
  }) {
    id
    name
    price
    metadata
  }
}";

var results = await repository.ExecuteGraphQLWithJsonQueryAsync(
    query,
    p => p.Metadata
);
```

## Database-Specific Optimizations

The enhanced repository automatically detects your database provider and uses optimized query strategies:

### PostgreSQL Optimizations

```csharp
// Uses JSONB containment operators for efficient querying
// Automatically leverages GIN indexes on JSONB columns
// Supports full-text search within JSON content

// Example: Efficient JSONB query
// Translates to: WHERE metadata @> '{\"category\": \"electronics\"}'
var products = await repository.GetByCriteriaWithJsonQueryAsync(
    p => p.Metadata,
    "category", 
    "electronics"
);
```

### SQL Server Optimizations

```csharp
// Uses JSON_VALUE and JSON_QUERY functions
// Supports computed columns for indexing JSON paths
// Integrates with full-text search capabilities

// Example: Optimized JSON_VALUE query
// Translates to: WHERE JSON_VALUE(metadata, '$.category') = 'electronics'
```

### MySQL Optimizations

```csharp
// Uses JSON_EXTRACT and JSON_CONTAINS functions
// Supports generated columns for JSON path indexing
// Leverages MySQL's JSON data type features

// Example: Efficient JSON_EXTRACT query
// Translates to: WHERE JSON_UNQUOTE(JSON_EXTRACT(metadata, '$.category')) = 'electronics'
```

### SQLite Optimizations

```csharp
// Uses SQLite's JSON1 extension functions
// Supports JSON_EXTRACT for path-based queries
// Falls back to string operations when needed

// Example: JSON_EXTRACT query
// Translates to: WHERE JSON_EXTRACT(metadata, '$.category') = 'electronics'
```

## Performance Considerations

### Indexing Strategies

#### PostgreSQL
```sql
-- Create GIN index for efficient JSONB queries
CREATE INDEX idx_product_metadata_gin ON products USING GIN (metadata);

-- Create partial index for specific JSON keys
CREATE INDEX idx_product_category ON products USING BTREE ((metadata->>'category'));
```

#### SQL Server
```sql
-- Create computed column and index
ALTER TABLE products 
ADD category AS JSON_VALUE(metadata, '$.category');

CREATE INDEX idx_product_category ON products (category);
```

#### MySQL
```sql
-- Create generated column and index
ALTER TABLE products 
ADD COLUMN category VARCHAR(100) AS (JSON_UNQUOTE(JSON_EXTRACT(metadata, '$.category'))) STORED;

CREATE INDEX idx_product_category ON products (category);
```

### Best Practices

1. **Use appropriate indexes** for frequently queried JSON paths
2. **Consider denormalization** for critical query paths
3. **Profile queries** specific to your database provider
4. **Use computed/generated columns** for complex JSON expressions
5. **Implement caching** for expensive JSON aggregations

## Error Handling

The enhanced repository provides detailed error information:

```csharp
try 
{
    var results = await repository.ExecuteJsonPathQueryAsync(
        p => p.Metadata,
        "invalid.path",
        "equals",
        "value"
    );
}
catch (NotSupportedException ex)
{
    // Provides specific guidance for unsupported operations
    Console.WriteLine(ex.Message);
    // Includes database provider information and implementation suggestions
}
```

## Testing with In-Memory Database

For unit testing, the enhanced repository gracefully falls back to string-based operations:

```csharp
[Test]
public async Task Should_Query_JSON_With_InMemory_Database()
{
    // Uses Entity Framework In-Memory provider
    var options = new DbContextOptionsBuilder<TestDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;
        
    using var context = new TestDbContext(options);
    var repository = new ProductRepository(context, logger);
    
    // JSON queries work with simplified string matching
    var results = await repository.GetByCriteriaWithJsonQueryAsync(
        p => p.Metadata,
        "category",
        "electronics"
    );
    
    Assert.NotNull(results);
}
```

## Migration Guide

### From Basic Repository

1. **Change inheritance**: Replace `RepositoryBase` with `EnhancedRepositoryBase`
2. **Update DbContext**: Add proper JSON column configurations
3. **Add indexes**: Create appropriate indexes for your JSON query patterns
4. **Update tests**: Ensure tests account for database-specific behaviors

### Backward Compatibility

- All existing repository methods continue to work unchanged
- JSON query methods gracefully fall back to the original implementations if not overridden
- GraphQL methods throw informative exceptions with implementation guidance

## Advanced Features

### Aggregation Support

```csharp
// Count products in a category (from JSON)
var count = await repository.AggregateJsonFieldAsync<int>(
    p => p.Metadata,
    "category",
    "COUNT"
);

// Average rating from JSON metadata
var avgRating = await repository.AggregateJsonFieldAsync<decimal>(
    p => p.Metadata,
    "reviews.rating",
    "AVG"
);
```

### Full-Text Search in JSON

```csharp
// Search across all JSON content
var searchResults = await repository.FullTextSearchInJsonAsync(
    p => p.Metadata,
    "wireless technology"
);

// Search specific JSON fields
var specificSearch = await repository.FullTextSearchInJsonAsync(
    p => p.Metadata,
    "bluetooth",
    new[] { "features", "description" }
);
```

## Troubleshooting

### Common Issues

1. **Provider not detected**: Ensure your connection string matches expected provider patterns
2. **JSON functions not available**: Verify database version supports JSON operations
3. **Performance issues**: Check indexing strategy for your JSON query patterns
4. **Type conversion errors**: Ensure JSON values match expected .NET types

### Debugging

Enable detailed logging to see generated SQL queries:

```csharp
services.AddDbContext<MyDbContext>(options =>
{
    options.UseNpgsql(connectionString)
           .EnableSensitiveDataLogging()
           .LogTo(Console.WriteLine, LogLevel.Information);
});
```

## Contributing

To add support for additional database providers:

1. Implement database-specific JSON query methods
2. Add provider detection logic
3. Create comprehensive tests
4. Update documentation with provider-specific examples

## See Also

- [Repository Pattern Documentation](./Repository-Pattern.md)
- [Entity Framework Configuration](./EntityFramework-Setup.md)
- [GraphQL Best Practices](./GraphQL-Integration.md)
- [Performance Optimization](./Performance-Tuning.md)