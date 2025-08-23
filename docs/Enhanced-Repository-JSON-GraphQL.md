# Repository with JSON Query and GraphQL Support

The Idevs.Foundation library includes comprehensive JSON query support and GraphQL integration across major database providers. **GraphQL support is now available in all repositories** that inherit from `RepositoryBase`, while enhanced JSON optimizations are available through `EnhancedRepositoryBase`.

## Overview

The repository system provides:

- **GraphQL query support** (available in all repositories via `RepositoryBase`)
- **Database-agnostic JSON querying** with automatic provider detection
- **Enhanced JSON optimizations** for specific database providers via `EnhancedRepositoryBase`
- **Optimized implementations** for PostgreSQL, SQL Server, MySQL, and SQLite
- **Backward compatibility** with existing repository implementations
- **Advanced JSON path operations** and aggregation support

## Supported Database Providers

| Database | JSON Functions | GraphQL Support | Advanced Features |
|----------|----------------|-----------------|-------------------|
| PostgreSQL | ✅ JSONB operators (`#>>`, `@>`) | ✅ Full support | ✅ Full-text search, aggregation |
| SQL Server | ✅ JSON_VALUE, JSON_QUERY | ✅ Full support | ✅ Full-text search, aggregation |
| MySQL | ✅ JSON_EXTRACT, JSON_SEARCH | ✅ Full support | ✅ JSON path queries, aggregation |
| SQLite | ✅ JSON_EXTRACT functions | ✅ Full support | ✅ JSON path queries |
| In-Memory | ⚠️ Fallback to string contains | ✅ Basic support | ❌ Testing only |

## Getting Started

### 1. Basic Repository with GraphQL Support

**All repositories now have GraphQL support by default** when inheriting from `RepositoryBase`:

```csharp
public class ProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext dbContext, ILogger<RepositoryBase<Product, int>> logger)
        : base(dbContext, logger)
    {
    }
    
    // GraphQL methods are now available automatically:
    // - ExecuteGraphQlQueryAsync()
    // - ExecuteGraphQlWithJsonQueryAsync()
}
```

### 2. Enhanced Repository for Optimized JSON Queries

For **database-specific JSON optimizations**, inherit from `EnhancedRepositoryBase`:

```csharp
public class ProductRepository : EnhancedRepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext dbContext, ILogger<RepositoryBase<Product, int>> logger)
        : base(dbContext, logger)
    {
    }
    
    // Includes both GraphQL support AND optimized JSON operations:
    // - Automatic database provider detection
    // - Provider-specific JSON query optimizations
    // - Advanced JSON path operations
}
```

### 3. Entity Setup

Ensure your entities have JSON properties:

```csharp
public class Product : SoftDeletableEntity<int>
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public JsonObject? Metadata { get; set; } // JSON column for flexible data
}
```

### 4. DbContext Configuration

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

## GraphQL Integration (Available in All Repositories)

GraphQL support is now built into the base `RepositoryBase` class and available to all repositories:

### Simple GraphQL Queries

```csharp
// Basic GraphQL query - works with any repository
var query = @"
{
  products(where: { name: { contains: ""laptop"" } }) {
    id
    name
    price
  }
}";

var results = await repository.ExecuteGraphQlQueryAsync(query);
```

### GraphQL with Variables

```csharp
var query = @"
query GetProductsByCategory($category: String!) {
  products(where: { 
    name: { contains: $category }
  }) {
    id
    name
    price
  }
}";

var variables = new Dictionary<string, object>
{
    ["category"] = "electronics"
};

var results = await repository.ExecuteGraphQlQueryAsync(query, variables);
```

### Supported GraphQL Operators

The base GraphQL implementation supports:

- `eq` - Equality comparison
- `contains` - String contains
- `startsWith` - String starts with
- `endsWith` - String ends with

### GraphQL Field Mapping

GraphQL fields are automatically mapped to entity properties:

```csharp
// GraphQL field -> Entity Property
"name" -> "Name"
"price" -> "Price"
"category" -> "Category"
"id" -> "Id"
"isActive" -> "IsActive"
// Other fields: First letter capitalized
```

## JSON Query Examples (Enhanced Repository Only)

### Basic JSON Querying

```csharp
// Find products with specific metadata (requires EnhancedRepositoryBase)
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
// Query nested JSON paths (requires EnhancedRepositoryBase)
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
```

## Architecture Changes

### Version 1.1.0+ Changes

- **GraphQL support moved to `RepositoryBase`**: All repositories now have GraphQL capabilities
- **Enhanced JSON optimizations remain in `EnhancedRepositoryBase`**: Database-specific JSON optimizations still require the enhanced repository
- **Backward compatibility maintained**: Existing code continues to work without changes
- **Simplified architecture**: GraphQL logic is now centralized in the base class

### Migration Guide

**No code changes required** for existing implementations:

- Repositories inheriting from `RepositoryBase` now automatically get GraphQL support
- Repositories inheriting from `EnhancedRepositoryBase` continue to work as before
- All existing JSON query methods remain available in `EnhancedRepositoryBase`

### When to Use Which Repository

| Repository Type | Use When | Features |
|-----------------|----------|----------|
| `RepositoryBase` | Basic CRUD + GraphQL | Standard EF operations + GraphQL queries |
| `EnhancedRepositoryBase` | Complex JSON queries + GraphQL | All base features + optimized JSON operations |

## Database-Specific Optimizations (Enhanced Repository)

The enhanced repository automatically detects your database provider and uses optimized query strategies:

### PostgreSQL Optimizations

- Uses JSONB operators for efficient JSON queries
- Leverages PostgreSQL's native JSON indexing
- Supports complex JSON path expressions

### SQL Server Optimizations

- Uses JSON_VALUE and JSON_QUERY functions
- Optimized for SQL Server's JSON implementation
- Supports full-text search on JSON content

### MySQL Optimizations

- Uses JSON_EXTRACT and JSON_SEARCH functions
- Leverages MySQL's native JSON type
- Supports JSON path queries with wildcards

### SQLite Optimizations

- Uses JSON_EXTRACT functions
- Fallback implementation for lightweight scenarios
- Basic JSON path support

## Performance Considerations

- **Base GraphQL**: Simple parsing and filtering, suitable for basic queries
- **Enhanced JSON**: Database-optimized queries for complex JSON operations
- **Provider Detection**: Automatic optimization based on detected database provider
- **Fallback Support**: Graceful degradation for unsupported scenarios

## Future Enhancements

- Advanced GraphQL schema support
- Custom GraphQL resolvers
- Enhanced JSON aggregation functions
- Additional database provider support
