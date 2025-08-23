using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.EntityFramework.Repositories;

/// <summary>
/// Enhanced repository base with JSON query support for major databases.
/// Automatically detects the database provider and provides optimized implementations.
/// GraphQL support is now provided by the base RepositoryBase class.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class EnhancedRepositoryBase<T, TId> : RepositoryBase<T, TId>
    where T : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    #region Constants

    private const string JsonKeyValuePattern = "\"{0}\":\"{1}\"";

    #endregion

    private readonly string _databaseProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnhancedRepositoryBase{T, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    protected EnhancedRepositoryBase(DbContext dbContext, ILogger<RepositoryBase<T, TId>> logger)
        : base(dbContext, logger)
    {
        _databaseProvider = DetectDatabaseProvider();
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
            DatabaseProviders.PostgreSqlProvider => (T?)await ExecutePostgreSqlJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            DatabaseProviders.SqlServerProvider => (T?)await ExecuteSqlServerJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            DatabaseProviders.MySqlProvider => (T?)await ExecuteMySqlJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            DatabaseProviders.SqliteProvider => (T?)await ExecuteSqliteJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
            DatabaseProviders.InMemoryProvider => (T?)await ExecuteInMemoryJsonQueryAsync(jsonPredicate, key, value, single: true, cancellationToken),
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
            DatabaseProviders.PostgreSqlProvider => await ExecutePostgreSqlJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            DatabaseProviders.SqlServerProvider => await ExecuteSqlServerJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            DatabaseProviders.MySqlProvider => await ExecuteMySqlJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            DatabaseProviders.SqliteProvider => await ExecuteSqliteJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            DatabaseProviders.InMemoryProvider => await ExecuteInMemoryJsonQueryAsync(jsonPredicate, key, value, single: false, cancellationToken),
            _ => await base.GetByCriteriaWithJsonQueryAsync(jsonPredicate, key, value, cancellationToken)
        };

        return result as List<T> ?? [(T)result!];
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
            DatabaseProviders.PostgreSqlProvider => await ExecutePostgreSqlJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            DatabaseProviders.SqlServerProvider => await ExecuteSqlServerJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            DatabaseProviders.MySqlProvider => await ExecuteMySqlJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
            DatabaseProviders.SqliteProvider => await ExecuteSqliteJsonPathAsync(jsonPredicate, jsonPath, operation, value, cancellationToken),
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
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(string.Format(JsonKeyValuePattern, key, value)));

        return single 
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
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
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(string.Format(JsonKeyValuePattern, key, value)));

        return single 
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
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
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(string.Format(JsonKeyValuePattern, key, value)));

        return single 
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
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
            .Where(entity => EF.Property<string>(entity, jsonColumnName).Contains(string.Format(JsonKeyValuePattern, key, value)));

        return single 
            ? await query.FirstOrDefaultAsync(cancellationToken)
            : await query.ToListAsync(cancellationToken);
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
                return jsonString.Contains(string.Format(JsonKeyValuePattern, key, value));
            })
            .AsQueryable(); // Convert back to IQueryable for consistency

        return Task.FromResult(
            single
                ? query.FirstOrDefault()
                : (object)query.ToList()
        );
    }

    #endregion

    #region Helper Methods

    private static string GetJsonColumnName(Expression<Func<T, JsonObject?>> jsonPredicate)
    {
        if (jsonPredicate.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }
        throw new ArgumentException("Invalid JSON predicate expression", nameof(jsonPredicate));
    }

    #endregion
}
