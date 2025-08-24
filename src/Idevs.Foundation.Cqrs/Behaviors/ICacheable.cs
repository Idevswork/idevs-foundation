namespace Idevs.Foundation.Cqrs.Behaviors;

/// <summary>
/// Marker interface for requests that support caching.
/// </summary>
public interface ICacheable
{
    /// <summary>
    /// Gets the cache key for the request.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time. If null, default expiration is used.
    /// </summary>
    TimeSpan? CacheExpiration { get; }
}