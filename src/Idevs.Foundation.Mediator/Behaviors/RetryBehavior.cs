using Idevs.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that provides retry functionality for requests that implement IRetryable.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public class RetryBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRetryable
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private static readonly Random Random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public RetryBehavior(ILogger<RetryBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var maxAttempts = Math.Max(1, request.MaxRetryAttempts + 1); // +1 for the initial attempt
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                if (attempt > 1)
                {
                    _logger.LogInformation("Retrying {RequestName} - Attempt {Attempt}/{MaxAttempts}",
                        requestName, attempt, maxAttempts);
                }

                var response = await next();
                
                if (attempt > 1)
                {
                    _logger.LogInformation("Retry succeeded for {RequestName} on attempt {Attempt}",
                        requestName, attempt);
                }

                return response;
            }
            catch (Exception ex)
            {
                lastException = ex;

                if (attempt == maxAttempts || !request.ShouldRetry(ex))
                {
                    _logger.LogWarning(ex, "Final attempt failed for {RequestName} after {Attempt} attempt(s)",
                        requestName, attempt);
                    throw;
                }

                var delay = CalculateDelay(request, attempt - 1); // attempt - 1 because we're calculating for the next retry
                
                _logger.LogWarning(ex, 
                    "Attempt {Attempt} failed for {RequestName}, retrying in {DelayMs}ms: {ErrorMessage}",
                    attempt, requestName, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // This should never be reached due to the throw in the catch block
        throw lastException ?? new InvalidOperationException("Unexpected retry loop exit");
    }

    private static TimeSpan CalculateDelay(IRetryable request, int retryAttempt)
    {
        var baseDelayMs = request.BaseDelay.TotalMilliseconds;
        
        var delayMs = request.RetryPolicy switch
        {
            RetryPolicy.FixedDelay => baseDelayMs,
            RetryPolicy.ExponentialBackoff => baseDelayMs * Math.Pow(2, retryAttempt),
            RetryPolicy.LinearBackoff => baseDelayMs * (retryAttempt + 1),
            _ => baseDelayMs
        };

        // Add jitter for exponential backoff if requested
        if (request.RetryPolicy == RetryPolicy.ExponentialBackoff && request.UseJitter)
        {
            var jitter = Random.NextDouble() * 0.1 * delayMs; // Up to 10% jitter
            delayMs += jitter;
        }

        return TimeSpan.FromMilliseconds(delayMs);
    }
}