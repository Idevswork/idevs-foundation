namespace IdevsWork.Foundation.Cqrs.Queries;

/// <summary>
/// Interface for handling queries that return a result.
/// </summary>
/// <typeparam name="TQuery">The type of query to handle.</typeparam>
/// <typeparam name="TResult">The type of result returned by the handler.</typeparam>
public interface IQueryHandler<in TQuery, TResult>
    where TQuery : class, IQuery<TResult>
{
    /// <summary>
    /// Handles the specified query and returns a result.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation with the result.</returns>
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
