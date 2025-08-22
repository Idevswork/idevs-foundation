using IdevsWork.Foundation.Cqrs.Commands;
using IdevsWork.Foundation.Cqrs.Queries;

namespace IdevsWork.Foundation.Mediator.Core;

/// <summary>
/// Interface for the mediator that handles command and query dispatching.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Sends a command for handling without expecting a result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;

    /// <summary>
    /// Sends a command for handling and expects a result.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to send.</typeparam>
    /// <typeparam name="TResult">The type of result expected from the command.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand<TResult>;

    /// <summary>
    /// Sends a query for handling and expects a result.
    /// </summary>
    /// <typeparam name="TQuery">The type of query to send.</typeparam>
    /// <typeparam name="TResult">The type of result expected from the query.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : class, IQuery<TResult>;
}
