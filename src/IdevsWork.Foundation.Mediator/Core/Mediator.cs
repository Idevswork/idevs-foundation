using IdevsWork.Foundation.Cqrs.Behaviors;
using IdevsWork.Foundation.Cqrs.Commands;
using IdevsWork.Foundation.Cqrs.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdevsWork.Foundation.Mediator.Core;

/// <summary>
/// Implementation of the mediator pattern for handling commands and queries.
/// </summary>
public class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Mediator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    /// <param name="logger">The logger.</param>
    public Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogDebug("Sending command {CommandType}", typeof(TCommand).Name);

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for command type {typeof(TCommand).Name}");
        }

        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TCommand, Unit>>().ToArray();
        
        if (behaviors.Length == 0)
        {
            await handler.HandleAsync(command, cancellationToken);
        }
        else
        {
            await ExecutePipelineAsync(command, behaviors, () => ExecuteHandlerAsync(handler, command, cancellationToken), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand<TResult>
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogDebug("Sending command {CommandType} expecting result {ResultType}", typeof(TCommand).Name, typeof(TResult).Name);

        var handler = _serviceProvider.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for command type {typeof(TCommand).Name}");
        }

        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TCommand, TResult>>().ToArray();
        
        if (behaviors.Length == 0)
        {
            return await handler.HandleAsync(command, cancellationToken);
        }
        else
        {
            return await ExecutePipelineAsync(command, behaviors, () => handler.HandleAsync(command, cancellationToken), cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        _logger.LogDebug("Executing query {QueryType} expecting result {ResultType}", typeof(TQuery).Name, typeof(TResult).Name);

        var handler = _serviceProvider.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler == null)
        {
            throw new InvalidOperationException($"No handler registered for query type {typeof(TQuery).Name}");
        }

        var behaviors = _serviceProvider.GetServices<IPipelineBehavior<TQuery, TResult>>().ToArray();
        
        if (behaviors.Length == 0)
        {
            return await handler.HandleAsync(query, cancellationToken);
        }
        else
        {
            return await ExecutePipelineAsync(query, behaviors, () => handler.HandleAsync(query, cancellationToken), cancellationToken);
        }
    }

    /// <summary>
    /// Executes the pipeline of behaviors.
    /// </summary>
    private static async Task<TResponse> ExecutePipelineAsync<TRequest, TResponse>(
        TRequest request,
        IPipelineBehavior<TRequest, TResponse>[] behaviors,
        Func<Task<TResponse>> finalHandler,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (behaviors.Length == 0)
        {
            return await finalHandler();
        }

        RequestHandlerDelegate<TResponse> next = () => finalHandler();

        // Build the pipeline from last to first
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var currentNext = next;
            next = () => behavior.HandleAsync(request, currentNext, cancellationToken);
        }

        return await next();
    }

    /// <summary>
    /// Executes a command handler without result and returns Unit.
    /// </summary>
    private static async Task<Unit> ExecuteHandlerAsync<TCommand>(
        ICommandHandler<TCommand> handler, 
        TCommand command, 
        CancellationToken cancellationToken)
        where TCommand : class, ICommand
    {
        await handler.HandleAsync(command, cancellationToken);
        return Unit.Value;
    }
}

/// <summary>
/// Represents a void result for commands that don't return values.
/// </summary>
public readonly struct Unit
{
    /// <summary>
    /// The default unit value.
    /// </summary>
    public static readonly Unit Value = new();
}
