using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Cqrs.Queries;
using Idevs.Foundation.Cqrs.Models;
using System.Linq.Expressions;

namespace Idevs.Foundation.Cqrs.Queries;

/// <summary>
/// Query operations for entities
/// </summary>
public enum EntityQueryOperation
{
    /// <summary>
    /// Retrieve a single entity by ID
    /// </summary>
    Retrieve,
    /// <summary>
    /// List entities with optional filtering, sorting, and paging
    /// </summary>
    List
}

/// <summary>
/// Caching interface for queries that can be cached
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// The cache key for this query. If null, the query will not be cached.
    /// </summary>
    string? CacheKey { get; }
    
    /// <summary>
    /// How long to cache the query result
    /// </summary>
    TimeSpan CacheDuration { get; }
}

/// <summary>
/// Generic query for entity operations supporting Retrieve and List operations.
/// This query follows the CQRS pattern and provides a unified interface for all entity queries.
/// </summary>
/// <typeparam name="TDto">The DTO type that represents the entity</typeparam>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public record EntityQuery<TDto, TId> : IQuery<EntityQueryResponse<TDto>>, ICacheableQuery
    where TDto : class, IHasId<TId>
{
    /// <summary>
    /// The operation to perform on the entity
    /// </summary>
    public EntityQueryOperation Operation { get; }

    /// <summary>
    /// The query request containing all query parameters
    /// </summary>
    public EntityQueryRequest<TDto, TId> Request { get; }

    /// <summary>
    /// The cache key for this query. If null, the query will not be cached.
    /// </summary>
    public string? CacheKey { get; }

    /// <summary>
    /// How long to cache the query result
    /// </summary>
    public TimeSpan CacheDuration { get; }

    /// <summary>
    /// Creates a new entity query
    /// </summary>
    /// <param name="operation">The query operation</param>
    /// <param name="request">The query request</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration (default: 5 minutes)</param>
    public EntityQuery(
        EntityQueryOperation operation, 
        EntityQueryRequest<TDto, TId> request,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
    {
        Operation = operation;
        Request = request ?? throw new ArgumentNullException(nameof(request));
        CacheKey = cacheKey;
        CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Validates that the query has the required data for the specified operation
    /// </summary>
    public bool IsValid => Operation switch
    {
        EntityQueryOperation.Retrieve => Request.IsSingleEntity && Request.IsValid,
        EntityQueryOperation.List => Request.IsMultipleEntities && Request.IsValid,
        _ => false
    };
}

/// <summary>
/// Factory methods for creating entity queries
/// </summary>
public static class EntityQuery
{
    /// <summary>
    /// Creates a query to retrieve a single entity by ID
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityId">The ID of the entity to retrieve</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The retrieve query</returns>
    public static EntityQuery<TDto, TId> Retrieve<TDto, TId>(
        TId entityId,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            EntityId = entityId
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.Retrieve, 
            request, 
            cacheKey, 
            cacheDuration);
    }

    /// <summary>
    /// Creates a query to retrieve multiple entities by IDs
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityIds">The IDs of the entities to retrieve</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The list query</returns>
    public static EntityQuery<TDto, TId> List<TDto, TId>(
        List<TId> entityIds,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            EntityIds = entityIds
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.List, 
            request, 
            cacheKey, 
            cacheDuration);
    }

    /// <summary>
    /// Creates a query to list entities with optional filtering and paging
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="pageNumber">Optional page number for paging</param>
    /// <param name="pageSize">Optional page size for paging</param>
    /// <param name="filters">Optional filters</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="searchText">Optional search text</param>
    /// <param name="searchableFields">Optional searchable fields</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The list query</returns>
    public static EntityQuery<TDto, TId> List<TDto, TId>(
        int? pageNumber = null,
        int? pageSize = null,
        CompositeFilterDescriptor? filters = null,
        List<SortDescriptor>? sorts = null,
        string? searchText = null,
        List<string>? searchableFields = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Filters = filters,
            Sorts = sorts,
            SearchText = searchText,
            SearchableFields = searchableFields ?? new List<string>()
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.List, 
            request, 
            cacheKey, 
            cacheDuration);
    }

    /// <summary>
    /// Creates a query to list entities using a predicate expression
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="predicate">The predicate expression</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The list query</returns>
    public static EntityQuery<TDto, TId> List<TDto, TId>(
        Expression<Func<TDto, bool>> predicate,
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            Predicate = predicate,
            Sorts = sorts
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.List, 
            request, 
            cacheKey, 
            cacheDuration);
    }

    /// <summary>
    /// Creates a query to list all entities (no filtering)
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The list query</returns>
    public static EntityQuery<TDto, TId> ListAll<TDto, TId>(
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            Sorts = sorts
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.List, 
            request, 
            cacheKey, 
            cacheDuration);
    }

    /// <summary>
    /// Creates a paged query to list entities
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="filters">Optional filters</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Cache duration</param>
    /// <returns>The paged list query</returns>
    public static EntityQuery<TDto, TId> PagedList<TDto, TId>(
        int pageNumber,
        int pageSize,
        CompositeFilterDescriptor? filters = null,
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null)
        where TDto : class, IHasId<TId>
    {
        var request = new EntityQueryRequest<TDto, TId>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Filters = filters,
            Sorts = sorts
        };

        return new EntityQuery<TDto, TId>(
            EntityQueryOperation.List, 
            request, 
            cacheKey, 
            cacheDuration);
    }
}
