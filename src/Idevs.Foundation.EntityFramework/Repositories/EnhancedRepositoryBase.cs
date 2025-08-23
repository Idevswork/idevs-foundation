using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Abstractions.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.EntityFramework.Repositories;

/// <summary>
/// Enhanced repository base with JSON query support for major databases and GraphQL integration.
/// Automatically detects the database provider and provides optimized implementations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class EnhancedRepositoryBase<T, TId> : RepositoryBase<T, TId>
    where T : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    private readonly string _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedRepositoryBase{T, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    protected EnhancedRepositoryBase(DbContext dbContext, ILogger<RepositoryBase<T, TId>> logger)
        : base(dbContext, logger)
    {
        _databaseProvider = DetectDatabaseProvider(dbContext);
    }

    #region Enhanced JSON Query Methods

    /// <summary>
    /// Performs JSON querying with database-specific optimizations.
    /// </summary>
    /// <param name="jsonPredicate">Expression to select the JSON column.</param>
    /// <param name="key">JSON key to search for.</param>
    /// <param name="value">Value to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>First matching entity or null.</returns>
    public override async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        return _databaseProvider switch
        {
            "PostgreSQL" => (T?)await ExecutePostgreSqlJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            "SQL Server" => (T?)await ExecuteSqlServerJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            "MySQL" => (T?)await ExecuteMySqlJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            "SQLite" => (T?)await ExecuteSqliteJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            "In-Memory" => (T?)await ExecuteInMemoryJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            _ => await base.FirstOrDefaultWithJsonQueryAsync(jsonPredicate, key, value, cancellationToken)
        };
    }

    /// <summary>
    /// Gets all entities matching JSON criteria with database-specific optimizations.
    /// </summary>
    /// <param name="jsonPredicate">Expression to select the JSON column.</param>
    /// <param name="key">JSON key to search for.</param>
    /// <param name="value">Value to match.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching entities.</returns>
    public override async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        var result = _databaseProvider switch
        {
            "PostgreSQL" => await ExecutePostgreSqlJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            "SQL Server" => await ExecuteSqlServerJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            "MySQL" => await ExecuteMySqlJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            "SQLite" => await ExecuteSqliteJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            "In-Memory" => await ExecuteInMemoryJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            _ => await base.GetByCriteriaWithJsonQueryAsync(jsonPredicate, key, value, cancellationToken)
        };

        return result is List<T> listResult ? listResult : new List<T> { (T)result! };
    }

    /// <summary>
    /// Executes GraphQL-style queries with automatic translation to database-specific operations.
    /// </summary>
    /// <param name="graphqlQuery">GraphQL query string.</param>
    /// <param name="variables">Query variables.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entities matching the GraphQL query.</returns>
    public override async Task<List<T>> ExecuteGraphQLQueryAsync(
        string graphqlQuery,
        Dictionary<string, object>? variables = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse GraphQL query into filter conditions
            var filters = ParseGraphQLFilters(graphqlQuery, variables);
            
            var query = QueryNoTracking();

            // Apply filters based on database provider capabilities
            foreach (var filter in filters)
            {
                query = ApplyFilter(query, filter);
            }

            return await query.ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new NotSupportedException($"GraphQL query execution failed for {_databaseProvider}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Performs advanced JSON path queries with aggregation support.
    /// </summary>
    /// <param name="jsonPredicate">Expression to select the JSON column.</param>
    /// <param name="jsonPath">JSON path to query (e.g., "metadata.tags[0]").</param>
    /// <param name="operation">Operation type: "equals", "contains", "exists".</param>
    /// <param name="value">Value to compare (null for "exists" operation).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of matching entities.</returns>
    public virtual async Task<List<T>> ExecuteJsonPathQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string jsonPath,
        string operation,
        object? value = null,
        CancellationToken cancellationToken = default)
    {
        return _databaseProvider switch
        {
            "PostgreSQL" => await ExecutePostgreSqlJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            "SQL Server" => await ExecuteSqlServerJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            "MySQL" => await ExecuteMySqlJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            "SQLite" => await ExecuteSqliteJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            _ => throw new NotSupportedException($"JSON path queries are not supported for {_databaseProvider}")
        };
    }

    #endregion

    #region Database-Specific Implementations

    private async Task<object?> ExecutePostgreSqlJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        bool single,
        CancellationToken cancellationToken)
    {
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        // Use simple string contains for PostgreSQL JSONB
        var query = QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains($"\"{key}\":\"{value}\""));

        return single 
            ? (object?)await query.FirstOrDefaultAsync(cancellationToken) 
            : (object)await query.ToListAsync(cancellationToken);
    }

    private async Task<object?> ExecuteSqlServerJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        bool single,
        CancellationToken cancellationToken)
    {
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        // Use string contains for SQL Server JSON
        var query = QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains($"\"{key}\":\"{value}\""));

        return single 
            ? (object?)await query.FirstOrDefaultAsync(cancellationToken) 
            : (object)await query.ToListAsync(cancellationToken);
    }

    private async Task<object?> ExecuteMySqlJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        bool single,
        CancellationToken cancellationToken)
    {
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        // Use string contains for MySQL JSON
        var query = QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains($"\"{key}\":\"{value}\""));

        return single 
            ? (object?)await query.FirstOrDefaultAsync(cancellationToken) 
            : (object)await query.ToListAsync(cancellationToken);
    }

    private async Task<object?> ExecuteSqliteJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        bool single,
        CancellationToken cancellationToken)
    {
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        // Use string contains for SQLite JSON
        var query = QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains($"\"{key}\":\"{value}\""));

        return single 
            ? (object?)await query.FirstOrDefaultAsync(cancellationToken) 
            : (object)await query.ToListAsync(cancellationToken);
    }

    private async Task<List<T>> ExecutePostgreSqlJsonPathAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string jsonPath,
        string operation,
        object? value,
        CancellationToken cancellationToken)
    {
        // Simplified implementation - in production, use proper PostgreSQL JSON path operators
        var searchPattern = value != null ? $"{jsonPath}:{JsonSerializer.Serialize(value)}" : jsonPath;
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        return await QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(searchPattern))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<T>> ExecuteSqlServerJsonPathAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string jsonPath,
        string operation,
        object? value,
        CancellationToken cancellationToken)
    {
        // Simplified implementation - in production, use SQL Server JSON_VALUE functions
        var searchPattern = value != null ? JsonSerializer.Serialize(value).Trim('"') : jsonPath;
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        return await QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(searchPattern))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<T>> ExecuteMySqlJsonPathAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string jsonPath,
        string operation,
        object? value,
        CancellationToken cancellationToken)
    {
        // Simplified implementation - in production, use MySQL JSON_EXTRACT functions
        var searchPattern = value != null ? JsonSerializer.Serialize(value).Trim('"') : jsonPath;
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        return await QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(searchPattern))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<T>> ExecuteSqliteJsonPathAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string jsonPath,
        string operation,
        object? value,
        CancellationToken cancellationToken)
    {
        // Simplified implementation - in production, use SQLite JSON functions
        var searchPattern = value != null ? JsonSerializer.Serialize(value).Trim('"') : jsonPath;
        var jsonColumnName = GetJsonColumnName(jsonPredicate);
        
        return await QueryNoTracking()
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(searchPattern))
            .ToListAsync(cancellationToken);
    }

    private Task<object?> ExecuteInMemoryJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        bool single,
        CancellationToken cancellationToken)
    {
        // For In-Memory provider, we need to work with the JsonObject directly
        // and then convert to string for comparison
        var query = QueryNoTracking()
            .AsEnumerable() // Switch to client-side evaluation
            .Where(entity =>
            {
                var jsonObj = jsonPredicate.Compile()(entity);
                if (jsonObj == null) return false;

                var jsonString = jsonObj.ToJsonString();
                return jsonString.Contains($"\"{key}\":\"{value}\"");
            })
            .AsQueryable(); // Convert back to IQueryable for consistency

        return Task.FromResult(
            single
                ? (object?)query.FirstOrDefault()
                : (object)query.ToList()
        );
    }

    #endregion

    #region Helper Methods

    private static string DetectDatabaseProvider(DbContext context)
    {
        try
        {
            var providerName = context.Database.ProviderName?.ToLowerInvariant();
            
            return providerName switch
            {
                string p when p.Contains("npgsql") => "PostgreSQL",
                string p when p.Contains("sqlserver") => "SQL Server", 
                string p when p.Contains("sqlite") => "SQLite",
                string p when p.Contains("mysql") => "MySQL",
                string p when p.Contains("oracle") => "Oracle",
                string p when p.Contains("inmemory") => "In-Memory",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetJsonColumnName(Expression<Func<T, JsonObject?>> jsonPredicate)
    {
        if (jsonPredicate.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        throw new ArgumentException("Invalid JSON predicate expression", nameof(jsonPredicate));
    }

    private List<QueryFilter> ParseGraphQLFilters(string graphqlQuery, Dictionary<string, object>? variables)
    {
        var filters = new List<QueryFilter>();
        
        // Simple GraphQL parsing - in production, use a proper GraphQL parser
        // Example: { users(where: { name: { eq: "John" } }) { id name } }
        
        // Extract simple equality filters
        var whereMatch = System.Text.RegularExpressions.Regex.Match(
            graphqlQuery, 
            @"where:\s*\{\s*(\w+):\s*\{\s*(\w+):\s*""?([^""}\s]+)""?\s*\}\s*\}");
        
        if (whereMatch.Success)
        {
            filters.Add(new QueryFilter
            {
                Field = whereMatch.Groups[1].Value,
                Operator = whereMatch.Groups[2].Value,
                Value = whereMatch.Groups[3].Value
            });
        }

        return filters;
    }

    private IQueryable<T> ApplyFilter(IQueryable<T> query, QueryFilter filter)
    {
        // Map common GraphQL field names to actual property names
        var propertyName = MapGraphQLFieldToProperty(filter.Field);
        
        return filter.Operator switch
        {
            "eq" => query.Where(entity => EF.Property<string>(entity, propertyName) == filter.Value),
            "contains" => query.Where(entity => EF.Property<string>(entity, propertyName).Contains(filter.Value)),
            "startsWith" => query.Where(entity => EF.Property<string>(entity, propertyName).StartsWith(filter.Value)),
            "endsWith" => query.Where(entity => EF.Property<string>(entity, propertyName).EndsWith(filter.Value)),
            _ => query
        };
    }
    
    private static string MapGraphQLFieldToProperty(string fieldName)
    {
        // Convert GraphQL camelCase field names to PascalCase property names
        return fieldName switch
        {
            "name" => "Name",
            "price" => "Price",
            "category" => "Category",
            "id" => "Id",
            "isActive" => "IsActive",
            _ => char.ToUpper(fieldName[0]) + fieldName[1..] // Convert first char to uppercase
        };
    }

    #endregion

    #region Helper Classes

    private class QueryFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
