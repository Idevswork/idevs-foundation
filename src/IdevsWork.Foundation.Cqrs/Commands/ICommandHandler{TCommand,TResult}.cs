namespace IdevsWork.Foundation.Cqrs.Commands;

/// <summary>
/// Interface for handling commands that return a result.
/// </summary>
/// <typeparam name="TCommand">The type of command to handle.</typeparam>
/// <typeparam name="TResult">The type of result returned by the handler.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : class, ICommand<TResult>
{
    /// <summary>
    /// Handles the specified command and returns a result.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
