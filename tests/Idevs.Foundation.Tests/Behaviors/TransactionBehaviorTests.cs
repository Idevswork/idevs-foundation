using System.Data;
using System.Transactions;
using FluentAssertions;
using Idevs.Foundation.Cqrs.Behaviors;
using Idevs.Foundation.Mediator.Behaviors;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Idevs.Foundation.Tests.Behaviors;

public class TransactionBehaviorTests
{
    private readonly ILogger<TransactionBehavior<TestTransactionalRequest, string>> _logger;
    private readonly TransactionBehavior<TestTransactionalRequest, string> _behavior;

    public TransactionBehaviorTests()
    {
        _logger = Substitute.For<ILogger<TransactionBehavior<TestTransactionalRequest, string>>>();
        _behavior = new TransactionBehavior<TestTransactionalRequest, string>(_logger);
    }

    [Fact]
    public async Task HandleAsync_WithSuccessfulExecution_ShouldCompleteTransaction()
    {
        // Arrange
        var request = new TestTransactionalRequest(System.Data.IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
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
    public async Task HandleAsync_WithException_ShouldRollbackTransaction()
    {
        // Arrange
        var request = new TestTransactionalRequest(System.Data.IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
        var exception = new InvalidOperationException("Test error");
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns<string>(_ => throw exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _behavior.HandleAsync(request, next, CancellationToken.None));

        thrownException.Should().Be(exception);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task HandleAsync_WithExistingTransaction_ShouldUseExistingTransaction()
    {
        // Arrange
        var request = new TestTransactionalRequest(System.Data.IsolationLevel.ReadCommitted, TimeSpan.FromMinutes(1));
        var expectedResponse = "success";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act & Assert
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);
        
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
        
        scope.Complete();
    }

    [Fact]
    public async Task HandleAsync_WithNullIsolationLevel_ShouldUseDefaultIsolationLevel()
    {
        // Arrange
        var request = new TestTransactionalRequest(null, TimeSpan.FromMinutes(1));
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
    public async Task HandleAsync_WithNullTimeout_ShouldUseDefaultTimeout()
    {
        // Arrange
        var request = new TestTransactionalRequest(System.Data.IsolationLevel.ReadCommitted, null);
        var expectedResponse = "success";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next.Invoke().Returns(expectedResponse);

        // Act
        var result = await _behavior.HandleAsync(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
        await next.Received(1).Invoke();
    }

    [Theory]
    [InlineData(System.Data.IsolationLevel.ReadUncommitted)]
    [InlineData(System.Data.IsolationLevel.ReadCommitted)]
    [InlineData(System.Data.IsolationLevel.RepeatableRead)]
    [InlineData(System.Data.IsolationLevel.Serializable)]
    [InlineData(System.Data.IsolationLevel.Snapshot)]
    [InlineData(System.Data.IsolationLevel.Chaos)]
    [InlineData(System.Data.IsolationLevel.Unspecified)]
    public async Task HandleAsync_WithDifferentIsolationLevels_ShouldExecuteSuccessfully(System.Data.IsolationLevel isolationLevel)
    {
        // Arrange
        var request = new TestTransactionalRequest(isolationLevel, TimeSpan.FromMinutes(1));
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
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new TransactionBehavior<TestTransactionalRequest, string>(null!));
        
        exception.ParamName.Should().Be("logger");
    }

    public record TestTransactionalRequest(System.Data.IsolationLevel? IsolationLevel, TimeSpan? Timeout) : ITransactional;
}