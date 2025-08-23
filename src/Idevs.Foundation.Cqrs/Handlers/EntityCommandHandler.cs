using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Abstractions.Repositories;
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Models;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Cqrs.Handlers;

/// <summary>
/// Generic command handler for entity operations.
/// Handles Create, Update, and Delete operations for entities using the repository pattern.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDto">The DTO type</typeparam>
/// <typeparam name="TId">The ID type</typeparam>
public class EntityCommandHandler<TEntity, TDto, TId> : ICommandHandler<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>
    where TEntity : class, IHasId<TId>
    where TDto : class, IHasId<TId>
{
    private readonly IRepositoryBase<TEntity, TId> _repository;
    private readonly IMapper<TEntity, TDto> _mapper;
    private readonly ILogger<EntityCommandHandler<TEntity, TDto, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the EntityCommandHandler
    /// </summary>
    /// <param name="repository">The repository for entity operations</param>
    /// <param name="mapper">The mapper for converting between entity and DTO</param>
    /// <param name="logger">The logger instance</param>
    public EntityCommandHandler(
        IRepositoryBase<TEntity, TId> repository,
        IMapper<TEntity, TDto> mapper,
        ILogger<EntityCommandHandler<TEntity, TDto, TId>> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the entity command asynchronously
    /// </summary>
    /// <param name="command">The entity command to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    public async Task<EntityCommandResponse<TDto>> HandleAsync(
        EntityCommand<TDto, TId> command,
        CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            _logger.LogError("Command cannot be null");
            return EntityCommandResponse<TDto>.Failure("Command cannot be null");
        }

        if (!command.IsValid)
        {
            _logger.LogWarning("Invalid command received: {Command}", command);
            return EntityCommandResponse<TDto>.Failure("Invalid command");
        }

        try
        {
            _logger.LogInformation("Processing {Operation} command for {EntityType}", 
                command.Operation, typeof(TEntity).Name);

            return command.Operation switch
            {
                EntityCommandOperation.Create => await HandleCreateAsync(command, cancellationToken),
                EntityCommandOperation.Update => await HandleUpdateAsync(command, cancellationToken),
                EntityCommandOperation.Delete => await HandleDeleteAsync(command, cancellationToken),
                _ => throw new NotSupportedException($"Operation {command.Operation} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Operation} command for {EntityType}", 
                command.Operation, typeof(TEntity).Name);
            return EntityCommandResponse<TDto>.Failure($"Error processing command: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles create operations
    /// </summary>
    private async Task<EntityCommandResponse<TDto>> HandleCreateAsync(
        EntityCommand<TDto, TId> command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (command.IsSingleEntity && command.Entity != null)
            {
                _logger.LogDebug("Creating single entity: {EntityId}", command.Entity.Id);
                
                var entity = _mapper.MapToEntity(command.Entity);
                var createdEntity = await _repository.AddAsync(entity, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                var resultDto = _mapper.MapToDto(createdEntity);
                
                _logger.LogInformation("Successfully created entity with ID: {EntityId}", createdEntity.Id);
                return EntityCommandResponse<TDto>.Success(resultDto);
            }
            
            if (command.IsMultipleEntities && command.Entities != null)
            {
                _logger.LogDebug("Creating {Count} entities", command.Entities.Count);
                
                var entities = command.Entities.Select(_mapper.MapToEntity).ToList();
                var result = await _repository.AddAsync(entities, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                var resultDtos = result.Entities.Select(_mapper.MapToDto).ToList();
                
                _logger.LogInformation("Successfully created {Count} entities, {RowsAffected} rows affected", 
                    entities.Count, result.RowsAffected);
                return EntityCommandResponse<TDto>.Success(resultDtos, result.RowsAffected);
            }

            _logger.LogWarning("Create command has no valid entity data");
            return EntityCommandResponse<TDto>.Failure("No entity data provided for create operation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during create operation");
            return EntityCommandResponse<TDto>.Failure($"Create operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles update operations
    /// </summary>
    private async Task<EntityCommandResponse<TDto>> HandleUpdateAsync(
        EntityCommand<TDto, TId> command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (command.IsSingleEntity && command.Entity != null)
            {
                _logger.LogDebug("Updating single entity: {EntityId}", command.Entity.Id);
                
                var entity = _mapper.MapToEntity(command.Entity);
                var updatedEntity = await _repository.UpdateAsync(entity, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                var resultDto = _mapper.MapToDto(updatedEntity);
                
                _logger.LogInformation("Successfully updated entity with ID: {EntityId}", updatedEntity.Id);
                return EntityCommandResponse<TDto>.Success(resultDto);
            }
            
            if (command.IsMultipleEntities && command.Entities != null)
            {
                _logger.LogDebug("Updating {Count} entities", command.Entities.Count);
                
                var entities = command.Entities.Select(_mapper.MapToEntity).ToList();
                var result = await _repository.UpdateAsync(entities, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                var resultDtos = result.Entities.Select(_mapper.MapToDto).ToList();
                
                _logger.LogInformation("Successfully updated {Count} entities, {RowsAffected} rows affected", 
                    entities.Count, result.RowsAffected);
                return EntityCommandResponse<TDto>.Success(resultDtos, result.RowsAffected);
            }

            _logger.LogWarning("Update command has no valid entity data");
            return EntityCommandResponse<TDto>.Failure("No entity data provided for update operation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during update operation");
            return EntityCommandResponse<TDto>.Failure($"Update operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles delete operations
    /// </summary>
    private async Task<EntityCommandResponse<TDto>> HandleDeleteAsync(
        EntityCommand<TDto, TId> command,
        CancellationToken cancellationToken)
    {
        try
        {
            if (command.IsSingleEntity && command.EntityId != null)
            {
                _logger.LogDebug("Deleting single entity: {EntityId}", command.EntityId);
                
                var rowsAffected = await _repository.DeleteAsync(command.EntityId, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Successfully deleted entity with ID: {EntityId}, {RowsAffected} rows affected", 
                    command.EntityId, rowsAffected);
                return EntityCommandResponse<TDto>.Success(rowsAffected);
            }
            
            if (command.IsMultipleEntities && command.EntityIds != null)
            {
                _logger.LogDebug("Deleting {Count} entities", command.EntityIds.Count);
                
                var rowsAffected = await _repository.DeleteAsync(command.EntityIds, cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Successfully deleted {Count} entities, {RowsAffected} rows affected", 
                    command.EntityIds.Count, rowsAffected);
                return EntityCommandResponse<TDto>.Success(rowsAffected);
            }

            _logger.LogWarning("Delete command has no valid entity ID data");
            return EntityCommandResponse<TDto>.Failure("No entity IDs provided for delete operation");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during delete operation");
            return EntityCommandResponse<TDto>.Failure($"Delete operation failed: {ex.Message}");
        }
    }
}

/// <summary>
/// Basic mapper interface for converting between entities and DTOs
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDto">The DTO type</typeparam>
public interface IMapper<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    /// <summary>
    /// Maps a DTO to an entity
    /// </summary>
    /// <param name="dto">The DTO to map</param>
    /// <returns>The mapped entity</returns>
    TEntity MapToEntity(TDto dto);

    /// <summary>
    /// Maps an entity to a DTO
    /// </summary>
    /// <param name="entity">The entity to map</param>
    /// <returns>The mapped DTO</returns>
    TDto MapToDto(TEntity entity);
}

/// <summary>
/// Base mapper implementation using expression-based mapping
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDto">The DTO type</typeparam>
public abstract class BaseMapper<TEntity, TDto> : IMapper<TEntity, TDto>
    where TEntity : class, new()
    where TDto : class, new()
{
    /// <summary>
    /// Maps a DTO to an entity
    /// </summary>
    public virtual TEntity MapToEntity(TDto dto)
    {
        if (dto == null) throw new ArgumentNullException(nameof(dto));
        
        var entity = new TEntity();
        MapToEntity(dto, entity);
        return entity;
    }

    /// <summary>
    /// Maps an entity to a DTO
    /// </summary>
    public virtual TDto MapToDto(TEntity entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        
        var dto = new TDto();
        MapToDto(entity, dto);
        return dto;
    }

    /// <summary>
    /// Override this method to provide custom mapping logic from DTO to entity
    /// </summary>
    protected abstract void MapToEntity(TDto dto, TEntity entity);

    /// <summary>
    /// Override this method to provide custom mapping logic from entity to DTO
    /// </summary>
    protected abstract void MapToDto(TEntity entity, TDto dto);
}
