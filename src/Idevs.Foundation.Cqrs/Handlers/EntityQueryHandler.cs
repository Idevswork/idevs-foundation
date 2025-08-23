using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Abstractions.Repositories;
using Idevs.Foundation.Cqrs.Queries;
using Idevs.Foundation.Cqrs.Models;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Cqrs.Handlers;

/// <summary>
/// Generic query handler for entity operations.
/// Handles Retrieve and List operations for entities using the repository pattern.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDto">The DTO type</typeparam>
/// <typeparam name="TId">The ID type</typeparam>
public class EntityQueryHandler<TEntity, TDto, TId> : IQueryHandler<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>
    where TEntity : class, IHasId<TId>
    where TDto : class, IHasId<TId>
{
    private readonly IRepositoryBase<TEntity, TId> _repository;
    private readonly IMapper<TEntity, TDto> _mapper;
    private readonly ILogger<EntityQueryHandler<TEntity, TDto, TId>> _logger;

    /// <summary>
    /// Initializes a new instance of the EntityQueryHandler
    /// </summary>
    /// <param name="repository">The repository for entity operations</param>
    /// <param name="mapper">The mapper for converting between entity and DTO</param>
    /// <param name="logger">The logger instance</param>
    public EntityQueryHandler(
        IRepositoryBase<TEntity, TId> repository,
        IMapper<TEntity, TDto> mapper,
        ILogger<EntityQueryHandler<TEntity, TDto, TId>> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the entity query asynchronously
    /// </summary>
    /// <param name="query">The entity query to handle</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    public async Task<EntityQueryResponse<TDto>> HandleAsync(
        EntityQuery<TDto, TId> query,
        CancellationToken cancellationToken = default)
    {
        if (query == null)
        {
            _logger.LogError("Query cannot be null");
            return EntityQueryResponse<TDto>.Failure("Query cannot be null");
        }

        if (!query.IsValid)
        {
            _logger.LogWarning("Invalid query received: {Query}", query);
            return EntityQueryResponse<TDto>.Failure("Invalid query");
        }

        try
        {
            _logger.LogInformation("Processing {Operation} query for {EntityType}", 
                query.Operation, typeof(TEntity).Name);

            return query.Operation switch
            {
                EntityQueryOperation.Retrieve => await HandleRetrieveAsync(query, cancellationToken),
                EntityQueryOperation.List => await HandleListAsync(query, cancellationToken),
                _ => throw new NotSupportedException($"Operation {query.Operation} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Operation} query for {EntityType}", 
                query.Operation, typeof(TEntity).Name);
            return EntityQueryResponse<TDto>.Failure($"Error processing query: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles retrieve operations for single entities
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleRetrieveAsync(
        EntityQuery<TDto, TId> query,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = query.Request;
            
            if (request.EntityId == null)
            {
                _logger.LogWarning("Retrieve query has no entity ID");
                return EntityQueryResponse<TDto>.Failure("Entity ID is required for retrieve operation");
            }

            _logger.LogDebug("Retrieving entity: {EntityId}", request.EntityId);
            
            var entity = await _repository.RetrieveAsync(request.EntityId, cancellationToken);
            
            if (entity == null)
            {
                _logger.LogDebug("Entity not found: {EntityId}", request.EntityId);
                return EntityQueryResponse<TDto>.Success((TDto?)null);
            }

            var dto = _mapper.MapToDto(entity);
            
            _logger.LogInformation("Successfully retrieved entity: {EntityId}", request.EntityId);
            return EntityQueryResponse<TDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during retrieve operation");
            return EntityQueryResponse<TDto>.Failure($"Retrieve operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles list operations for multiple entities
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleListAsync(
        EntityQuery<TDto, TId> query,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = query.Request;
            
            // Handle list by specific IDs
            if (request.EntityIds != null && request.EntityIds.Any())
            {
                return await HandleListByIdsAsync(request, cancellationToken);
            }

            // Handle list by predicate
            if (request.Predicate != null)
            {
                return await HandleListByPredicateAsync(request, cancellationToken);
            }

            // Handle paged list
            if (request.IsPaged)
            {
                return await HandlePagedListAsync(request, cancellationToken);
            }

            // Handle filtered list
            if (request.Filters != null)
            {
                return await HandleFilteredListAsync(request, cancellationToken);
            }

            // Handle simple list (all entities)
            return await HandleSimpleListAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during list operation");
            return EntityQueryResponse<TDto>.Failure($"List operation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handles list by specific entity IDs
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleListByIdsAsync(
        EntityQueryRequest<TDto, TId> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing {Count} entities by IDs", request.EntityIds!.Count);
        
        var entities = await _repository.ListAsync(request.EntityIds!, cancellationToken);
        var dtos = entities.Select(_mapper.MapToDto).ToList();
        
        _logger.LogInformation("Successfully retrieved {Count} entities by IDs", dtos.Count);
        return EntityQueryResponse<TDto>.Success(dtos);
    }

    /// <summary>
    /// Handles list by predicate expression
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleListByPredicateAsync(
        EntityQueryRequest<TDto, TId> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing entities by predicate");
        
        // Note: This would need to convert the TDto predicate to a TEntity predicate
        // For now, we'll get all entities and filter on the DTO side
        // In a real implementation, you'd want to translate the predicate
        var allEntities = await _repository.GetAllAsync(cancellationToken);
        var allDtos = allEntities.Select(_mapper.MapToDto).AsQueryable();
        
        var filteredDtos = allDtos.Where(request.Predicate!).ToList();
        
        _logger.LogInformation("Successfully filtered {Count} entities by predicate", filteredDtos.Count);
        return EntityQueryResponse<TDto>.Success(filteredDtos);
    }

    /// <summary>
    /// Handles paged list operations
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandlePagedListAsync(
        EntityQueryRequest<TDto, TId> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing entities with paging: Page {PageNumber}, Size {PageSize}", 
            request.PageNumber, request.PageSize);
        
        var skip = ((request.PageNumber ?? 1) - 1) * (request.PageSize ?? 10);
        var take = request.PageSize ?? 10;
        
        // Get all entities first (in a real implementation, you'd want to use proper paging at the database level)
        var allEntities = await _repository.GetAllAsync(cancellationToken);
        var totalCount = allEntities.Count;
        
        // Apply paging
        var pagedEntities = allEntities.Skip(skip).Take(take).ToList();
        var dtos = pagedEntities.Select(_mapper.MapToDto).ToList();
        
        _logger.LogInformation("Successfully retrieved page {PageNumber} with {Count} entities, {TotalCount} total", 
            request.PageNumber, dtos.Count, totalCount);
        
        return EntityQueryResponse<TDto>.Success(dtos, totalCount, request.PageNumber!.Value, request.PageSize!.Value);
    }

    /// <summary>
    /// Handles filtered list operations
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleFilteredListAsync(
        EntityQueryRequest<TDto, TId> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing entities with filters");
        
        // Note: In a real implementation, you'd want to translate the filters to entity expressions
        // For now, we'll get all entities and filter on the DTO side
        var allEntities = await _repository.GetAllAsync(cancellationToken);
        var allDtos = allEntities.Select(_mapper.MapToDto).ToList();
        
        // Apply filters (this would be more sophisticated in a real implementation)
        var filteredDtos = ApplyFilters(allDtos, request.Filters!);
        
        // Apply sorting if specified
        if (request.Sorts != null && request.Sorts.Any())
        {
            filteredDtos = ApplySorting(filteredDtos, request.Sorts);
        }
        
        _logger.LogInformation("Successfully filtered {Count} entities", filteredDtos.Count);
        return EntityQueryResponse<TDto>.Success(filteredDtos);
    }

    /// <summary>
    /// Handles simple list operations (all entities)
    /// </summary>
    private async Task<EntityQueryResponse<TDto>> HandleSimpleListAsync(
        EntityQueryRequest<TDto, TId> request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Listing all entities");
        
        var entities = await _repository.GetAllAsync(cancellationToken);
        var dtos = entities.Select(_mapper.MapToDto).ToList();
        
        // Apply sorting if specified
        if (request.Sorts != null && request.Sorts.Any())
        {
            dtos = ApplySorting(dtos, request.Sorts);
        }
        
        _logger.LogInformation("Successfully retrieved {Count} entities", dtos.Count);
        return EntityQueryResponse<TDto>.Success(dtos);
    }

    /// <summary>
    /// Applies filters to a list of DTOs
    /// Note: This is a simplified implementation. In practice, you'd want to translate
    /// filters to database queries for better performance.
    /// </summary>
    private List<TDto> ApplyFilters(List<TDto> dtos, CompositeFilterDescriptor filters)
    {
        // This is a placeholder implementation
        // In a real application, you would implement proper filter translation
        _logger.LogWarning("Filter application is not fully implemented - returning all entities");
        return dtos;
    }

    /// <summary>
    /// Applies sorting to a list of DTOs
    /// </summary>
    private List<TDto> ApplySorting(List<TDto> dtos, List<SortDescriptor> sorts)
    {
        var query = dtos.AsQueryable();
        
        foreach (var sort in sorts)
        {
            // This is a simplified implementation using reflection
            // In a real application, you'd want to use expression trees for better performance
            var property = typeof(TDto).GetProperty(sort.FieldName);
            if (property != null)
            {
                if (sort.Direction == SortDirection.Ascending)
                {
                    query = query.OrderBy(x => property.GetValue(x));
                }
                else
                {
                    query = query.OrderByDescending(x => property.GetValue(x));
                }
            }
        }
        
        return query.ToList();
    }
}
