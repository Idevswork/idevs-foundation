using FluentAssertions;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Mediator.Behaviors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.Behaviors;

public class RetryBehaviorTests
{
    private readonly ILogger<RetryBehavior<TestRetryableRequest, string>> _logger;
    private readonly RetryBehavior<TestRetryableRequest, string> _behavior;

    public RetryBehaviorTests()
    {
        _logger = Substitute.For<ILogger<RetryBehavior<TestRetryableRequest, string>>>();
        _behavior = new RetryBehavior<TestRetryableRequest, string>(_logger);
    }

    [Fact]
    public async Task HandleAsync_WithSuccessfulFirstAttempt_ShouldNotRetry()
    {
        // Arrange
        var request = new TestRetryableRequest(3, TimeSpan.FromMilliseconds(100), RetryPolicy.FixedDelay);
        var expectedResponse = "success";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithRetryableException_ShouldRetryAndSucceed()
    {
        // Arrange
        var request = new TestRetryableRequest(2, TimeSpan.FromMilliseconds(10), RetryPolicy.FixedDelay);
        var expectedResponse = "success";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke()
            .Returns(Task.FromException<string>(new InvalidOperationException("Transient error")), 
                     Task.FromResult(expectedResponse));

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(2).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithNonRetryableException_ShouldNotRetry()
    {
        // Arrange
        var request = new TestRetryableRequest(3, TimeSpan.FromMilliseconds(10), RetryPolicy.FixedDelay, 
            ex => ex is not ArgumentException);
        var exception = new ArgumentException("Non-retryable error");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(Task.FromException<string>(exception));

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<ArgumentException>(() =>
            _behavior.HandleAsync(request, next, CancellationToken.None));

        thrownException.Should().Be(exception);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithMaxRetriesExceeded_ShouldThrowLastException()
    {
        // Arrange
        var request = new TestRetryableRequest(2, TimeSpan.FromMilliseconds(10), RetryPolicy.FixedDelay);
        var exception = new InvalidOperationException("Persistent error");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(Task.FromException<string>(exception));

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.HandleAsync(request, next, CancellationToken.None));

        thrownException.Should().Be(exception);
        await next.Received(3).Invoke(); // Initial attempt + 2 retries
    }

    [Theory]
    [InlineData(RetryPolicy.FixedDelay)]
    [InlineData(RetryPolicy.LinearBackoff)]
    [InlineData(RetryPolicy.ExponentialBackoff)]
    public async Task HandleAsync_WithDifferentRetryPolicies_ShouldApplyCorrectDelay(RetryPolicy policy)
    {
        // Arrange
        var request = new TestRetryableRequest(1, TimeSpan.FromMilliseconds(10), policy);
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke()
            .Returns(Task.FromException<string>(new InvalidOperationException("First failure")),
                     Task.FromResult("success"));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);
        var endTime = DateTime.UtcNow;

        // Assert
        result.Should().Be("success");
        await next.Received(2).Invoke();
        (endTime - startTime).Should().BeGreaterThan(TimeSpan.FromMilliseconds(5)); // Some delay occurred
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new RetryBehavior<TestRetryableRequest, string>(null!));
        
        exception.ParamName.Should().Be("logger");
    }

    public record TestRetryableRequest(
        int MaxRetryAttempts, 
        TimeSpan BaseDelay, 
        RetryPolicy RetryPolicy,
        Func<Exception, bool>? ShouldRetryFunc = null,
        bool UseJitter = false) : IRetryable
    {
        public bool ShouldRetry(Exception exception)
        {
            return ShouldRetryFunc?.Invoke(exception) ?? true;
        }
    }
}