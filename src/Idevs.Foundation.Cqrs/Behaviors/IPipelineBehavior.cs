namespace Idevs.Foundation.Cqrs.Behaviors;

/// <summary>
/// Interface for pipeline behaviors that can be used to add cross-cutting concerns
/// to command and query processing.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : class
{
    /// <summary>
    /// Handles the request and delegates to the next behavior in the pipeline.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous operation with the response.</returns>
    Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next request handler in the pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
/// <returns>A task representing the asynchronous operation with the response.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
