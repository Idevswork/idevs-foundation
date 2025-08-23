using System.Data.Common;
using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Abstractions.Repositories;
using Idevs.Foundation.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.EntityFramework.Repositories;

/// <summary>
/// Base implementation of a generic repository using Entity Framework Core.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public abstract class RepositoryBase<T, TId> : IRepositoryBase<T, TId>
    where T : class, IHasId<TId>
    where TId : IEquatable<TId>
{
    private readonly DbContext _dbContext;
    private readonly ILogger<RepositoryBase<T, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RepositoryBase{T, TId}"/> class.
    /// </summary>
    /// <param name="dbContext">The database context.</param>
    /// <param name="logger">The logger.</param>
    protected RepositoryBase(DbContext dbContext, ILogger<RepositoryBase<T, TId>> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Unit of Work

    /// <inheritdoc />
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task UseExistingTransactionAsync(DbTransaction transaction, CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.UseTransactionAsync(transaction, cancellationToken);
    }

    #endregion

    #region Query Methods

    /// <inheritdoc />
    public virtual IQueryable<T> Query()
    {
        return _dbContext.Set<T>().AsQueryable();
    }

    /// <inheritdoc />
    public virtual IQueryable<T> QueryNoTracking()
    {
        return _dbContext.Set<T>().AsNoTracking();
    }

    #endregion

    #region Retrieval Methods

    /// <inheritdoc />
    public virtual async Task<T?> RetrieveAsync(TId id, CancellationToken cancellationToken = default)
    {
        var predicate = ExpressionExtensions.CreateIdPredicate<T, TId>(id);
        return await QueryNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> ListAsync(IEnumerable<TId>? ids, CancellationToken cancellationToken = default)
    {
        var enumerable = ids?.ToArray() ?? [];
        if (enumerable.Length == 0)
        {
            return await QueryNoTracking().ToListAsync(cancellationToken);
        }

        var predicate = ExpressionExtensions.CreateIdPredicate<T, TId>(enumerable);
        return await QueryNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await QueryNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await QueryNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        var predicate = ExpressionExtensions.CreateIdPredicate<T, TId>(id);
        return await QueryNoTracking().AnyAsync(predicate, cancellationToken);
    }

    #endregion

    #region JSON Query Methods


    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("FirstOrDefaultWithJsonQueryAsync");
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("FirstOrDefaultWithJsonQueryAsync");
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("FirstOrDefaultWithJsonQueryAsync");
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("GetByCriteriaWithJsonQueryAsync");
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("GetByCriteriaWithJsonQueryAsync");
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        
        await Task.CompletedTask; // Satisfy async contract
        throw CreateJsonQueryNotSupportedException("GetByCriteriaWithJsonQueryAsync");
    }

    #endregion

    #region GraphQL Query Methods

    /// <inheritdoc />
    public virtual async Task<List<T>> ExecuteGraphQlQueryAsync(
        string graphqlQuery,
        Dictionary<string, object>? variables = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse GraphQL query into filter conditions
            var filters = ParseGraphQlFilters(graphqlQuery, variables);

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
            var dbProvider = DetectDatabaseProvider();
            throw new NotSupportedException($"GraphQL query execution failed for {dbProvider}: {ex.Message}", ex);
        }
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> ExecuteGraphQlWithJsonQueryAsync(
        string graphqlQuery,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        Dictionary<string, object>? variables = null,
        CancellationToken cancellationToken = default)
    {
        // Default implementation - can be overridden by enhanced repositories
        await Task.CompletedTask; // Satisfy async contract
        throw CreateGraphQlNotSupportedException("ExecuteGraphQlWithJsonQueryAsync");
    }

    #endregion

    #region Command Methods

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Set audit timestamps
        SetAuditPropertiesForAdd(entity);

        await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<(List<T> Entities, int RowsAffected)> AddAsync(
        IEnumerable<T>? entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<T>();
        if (entityList.Count == 0)
        {
            _logger.LogWarning("No entities provided for addition.");
            throw new ArgumentException("No entities provided for addition.", nameof(entities));
        }

        // Set audit timestamps
        foreach (var entity in entityList)
        {
            SetAuditPropertiesForAdd(entity);
        }

        await _dbContext.Set<T>().AddRangeAsync(entityList, cancellationToken);
        return (entityList, entityList.Count);
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        // Set audit timestamps
        SetAuditPropertiesForUpdate(entity);

        var updatedEntity = _dbContext.Set<T>().Update(entity);
        return await Task.FromResult(updatedEntity.Entity);
    }

    /// <inheritdoc />
    public virtual async Task<(List<T> Entities, int RowsAffected)> UpdateAsync(
        IEnumerable<T>? entities,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities?.ToList() ?? new List<T>();
        if (entityList.Count == 0)
        {
            _logger.LogWarning("No entities provided for update.");
            throw new ArgumentException("No entities provided for update.", nameof(entities));
        }

        // Set audit timestamps
        foreach (var entity in entityList)
        {
            SetAuditPropertiesForUpdate(entity);
        }

        _dbContext.Set<T>().UpdateRange(entityList);
        return await Task.FromResult((entityList, entityList.Count));
    }

    /// <inheritdoc />
    public virtual async Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var predicate = ExpressionExtensions.CreateIdPredicate<T, TId>(id);
        return await DeleteAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> DeleteAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        var idsArray = ids.ToArray();
        if (idsArray.Length == 0)
        {
            return 0;
        }

        var predicate = ExpressionExtensions.CreateIdPredicate<T, TId>(idsArray);
        return await DeleteAsync(predicate, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var entities = await Query().Where(predicate).ToListAsync(cancellationToken);
        if (entities.Count == 0)
        {
            return 0;
        }

        // Handle soft delete vs hard delete
        var softDeleteCount = 0;
        var hardDeleteEntities = new List<T>();

        foreach (var entity in entities)
        {
            if (entity is IHasDeletedLog softDeletableEntity)
            {
                softDeletableEntity.IsDeleted = true;
                softDeletableEntity.DeletedAt = DateTimeOffset.UtcNow;
                softDeleteCount++;
            }
            else
            {
                hardDeleteEntities.Add(entity);
            }
        }

        if (hardDeleteEntities.Count > 0)
        {
            _dbContext.Set<T>().RemoveRange(hardDeleteEntities);
        }

        return softDeleteCount + hardDeleteEntities.Count;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Sets audit properties for entity addition.
    /// </summary>
    /// <param name="entity">The entity to set properties for.</param>
    private static void SetAuditPropertiesForAdd(T entity)
    {
        var now = DateTimeOffset.UtcNow;

        if (entity is IHasCreatedLog createdLog)
        {
            createdLog.CreatedAt = now;
        }

        if (entity is IHasUpdatedLog updatedLog)
        {
            updatedLog.UpdatedAt = now;
        }
    }

    /// <summary>
    /// Sets audit properties for entity update.
    /// </summary>
    /// <param name="entity">The entity to set properties for.</param>
    private static void SetAuditPropertiesForUpdate(T entity)
    {
        if (entity is IHasUpdatedLog updatedLog)
        {
            updatedLog.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Creates a NotSupportedException with detailed guidance for implementing JSON query functionality.
    /// </summary>
    /// <param name="methodName">The name of the method that requires implementation.</param>
    /// <returns>A NotSupportedException with helpful guidance.</returns>
    private NotSupportedException CreateJsonQueryNotSupportedException(string methodName)
    {
        var dbProviderHint = DetectDatabaseProvider();
        
        var message = $"JSON query method '{methodName}' requires a database provider-specific implementation.\n\n" +
                     $"To implement JSON queries:\n" +
                     $"1. Override this method in your repository class\n" +
                     $"2. Use provider-specific JSON functions (detected provider: {dbProviderHint})\n" +
                     $"3. Implement the query logic for your database\n\n" +
                     $"Common implementations:\n" +
                     $"• PostgreSQL: Use JSONB operators and functions\n" +
                     $"• SQL Server: Use JSON_VALUE, JSON_QUERY functions\n" +
                     $"• SQLite: Use JSON functions (JSON_EXTRACT, etc.)\n\n" +
                     $"For examples and documentation, see: https://github.com/Idevswork/idevs-foundation/docs/JsonQueries.md";

        _logger.LogError("JSON query operation '{MethodName}' was called but not implemented. " +
                        "Database provider detected: {DbProvider}. Repository type: {RepositoryType}",
                        methodName, dbProviderHint, GetType().Name);

        return new NotSupportedException(message);
    }


    /// <summary>
    /// Attempts to detect the database provider being used.
    /// </summary>
    /// <returns>A string indicating the detected database provider or "Unknown".</returns>
    protected string DetectDatabaseProvider()
    {
        try
        {
            var providerName = _dbContext.Database.ProviderName?.ToLowerInvariant();
            return DatabaseProviders.Detect(providerName);
        }
        catch
        {
            return DatabaseProviders.UnknownProvider;
        }
    }

    /// <summary>
    /// Creates a NotSupportedException for GraphQL operations.
    /// </summary>
    /// <param name="methodName">The name of the method that requires implementation.</param>
    /// <returns>A NotSupportedException with helpful guidance.</returns>
    private NotSupportedException CreateGraphQlNotSupportedException(string methodName)
    {
        var dbProviderHint = DetectDatabaseProvider();
        
        var message = $"GraphQL method '{methodName}' requires a database provider that supports GraphQL integration.\n\n" +
                     $"To enable GraphQL support:\n" +
                     $"1. Install the appropriate GraphQL package for {dbProviderHint}\n" +
                     $"2. Configure GraphQL schema and resolvers\n" +
                     $"3. Override this method in your repository class\n\n" +
                     $"Supported providers with GraphQL:\n" +
                     $"• PostgreSQL: Use HotChocolate with Npgsql\n" +
                     $"• SQL Server: Use HotChocolate with SqlClient\n" +
                     $"• MySQL: Use HotChocolate with MySql.EntityFrameworkCore\n\n" +
                     $"For documentation, see: https://github.com/Idevswork/idevs-foundation/docs/GraphQLQueries.md";

        _logger.LogError("GraphQL operation '{MethodName}' was called but not implemented. " +
                        "Database provider detected: {DbProvider}. Repository type: {RepositoryType}",
                        methodName, dbProviderHint, GetType().Name);

        return new NotSupportedException(message);
    }

    /// <summary>
    /// Parses GraphQL query filters into QueryFilter objects.
    /// </summary>
    /// <param name="graphqlQuery">The GraphQL query string.</param>
    /// <param name="variables">Variables for the GraphQL query.</param>
    /// <returns>List of parsed query filters.</returns>
    private List<QueryFilter> ParseGraphQlFilters(string graphqlQuery, Dictionary<string, object>? variables)
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

    /// <summary>
    /// Applies a query filter to the IQueryable.
    /// </summary>
    /// <param name="query">The query to apply the filter to.</param>
    /// <param name="filter">The filter to apply.</param>
    /// <returns>The filtered query.</returns>
    private IQueryable<T> ApplyFilter(IQueryable<T> query, QueryFilter filter)
    {
        // Map common GraphQL field names to actual property names
        var propertyName = MapGraphQlFieldToProperty(filter.Field);

        return filter.Operator switch
        {
            "eq" => query.Where(entity => EF.Property<string>(entity, propertyName) == filter.Value),
            "contains" => query.Where(entity => EF.Property<string>(entity, propertyName).Contains(filter.Value)),
            "startsWith" => query.Where(entity => EF.Property<string>(entity, propertyName).StartsWith(filter.Value)),
            "endsWith" => query.Where(entity => EF.Property<string>(entity, propertyName).EndsWith(filter.Value)),
            _ => query
        };
    }

    /// <summary>
    /// Maps GraphQL field names to entity property names.
    /// </summary>
    /// <param name="fieldName">The GraphQL field name.</param>
    /// <returns>The corresponding entity property name.</returns>
    private static string MapGraphQlFieldToProperty(string fieldName)
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

    /// <summary>
    /// Represents a filter condition parsed from a GraphQL query.
    /// </summary>
    private class QueryFilter
    {
        public string Field { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    #endregion
}
