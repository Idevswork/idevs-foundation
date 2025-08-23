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
        var enumerable = ids?.ToArray() ?? Array.Empty<TId>();
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
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public virtual async Task<T?> FirstOrDefaultWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult<T?>(null);
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult(new List<T>());
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string key,
        string value,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult(new List<T>());
    }

    /// <inheritdoc />
    public virtual async Task<List<T>> GetByCriteriaWithJsonQueryAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, JsonObject?>> jsonPredicate,
        string[] valueChoices,
        string key,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("JSON query functionality not implemented in base class. Override in derived class with provider-specific implementation.");
        return await Task.FromResult(new List<T>());
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

    #endregion
}
