using System.Text.Json;
using Idevs.Foundation.Cqrs.Behaviors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Idevs.Foundation.Mediator.Behaviors;

/// <summary>
/// Pipeline behavior that provides caching functionality for requests that implement ICacheable.
/// </summary>
/// <typeparam name="TRequest">The type of request.</typeparam>
/// <typeparam name="TResponse">The type of response.</typeparam>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, ICacheable
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger.</param>
    public CachingBehavior(IMemoryCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        var cacheKey = request.CacheKey;
        
        if (string.IsNullOrEmpty(cacheKey))
        {
            _logger.LogWarning("Cache key is null or empty for request {RequestType}, skipping cache", typeof(TRequest).Name);
            return await next();
        }

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out TResponse? cachedResponse))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedResponse!;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}", cacheKey);

        // Execute the request
        var response = await next();

        // Cache the response
        var expiration = request.CacheExpiration ?? DefaultCacheExpiration;
        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Size = CalculateSize(response)
        };

        _cache.Set(cacheKey, response, cacheEntryOptions);
        
        _logger.LogDebug("Cached response for key: {CacheKey} with expiration: {Expiration}", 
            cacheKey, expiration);

        return response;
    }

    private static long CalculateSize(TResponse response)
    {
        try
        {
            if (response == null) return 1;
            
            // Simple size estimation based on JSON serialization
            var json = JsonSerializer.Serialize(response);
            return json.Length;
        }
        catch
        {
            // If serialization fails, use default size
            return 1;
        }
    }
}