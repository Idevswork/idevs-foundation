using System.Linq.Expressions;
using IdevsWork.Foundation.Abstractions.Common;
using IdevsWork.Foundation.Abstractions.Services;
using IdevsWork.Foundation.Mediator.Core;
using Microsoft.Extensions.Logging;

namespace IdevsWork.Foundation.Services;

/// <summary>
/// CQRS-based implementation of generic service operations using mediator pattern.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The type of the entity identifier.</typeparam>
public class CqrsGenericService<T, TId> : ServiceBase, IGenericService<T, TId>
    where T : class, IHasId<TId>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CqrsGenericService{T, TId}"/> class.
    /// </summary>
    /// <param name="mediator">The mediator for dispatching commands and queries.</param>
    /// <param name="logger">The logger instance.</param>
    public CqrsGenericService(IMediator mediator, ILogger<CqrsGenericService<T, TId>> logger)
        : base(mediator, logger)
    {
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        var query = new GetByIdQuery<T, TId>(id);
        return await SendQueryAsync<GetByIdQuery<T, TId>, T?>(query, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByIdsAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        var query = new GetByIdsQuery<T, TId>(ids);
        return await SendQueryAsync<GetByIdsQuery<T, TId>, List<T>>(query, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var query = new GetAllQuery<T>();
        return await SendQueryAsync<GetAllQuery<T>, List<T>>(query, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        var query = new GetByPredicateQuery<T>(predicate);
        return await SendQueryAsync<GetByPredicateQuery<T>, List<T>>(query, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var command = new CreateEntityCommand<T>(entity);
        return await SendCommandAsync<CreateEntityCommand<T>, T>(command, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> CreateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var command = new CreateEntitiesCommand<T>(entities);
        return await SendCommandAsync<CreateEntitiesCommand<T>, List<T>>(command, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var command = new UpdateEntityCommand<T>(entity);
        return await SendCommandAsync<UpdateEntityCommand<T>, T>(command, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> UpdateAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var command = new UpdateEntitiesCommand<T>(entities);
        return await SendCommandAsync<UpdateEntitiesCommand<T>, List<T>>(command, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEntityCommand<T, TId>(id);
        var result = await SendCommandAsync<DeleteEntityCommand<T, TId>, int>(command, cancellationToken);
        return result > 0;
    }

    /// <inheritdoc />
    public virtual async Task<int> DeleteAsync(IEnumerable<TId> ids, CancellationToken cancellationToken = default)
    {
        var command = new DeleteEntitiesCommand<T, TId>(ids);
        return await SendCommandAsync<DeleteEntitiesCommand<T, TId>, int>(command, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        var query = new EntityExistsQuery<T, TId>(id);
        return await SendQueryAsync<EntityExistsQuery<T, TId>, bool>(query, cancellationToken);
    }
}

// Command and Query definitions would typically be in separate files, but included here for completeness

#region Commands

/// <summary>
/// Command to create a new entity.
/// </summary>
public record CreateEntityCommand<T>(T Entity) : IdevsWork.Foundation.Cqrs.Commands.ICommand<T>;

/// <summary>
/// Command to create multiple entities.
/// </summary>
public record CreateEntitiesCommand<T>(IEnumerable<T> Entities) : IdevsWork.Foundation.Cqrs.Commands.ICommand<List<T>>;

/// <summary>
/// Command to update an entity.
/// </summary>
public record UpdateEntityCommand<T>(T Entity) : IdevsWork.Foundation.Cqrs.Commands.ICommand<T>;

/// <summary>
/// Command to update multiple entities.
/// </summary>
public record UpdateEntitiesCommand<T>(IEnumerable<T> Entities) : IdevsWork.Foundation.Cqrs.Commands.ICommand<List<T>>;

/// <summary>
/// Command to delete an entity by ID.
/// </summary>
public record DeleteEntityCommand<T, TId>(TId Id) : IdevsWork.Foundation.Cqrs.Commands.ICommand<int>;

/// <summary>
/// Command to delete multiple entities by IDs.
/// </summary>
public record DeleteEntitiesCommand<T, TId>(IEnumerable<TId> Ids) : IdevsWork.Foundation.Cqrs.Commands.ICommand<int>;

#endregion

#region Queries

/// <summary>
/// Query to get an entity by ID.
/// </summary>
public record GetByIdQuery<T, TId>(TId Id) : IdevsWork.Foundation.Cqrs.Queries.IQuery<T?>;

/// <summary>
/// Query to get entities by IDs.
/// </summary>
public record GetByIdsQuery<T, TId>(IEnumerable<TId> Ids) : IdevsWork.Foundation.Cqrs.Queries.IQuery<List<T>>;

/// <summary>
/// Query to get all entities.
/// </summary>
public record GetAllQuery<T> : IdevsWork.Foundation.Cqrs.Queries.IQuery<List<T>>;

/// <summary>
/// Query to get entities by predicate.
/// </summary>
public record GetByPredicateQuery<T>(Expression<Func<T, bool>> Predicate) : IdevsWork.Foundation.Cqrs.Queries.IQuery<List<T>>;

/// <summary>
/// Query to check if an entity exists.
/// </summary>
public record EntityExistsQuery<T, TId>(TId Id) : IdevsWork.Foundation.Cqrs.Queries.IQuery<bool>;

#endregion
