# GraphQL Query Support

The Idevs.Foundation library now provides built-in GraphQL query support in all repositories through the base `RepositoryBase` class. This feature allows you to execute GraphQL-style queries that are automatically translated to Entity Framework Core queries.

## Overview

GraphQL support is now available by default in all repositories that inherit from `RepositoryBase`. This means you get GraphQL functionality without needing to use the enhanced repository, making it accessible to all repository implementations.

## Key Features

- **Built into base class**: Available in all repositories via `RepositoryBase`
- **Automatic query parsing**: Simple GraphQL queries are parsed and converted to EF queries
- **Field mapping**: GraphQL field names are automatically mapped to entity properties
- **Variable support**: Query variables are supported for dynamic queries
- **Database agnostic**: Works with all supported database providers
- **Extensible**: Can be overridden for custom implementations

## Getting Started

### Basic Repository Setup

Any repository inheriting from `RepositoryBase` automatically has GraphQL support:

```csharp
public class ProductRepository : RepositoryBase<Product, int>, IProductRepository
{
    public ProductRepository(DbContext dbContext, ILogger<RepositoryBase<Product, int>> logger)
        : base(dbContext, logger)
    {
    }
    
    // GraphQL methods are available:
    // - ExecuteGraphQlQueryAsync()
    // - ExecuteGraphQlWithJsonQueryAsync()
}
```

### Using GraphQL Queries

```csharp
// Simple equality filter
var query = @"
{
  products(where: { name: { eq: ""Laptop"" } }) {
    id
    name
    price
  }
}";

var results = await repository.ExecuteGraphQlQueryAsync(query);
```

## Supported Query Syntax

### Basic Where Clauses

```csharp
// Equality
where: { name: { eq: "value" } }

// Contains
where: { name: { contains: "value" } }

// Starts with
where: { name: { startsWith: "value" } }

// Ends with
where: { name: { endsWith: "value" } }
```

### Query Examples

#### 1. Simple Equality Query

```csharp
var query = @"
{
  products(where: { category: { eq: ""Electronics"" } }) {
    id
    name
    price
  }
}";

var electronics = await repository.ExecuteGraphQlQueryAsync(query);
```

#### 2. String Contains Query

```csharp
var query = @"
{
  products(where: { name: { contains: ""laptop"" } }) {
    id
    name
    price
  }
}";

var laptops = await repository.ExecuteGraphQlQueryAsync(query);
```

#### 3. Query with Variables

```csharp
var query = @"
query GetProductsByName($searchTerm: String!) {
  products(where: { name: { contains: $searchTerm } }) {
    id
    name
    price
  }
}";

var variables = new Dictionary<string, object>
{
    ["searchTerm"] = "gaming"
};

var gamingProducts = await repository.ExecuteGraphQlQueryAsync(query, variables);
```

## Field Mapping

GraphQL field names are automatically mapped to entity property names using these rules:

| GraphQL Field | Entity Property | Notes |
|---------------|-----------------|-------|
| `name` | `Name` | Common mapping |
| `price` | `Price` | Common mapping |
| `category` | `Category` | Common mapping |
| `id` | `Id` | Common mapping |
| `isActive` | `IsActive` | Common mapping |
| `customField` | `CustomField` | First letter capitalized |

### Custom Field Mapping

You can extend the field mapping by overriding the `MapGraphQlFieldToProperty` method:

```csharp
public class CustomProductRepository : RepositoryBase<Product, int>
{
    // Override to add custom field mappings
    protected override string MapGraphQlFieldToProperty(string fieldName)
    {
        return fieldName switch
        {
            "productName" => "Name",
            "productPrice" => "Price",
            "isAvailable" => "InStock",
            _ => base.MapGraphQlFieldToProperty(fieldName)
        };
    }
}
```

## Advanced Usage

### JSON Field Integration

For repositories that inherit from `EnhancedRepositoryBase`, you can combine GraphQL with JSON field queries:

```csharp
var query = @"
{
  products(where: { price: { gt: 100 } }) {
    id
    name
    metadata
  }
}";

// This method is available in EnhancedRepositoryBase
var results = await repository.ExecuteGraphQlWithJsonQueryAsync(
    query,
    p => p.Metadata
);
```

### Error Handling

GraphQL queries that fail to parse or execute will throw informative exceptions:

```csharp
try
{
    var results = await repository.ExecuteGraphQlQueryAsync(invalidQuery);
}
catch (NotSupportedException ex)
{
    // Exception includes database provider information and helpful guidance
    logger.LogError(ex, "GraphQL query failed");
}
```

## Implementation Details

### Query Parsing

The GraphQL parser currently supports:

- Simple where clauses with nested conditions
- Basic operators: `eq`, `contains`, `startsWith`, `endsWith`
- Query variables (referenced with `$` syntax)
- Single-level field filtering

### Current Limitations

- **Simple parsing**: Uses regex-based parsing (not a full GraphQL parser)
- **Limited operators**: Supports basic string operations only
- **Single conditions**: Currently supports one where condition per query
- **No aggregations**: Count, sum, etc. are not yet supported
- **No sorting**: Order by clauses are not implemented

### Future Enhancements

Planned improvements include:

- Full GraphQL parser integration
- Additional operators (`gt`, `lt`, `gte`, `lte`, `in`, `not`)
- Multiple where conditions with AND/OR logic
- Sorting and pagination support
- Aggregation functions
- Custom resolver support
- Schema generation

## Performance Considerations

### Efficiency

- GraphQL queries are translated to standard EF Core queries
- No additional database round trips
- Uses `QueryNoTracking()` for read-only operations
- Automatic database provider optimization

### Best Practices

1. **Use specific fields**: Only query the fields you need
2. **Leverage variables**: Use variables for dynamic values
3. **Consider complexity**: Simple queries work best with current implementation
4. **Profile queries**: Monitor generated SQL for performance

## Migration and Compatibility

### Breaking Changes

- **None**: Adding GraphQL support to `RepositoryBase` is non-breaking
- All existing repositories automatically gain GraphQL functionality
- Existing `EnhancedRepositoryBase` implementations continue to work

### Upgrading

No code changes required:

```csharp
// Before - only had basic CRUD
public class UserRepository : RepositoryBase<User, int> { }

// After - automatically has GraphQL support
public class UserRepository : RepositoryBase<User, int> 
{
    // Can now use:
    // await ExecuteGraphQlQueryAsync(query);
}
```

## Examples by Use Case

### E-commerce Product Search

```csharp
// Search products by name
var searchQuery = @"
{
  products(where: { name: { contains: ""smartphone"" } }) {
    id
    name
    price
  }
}";

var smartphones = await productRepository.ExecuteGraphQlQueryAsync(searchQuery);
```

### User Management

```csharp
// Find active users
var activeUsersQuery = @"
{
  users(where: { isActive: { eq: ""true"" } }) {
    id
    name
    email
  }
}";

var activeUsers = await userRepository.ExecuteGraphQlQueryAsync(activeUsersQuery);
```

### Content Management

```csharp
// Find published articles
var publishedQuery = @"
{
  articles(where: { status: { eq: ""Published"" } }) {
    id
    title
    publishedDate
  }
}";

var publishedArticles = await articleRepository.ExecuteGraphQlQueryAsync(publishedQuery);
```

## Troubleshooting

### Common Issues

1. **Field not found**: Ensure GraphQL field names map to entity properties
2. **Query parsing fails**: Check query syntax matches supported format
3. **No results**: Verify data exists and query conditions are correct

### Debug Information

Enable logging to see generated EF Core queries:

```csharp
// In appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

This will show the SQL queries generated from your GraphQL queries.
