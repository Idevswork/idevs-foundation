namespace Idevs.Foundation.Cqrs.Behaviors;

/// <summary>
/// Defines retry policies for requests.
/// </summary>
public enum RetryPolicy
{
    /// <summary>
    /// Fixed delay between retries.
    /// </summary>
    FixedDelay,
    
    /// <summary>
    /// Exponential backoff with optional jitter.
    /// </summary>
    ExponentialBackoff,
    
    /// <summary>
    /// Linear backoff.
    /// </summary>
    LinearBackoff
}

/// <summary>
/// Marker interface for requests that support retry functionality.
/// </summary>
public interface IRetryable
{
    /// <summary>
    /// Gets the maximum number of retry attempts.
    /// </summary>
    int MaxRetryAttempts { get; }

    /// <summary>
    /// Gets the base delay between retry attempts.
    /// </summary>
    TimeSpan BaseDelay { get; }

    /// <summary>
    /// Gets the retry policy to use.
    /// </summary>
    RetryPolicy RetryPolicy { get; }

    /// <summary>
    /// Gets a value indicating whether to add jitter to the delay (only applies to exponential backoff).
    /// </summary>
    bool UseJitter { get; }

    /// <summary>
    /// Determines if an exception should trigger a retry.
    /// </summary>
    /// <param name="exception">The exception that occurred.</param>
    /// <returns>True if the request should be retried; otherwise, false.</returns>
    bool ShouldRetry(Exception exception);
}