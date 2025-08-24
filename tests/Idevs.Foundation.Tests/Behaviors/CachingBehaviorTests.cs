using FluentAssertions;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Mediator.Behaviors;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.Behaviors;

public class CachingBehaviorTests
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachingBehavior<TestCacheableRequest, string>> _logger;
    private readonly CachingBehavior<TestCacheableRequest, string> _behavior;

    public CachingBehaviorTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = Substitute.For<ILogger<CachingBehavior<TestCacheableRequest, string>>>();
        _behavior = new CachingBehavior<TestCacheableRequest, string>(_cache, _logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidCacheKey_ShouldCacheResponse()
    {
        // Arrange
        var request = new TestCacheableRequest("test-key", TimeSpan.FromMinutes(1));
        var expectedResponse = "test response";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _cache.TryGetValue("test-key", out string? cachedValue).Should().BeTrue();
        cachedValue.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        var request = new TestCacheableRequest("test-key", TimeSpan.FromMinutes(1));
        var cachedResponse = "cached response";
        _cache.Set("test-key", cachedResponse);
        
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns("new response");

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(cachedResponse);
        await next.DidNotReceive().Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithEmptyCacheKey_ShouldSkipCache()
    {
        // Arrange
        var request = new TestCacheableRequest("", TimeSpan.FromMinutes(1));
        var expectedResponse = "test response";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        _cache.TryGetValue("", out _).Should().BeFalse();
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithNullCacheKey_ShouldSkipCache()
    {
        // Arrange
        var request = new TestCacheableRequest(null!, TimeSpan.FromMinutes(1));
        var expectedResponse = "test response";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new CachingBehavior<TestCacheableRequest, string>(null!, _logger));
        
        exception.ParamName.Should().Be("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new CachingBehavior<TestCacheableRequest, string>(_cache, null!));
        
        exception.ParamName.Should().Be("logger");
    }

    public record TestCacheableRequest(string CacheKey, TimeSpan? CacheExpiration) : ICacheable;
}