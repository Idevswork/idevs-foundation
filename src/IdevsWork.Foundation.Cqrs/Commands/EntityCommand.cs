using IdevsWork.Foundation.Abstractions.Common;
using IdevsWork.Foundation.Cqrs.Commands;
using IdevsWork.Foundation.Cqrs.Models;

namespace IdevsWork.Foundation.Cqrs.Commands;

/// <summary>
/// Command operations for entities
/// </summary>
public enum EntityCommandOperation
{
    Create,
    Update,
    Delete
}

/// <summary>
/// Generic command for entity operations supporting Create, Update, and Delete operations.
/// This command follows the CQRS pattern and provides a unified interface for all entity modifications.
/// </summary>
/// <typeparam name="TDto">The DTO type that represents the entity</typeparam>
/// <typeparam name="TId">The type of the entity identifier</typeparam>
public record EntityCommand<TDto, TId> : ICommand<EntityCommandResponse<TDto>>
    where TDto : class, IHasId<TId>
{
    /// <summary>
    /// The operation to perform on the entity
    /// </summary>
    public EntityCommandOperation Operation { get; }

    /// <summary>
    /// Single entity for Create or Update operations
    /// </summary>
    public TDto? Entity { get; }

    /// <summary>
    /// Multiple entities for bulk Create or Update operations
    /// </summary>
    public List<TDto>? Entities { get; }

    /// <summary>
    /// Single entity ID for Delete operations
    /// </summary>
    public TId? EntityId { get; }

    /// <summary>
    /// Multiple entity IDs for bulk Delete operations
    /// </summary>
    public List<TId>? EntityIds { get; }

    /// <summary>
    /// Creates a command for a single entity create operation
    /// </summary>
    /// <param name="entity">The entity to create</param>
    public EntityCommand(TDto entity)
    {
        Operation = EntityCommandOperation.Create;
        Entity = entity ?? throw new ArgumentNullException(nameof(entity));
        Entities = null;
        EntityId = default;
        EntityIds = null;
    }

    /// <summary>
    /// Creates a command for multiple entities create operation
    /// </summary>
    /// <param name="entities">The entities to create</param>
    public EntityCommand(List<TDto> entities)
    {
        Operation = EntityCommandOperation.Create;
        Entity = null;
        Entities = entities ?? throw new ArgumentNullException(nameof(entities));
        EntityId = default;
        EntityIds = null;
    }

    /// <summary>
    /// Creates a command for entity operations
    /// </summary>
    /// <param name="operation">The operation to perform</param>
    /// <param name="entity">Single entity for Create/Update operations</param>
    /// <param name="entities">Multiple entities for bulk Create/Update operations</param>
    /// <param name="entityId">Single entity ID for Delete operations</param>
    /// <param name="entityIds">Multiple entity IDs for bulk Delete operations</param>
    public EntityCommand(
        EntityCommandOperation operation,
        TDto? entity = null,
        List<TDto>? entities = null,
        TId? entityId = default,
        List<TId>? entityIds = null)
    {
        Operation = operation;
        Entity = entity;
        Entities = entities;
        EntityId = entityId;
        EntityIds = entityIds;
    }

    /// <summary>
    /// Gets the command request for this command
    /// </summary>
    public EntityCommandRequest<TDto, TId> ToRequest()
    {
        return new EntityCommandRequest<TDto, TId>
        {
            Entity = Entity,
            Entities = Entities,
            EntityId = EntityId,
            EntityIds = EntityIds
        };
    }

    /// <summary>
    /// Validates that the command has the required data for the specified operation
    /// </summary>
    public bool IsValid => Operation switch
    {
        EntityCommandOperation.Create => 
            (Entity != null && Entities == null && EntityId == null && EntityIds == null) ||
            (Entity == null && Entities is { Count: > 0 } && EntityId == null && EntityIds == null),
        EntityCommandOperation.Update => 
            (Entity != null && Entities == null && EntityId == null && EntityIds == null) ||
            (Entity == null && Entities is { Count: > 0 } && EntityId == null && EntityIds == null),
        EntityCommandOperation.Delete => 
            (Entity == null && Entities == null && EntityId != null && EntityIds == null) ||
            (Entity == null && Entities == null && EntityId == null && EntityIds is { Count: > 0 }),
        _ => false
    };

    /// <summary>
    /// Indicates if this is a single entity operation
    /// </summary>
    public bool IsSingleEntity => Operation switch
    {
        EntityCommandOperation.Create or EntityCommandOperation.Update => Entity != null,
        EntityCommandOperation.Delete => EntityId != null,
        _ => false
    };

    /// <summary>
    /// Indicates if this is a multiple entities operation
    /// </summary>
    public bool IsMultipleEntities => Operation switch
    {
        EntityCommandOperation.Create or EntityCommandOperation.Update => Entities is { Count: > 0 },
        EntityCommandOperation.Delete => EntityIds is { Count: > 0 },
        _ => false
    };
}

/// <summary>
/// Factory methods for creating entity commands
/// </summary>
public static class EntityCommand
{
    /// <summary>
    /// Creates a command to create a single entity
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entity">The entity to create</param>
    /// <returns>The create command</returns>
    public static EntityCommand<TDto, TId> Create<TDto, TId>(TDto entity)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(entity);
    }

    /// <summary>
    /// Creates a command to create multiple entities
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entities">The entities to create</param>
    /// <returns>The create command</returns>
    public static EntityCommand<TDto, TId> Create<TDto, TId>(List<TDto> entities)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(entities);
    }

    /// <summary>
    /// Creates a command to update a single entity
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entity">The entity to update</param>
    /// <returns>The update command</returns>
    public static EntityCommand<TDto, TId> Update<TDto, TId>(TDto entity)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(EntityCommandOperation.Update, entity);
    }

    /// <summary>
    /// Creates a command to update multiple entities
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entities">The entities to update</param>
    /// <returns>The update command</returns>
    public static EntityCommand<TDto, TId> Update<TDto, TId>(List<TDto> entities)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(EntityCommandOperation.Update, entities: entities);
    }

    /// <summary>
    /// Creates a command to delete a single entity by ID
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityId">The ID of the entity to delete</param>
    /// <returns>The delete command</returns>
    public static EntityCommand<TDto, TId> Delete<TDto, TId>(TId entityId)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(EntityCommandOperation.Delete, entityId: entityId);
    }

    /// <summary>
    /// Creates a command to delete multiple entities by IDs
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityIds">The IDs of the entities to delete</param>
    /// <returns>The delete command</returns>
    public static EntityCommand<TDto, TId> Delete<TDto, TId>(List<TId> entityIds)
        where TDto : class, IHasId<TId>
    {
        return new EntityCommand<TDto, TId>(EntityCommandOperation.Delete, entityIds: entityIds);
    }
}
