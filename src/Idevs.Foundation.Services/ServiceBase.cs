using Idevs.Foundation.Abstractions.Common;
using Idevs.Foundation.Abstractions.Logging;
using Idevs.Foundation.Abstractions.Services;
using Idevs.Foundation.Cqrs.Commands;
using Idevs.Foundation.Cqrs.Models;
using Idevs.Foundation.Cqrs.Queries;
using Idevs.Foundation.Cqrs.Results;
using Idevs.Foundation.Mediator.Core;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Linq.Expressions;

namespace Idevs.Foundation.Services;

/// <summary>
/// Base class for services providing common functionality like logging and CQRS operations
/// </summary>
public abstract class ServiceBase : IServiceBase
{
    /// <summary>
    /// The mediator for CQRS operations
    /// </summary>
    protected readonly IMediator Mediator;

    /// <summary>
    /// The logger instance for this service
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the ServiceBase class
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="logger">The logger instance</param>
    protected ServiceBase(IMediator mediator, ILogger logger)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Initializes a new instance of the ServiceBase class using LogManager
    /// </summary>
    /// <param name="mediator">The mediator instance</param>
    /// <param name="logManager">The log manager instance</param>
    protected ServiceBase(IMediator mediator, ILogManager logManager)
    {
        Mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        
        var logManagerInstance = logManager ?? throw new ArgumentNullException(nameof(logManager));
        Logger = logManagerInstance.GetLogger(GetType());
    }

    /// <summary>
    /// Executes an operation with logging and exception handling
    /// </summary>
    /// <typeparam name="T">The result type</typeparam>
    /// <param name="operation">The operation to execute</param>
    /// <param name="operationName">The name of the operation for logging</param>
    /// <param name="parameters">Optional parameters for logging</param>
    /// <returns>The result wrapped in a Result object</returns>
    protected async Task<Result<T>> ExecuteWithLoggingAsync<T>(
        Func<Task<T>> operation,
        string operationName,
        object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogInformation("Starting operation {OperationName} with parameters {@Parameters}", 
                operationName, parameters);

            var result = await operation();
            
            stopwatch.Stop();
            Logger.LogInformation("Completed operation {OperationName} in {ElapsedMilliseconds}ms", 
                operationName, stopwatch.ElapsedMilliseconds);

            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMilliseconds}ms with parameters {@Parameters}", 
                operationName, stopwatch.ElapsedMilliseconds, parameters);
            
            return Result<T>.Failure($"Operation {operationName} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Executes an operation with logging and exception handling (void return)
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <param name="operationName">The name of the operation for logging</param>
    /// <param name="parameters">Optional parameters for logging</param>
    /// <returns>The result indicating success or failure</returns>
    protected async Task<Result> ExecuteWithLoggingAsync(
        Func<Task> operation,
        string operationName,
        object? parameters = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            Logger.LogInformation("Starting operation {OperationName} with parameters {@Parameters}", 
                operationName, parameters);

            await operation();
            
            stopwatch.Stop();
            Logger.LogInformation("Completed operation {OperationName} in {ElapsedMilliseconds}ms", 
                operationName, stopwatch.ElapsedMilliseconds);

            return Result.Success();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Operation {OperationName} failed after {ElapsedMilliseconds}ms with parameters {@Parameters}", 
                operationName, stopwatch.ElapsedMilliseconds, parameters);
            
            return Result.Failure($"Operation {operationName} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends a command through the mediator with logging
    /// </summary>
    /// <typeparam name="TCommand">The command type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="command">The command to send</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command result</returns>
    protected async Task<TResult> SendCommandAsync<TCommand, TResult>(
        TCommand command, 
        CancellationToken cancellationToken = default)
        where TCommand : class, Idevs.Foundation.Cqrs.Commands.ICommand<TResult>
    {
        var commandName = typeof(TCommand).Name;
        
        Logger.LogInformation("Sending command {CommandName} with data {@Command}", 
            commandName, command);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await Mediator.SendAsync<TCommand, TResult>(command, cancellationToken);
            
            stopwatch.Stop();
            Logger.LogInformation("Command {CommandName} completed successfully in {ElapsedMilliseconds}ms", 
                commandName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Command {CommandName} failed after {ElapsedMilliseconds}ms", 
                commandName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Sends a query through the mediator with logging
    /// </summary>
    /// <typeparam name="TQuery">The query type</typeparam>
    /// <typeparam name="TResult">The result type</typeparam>
    /// <param name="query">The query to send</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query result</returns>
    protected async Task<TResult> SendQueryAsync<TQuery, TResult>(
        TQuery query, 
        CancellationToken cancellationToken = default)
        where TQuery : class, Idevs.Foundation.Cqrs.Queries.IQuery<TResult>
    {
        var queryName = typeof(TQuery).Name;
        
        Logger.LogDebug("Sending query {QueryName} with data {@Query}", 
            queryName, query);

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var result = await Mediator.QueryAsync<TQuery, TResult>(query, cancellationToken);
            
            stopwatch.Stop();
            Logger.LogDebug("Query {QueryName} completed successfully in {ElapsedMilliseconds}ms", 
                queryName, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.LogError(ex, "Query {QueryName} failed after {ElapsedMilliseconds}ms", 
                queryName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    /// <summary>
    /// Logs an information message with structured logging
    /// </summary>
    /// <param name="message">The message template</param>
    /// <param name="args">The message arguments</param>
    protected void LogInformation(string message, params object?[] args)
    {
        Logger.LogInformation(message, args);
    }

    /// <summary>
    /// Logs a warning message with structured logging
    /// </summary>
    /// <param name="message">The message template</param>
    /// <param name="args">The message arguments</param>
    protected void LogWarning(string message, params object?[] args)
    {
        Logger.LogWarning(message, args);
    }

    /// <summary>
    /// Logs an error message with structured logging
    /// </summary>
    /// <param name="exception">The exception</param>
    /// <param name="message">The message template</param>
    /// <param name="args">The message arguments</param>
    protected void LogError(Exception exception, string message, params object?[] args)
    {
        Logger.LogError(exception, message, args);
    }

    /// <summary>
    /// Logs an error message with structured logging
    /// </summary>
    /// <param name="message">The message template</param>
    /// <param name="args">The message arguments</param>
    protected void LogError(string message, params object?[] args)
    {
        Logger.LogError(message, args);
    }

    /// <summary>
    /// Logs a debug message with structured logging
    /// </summary>
    /// <param name="message">The message template</param>
    /// <param name="args">The message arguments</param>
    protected void LogDebug(string message, params object?[] args)
    {
        Logger.LogDebug(message, args);
    }

    #region Entity CQRS Operations

    /// <summary>
    /// Creates a single entity using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entity">The entity to create</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> CreateEntityAsync<TDto, TId>(
        TDto entity,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Create<TDto, TId>(entity);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Creates multiple entities using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entities">The entities to create</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> CreateEntitiesAsync<TDto, TId>(
        List<TDto> entities,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Create<TDto, TId>(entities);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Updates a single entity using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entity">The entity to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> UpdateEntityAsync<TDto, TId>(
        TDto entity,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Update<TDto, TId>(entity);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Updates multiple entities using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entities">The entities to update</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> UpdateEntitiesAsync<TDto, TId>(
        List<TDto> entities,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Update<TDto, TId>(entities);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Deletes a single entity by ID using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityId">The ID of the entity to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> DeleteEntityAsync<TDto, TId>(
        TId entityId,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Delete<TDto, TId>(entityId);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Deletes multiple entities by IDs using EntityCommand
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityIds">The IDs of the entities to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The command response</returns>
    protected async Task<EntityCommandResponse<TDto>> DeleteEntitiesAsync<TDto, TId>(
        List<TId> entityIds,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var command = EntityCommand.Delete<TDto, TId>(entityIds);
        return await SendCommandAsync<EntityCommand<TDto, TId>, EntityCommandResponse<TDto>>(command, cancellationToken);
    }

    /// <summary>
    /// Retrieves a single entity by ID using EntityQuery
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityId">The ID of the entity to retrieve</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> RetrieveEntityAsync<TDto, TId>(
        TId entityId,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.Retrieve<TDto, TId>(entityId, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    /// <summary>
    /// Lists entities by IDs using EntityQuery
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="entityIds">The IDs of the entities to list</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> ListEntitiesByIdsAsync<TDto, TId>(
        List<TId> entityIds,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.List<TDto, TId>(entityIds, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    /// <summary>
    /// Lists entities with optional filtering and paging using EntityQuery
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="pageNumber">Optional page number</param>
    /// <param name="pageSize">Optional page size</param>
    /// <param name="filters">Optional filters</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="searchText">Optional search text</param>
    /// <param name="searchableFields">Optional searchable fields</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> ListEntitiesAsync<TDto, TId>(
        int? pageNumber = null,
        int? pageSize = null,
        CompositeFilterDescriptor? filters = null,
        List<SortDescriptor>? sorts = null,
        string? searchText = null,
        List<string>? searchableFields = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.List<TDto, TId>(
            pageNumber, pageSize, filters, sorts, 
            searchText, searchableFields, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    /// <summary>
    /// Lists entities using a predicate expression
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="predicate">The predicate expression</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> ListEntitiesByPredicateAsync<TDto, TId>(
        Expression<Func<TDto, bool>> predicate,
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.List<TDto, TId>(predicate, sorts, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    /// <summary>
    /// Lists all entities with optional sorting
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> ListAllEntitiesAsync<TDto, TId>(
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.ListAll<TDto, TId>(sorts, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    /// <summary>
    /// Lists entities with paging
    /// </summary>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TId">The ID type</typeparam>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Page size</param>
    /// <param name="filters">Optional filters</param>
    /// <param name="sorts">Optional sorts</param>
    /// <param name="cacheKey">Optional cache key</param>
    /// <param name="cacheDuration">Optional cache duration</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The query response</returns>
    protected async Task<EntityQueryResponse<TDto>> ListEntitiesPagedAsync<TDto, TId>(
        int pageNumber,
        int pageSize,
        CompositeFilterDescriptor? filters = null,
        List<SortDescriptor>? sorts = null,
        string? cacheKey = null,
        TimeSpan? cacheDuration = null,
        CancellationToken cancellationToken = default)
        where TDto : class, IHasId<TId>
    {
        var query = EntityQuery.PagedList<TDto, TId>(pageNumber, pageSize, filters, sorts, cacheKey, cacheDuration);
        return await SendQueryAsync<EntityQuery<TDto, TId>, EntityQueryResponse<TDto>>(query, cancellationToken);
    }

    #endregion
}
