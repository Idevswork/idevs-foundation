using System.Linq.Expressions;
using System.Text.Json.Nodes;
using Idevs.Foundation.Abstractions.Common;

namespace Idevs.Foundation.Abstractions.Repositories;

/// <summary>
/// Defines a contract for a generic repository with comprehensive CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public interface IRepositoryBase<T, in TId> : IUnitOfWork 
    where T : class, IHasId<TId>
{
    #region Retrieval Methods
    
    /// <summary>
    /// Retrieves an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The entity if found; otherwise, null.</returns>
    Task<T?> RetrieveAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a list of entities by their identifiers.
    /// </summary>
    /// <param name="ids">The collection of entity identifiers.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities matching the provided identifiers.</returns>
    Task<List<T>> ListAsync(IEnumerable<TId>? ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries entities using a predicate expression.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of entities matching the predicate.</returns>
    Task<List<T>> QueryAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of all entities.</returns>
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity exists with the specified identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>True if the entity exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    #endregion

    #region GraphQL Query Methods

    /// <summary>
    /// Executes a GraphQL query and returns entities.
    /// </summary>
    /// <param name="graphqlQuery">The GraphQL query string.</param>
    /// <param name="variables">Variables for the GraphQL query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entities matching the GraphQL query.</returns>
    Task<List<T>> ExecuteGraphQlQueryAsync(
        string graphqlQuery,
        Dictionary<string, object>? variables = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a GraphQL query with JSON field filtering.
    /// </summary>
    /// <param name="graphqlQuery">The GraphQL query string.</param>
    /// <param name="jsonPredicate">Expression to select JSON field.</param>
    /// <param name="variables">Variables for the GraphQL query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of entities matching the GraphQL query with JSON filtering.</returns>
    Task<List<T>> ExecuteGraphQlWithJsonQueryAsync(
        string graphqlQuery,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        Dictionary<string, object>? variables = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region JSON Query Methods

    /// <summary>
    /// Finds the first entity matching a JSON property query.
    /// </summary>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="value">The JSON value to match.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string key, 
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the first entity matching both a predicate and a JSON property query.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="value">The JSON value to match.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string key, 
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching JSON property queries with multiple value choices.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="valueChoices">Array of JSON values to match.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The first matching entity or null.</returns>
    Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string[] valueChoices, 
        string key,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching a JSON property query.
    /// </summary>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="value">The JSON value to match.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string key,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching both a predicate and a JSON property query.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="value">The JSON value to match.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string key, 
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets entities matching both a predicate and JSON property queries with multiple value choices.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities.</param>
    /// <param name="jsonPredicate">Expression to access the JSON property.</param>
    /// <param name="valueChoices">Array of JSON values to match.</param>
    /// <param name="key">The JSON key to search for.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A list of matching entities.</returns>
    Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate, 
        string[] valueChoices, 
        string key,
        CancellationToken cancellationToken = default);

    #endregion

    #region Command Methods

    /// <summary>
    /// Adds a new entity.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The added entity.</returns>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple entities.
    /// </summary>
    /// <param name="entities">The entities to add.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A tuple containing the added entities and the number of rows affected.</returns>
    Task<(List<T> Entities, int RowsAffected)> AddAsync(
        IEnumerable<T>? entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The updated entity.</returns>
    Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple entities.
    /// </summary>
    /// <param name="entities">The entities to update.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A tuple containing the updated entities and the number of rows affected.</returns>
    Task<(List<T> Entities, int RowsAffected)> UpdateAsync(
        IEnumerable<T>? entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its identifier.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of entities deleted.</returns>
    Task<int> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple entities by their identifiers.
    /// </summary>
    /// <param name="ids">The entity identifiers.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of entities deleted.</returns>
    Task<int> DeleteAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to filter entities for deletion.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>The number of entities deleted.</returns>
    Task<int> DeleteAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets a queryable collection of entities with change tracking enabled.
    /// </summary>
    /// <returns>An IQueryable of entities.</returns>
    IQueryable<T> Query();

    /// <summary>
    /// Gets a queryable collection of entities with change tracking disabled.
    /// </summary>
    /// <returns>An IQueryable of entities without change tracking.</returns>
    IQueryable<T> QueryNoTracking();

    #endregion
}
